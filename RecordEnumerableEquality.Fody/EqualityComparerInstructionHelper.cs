using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace RecordEnumerableEquality.Fody;

public class EqualityComparerInstructionHelper
{
    private readonly ExternalDefinitions _externalDefinitions;

    public EqualityComparerInstructionHelper(ExternalDefinitions externalDefinitions)
    {
        _externalDefinitions = externalDefinitions;
    }

    public GetDefaultComparerInstructionResult TryGetDefaultComparerReference(Instruction instruction)
    {
        if (instruction.OpCode != OpCodes.Call)
        {
            return GetDefaultComparerInstructionResult.Fail;
        }

        // Checks if:
        // - EqualityComparer<List<SubClass>>
        // - EqualityComparer<IEnumerable<SubClass>>

        if (instruction.Operand is not MethodReference methodReference)
        {
            return GetDefaultComparerInstructionResult.Fail;
        }

        // Is get_Default method?
        if (methodReference.Name != "get_Default")
        {
            return GetDefaultComparerInstructionResult.Fail;
        }

        //  methodReference.DeclaringType: EqualityComparer<List<SubClass>> ?
        var isEnumerableComparer = methodReference.DeclaringType.Name.StartsWith("EqualityComparer");
        if (!isEnumerableComparer)
        {
            return GetDefaultComparerInstructionResult.Fail;
        }

        //  methodReference.DeclaringType: List<SubClass> implements IEnumerable<SubClass> ?
        // type is the property type eg System.Collections.Generic.List`1<TestAssembly.SubClass>
        var enumerableType = methodReference.DeclaringType.GetGenericArguments().SingleOrDefault();
        if (!enumerableType.ImplementsInterface(_externalDefinitions.IEnumerableReference))
        {
            return GetDefaultComparerInstructionResult.Fail;
        }

        // Exclude String
        if (enumerableType.FullName == "System.String")
        {
            return GetDefaultComparerInstructionResult.Fail;
        }

        // Get SubClass from List<SubClass> {System.Collections.Generic.IEnumerable`1<TestAssembly.SubClass>}
        // When type is List<SubClass> then subclass is SubClass
        var elementType = enumerableType
            .GetGenericArgumentsForInterface(_externalDefinitions.IEnumerableReference)
            .SingleOrDefault()?[0];

        if (elementType == null)
        {
            return GetDefaultComparerInstructionResult.Fail;
        }

        // var extRef = _valueComparerDefinition.MakeGenericInstanceType(elementType);
        // var newRef = method.Module.ImportReference(extRef);

        return GetDefaultComparerInstructionResult.Success(instruction, methodReference, methodReference.DeclaringType,
            enumerableType, elementType);
    }

    public void ReplaceGetDefaultInstruction(ILProcessor processor, GetDefaultComparerInstructionResult result)
    {
        var module = result.MethodReference!.Module;

        var newComparerType = result.CreateComparerType(module, _externalDefinitions.ValueComparerDefinition);
        var getDefaultMethod = result.CreateGetDefaultMethod(module, _externalDefinitions.GetDefaultValueDef, newComparerType);

        processor.Replace(result.Instruction, processor.Create(OpCodes.Call, getDefaultMethod));
    }


    public void ReplaceEqualsCallInstruction(ILProcessor processor, int i, GetDefaultComparerInstructionResult result)
    {
        var module = result.MethodReference!.Module;

        var newComparerType = result.CreateComparerType(module, _externalDefinitions.ValueComparerDefinition);
        var importedEqualsMethod = module.ImportReference(_externalDefinitions.EqualsMethodDef);
        importedEqualsMethod.DeclaringType = newComparerType;

        var newInstruction = processor.Create(OpCodes.Call, importedEqualsMethod);
        var instructions = processor.Body.Instructions;

        Ensure.NotEqual(instructions.Skip(i).Take(4), OpCodes.Callvirt);

        i += 5;
        Ensure.Assert(instructions[i].OpCode == OpCodes.Callvirt);

        processor.Replace(instructions[i], newInstruction);
    }
}

public record struct GetDefaultComparerInstructionResult(
    bool IsSuccess,
    Instruction? Instruction,
    MethodReference? MethodReference,

    // Eg: EqualityComparer<List<SubClass>>
    TypeReference? ComparerType,

    // Eg: List<SubClass>
    TypeReference? EnumerableType,

    // Eg: SubClass
    TypeReference? ElementType
)
{
    public static GetDefaultComparerInstructionResult Fail =>
        new GetDefaultComparerInstructionResult(false, null, null, null, null, null);

    public static GetDefaultComparerInstructionResult Success(
        Instruction instruction,
        MethodReference methodReference,
        TypeReference comparerType,
        TypeReference enumerableType,
        TypeReference elementType
    )
    {
        return new GetDefaultComparerInstructionResult(true, instruction, methodReference, comparerType, enumerableType,
            elementType);
    }

    // Creates new comparer type eg EnumerableValueComparer<SubClass>
    public GenericInstanceType CreateComparerType(ModuleDefinition module, TypeReference valueComparerDefinition)
    {
        var genericType = valueComparerDefinition.MakeGenericInstanceType(ElementType);
        var extRef = module.ImportReference(genericType);
        return (GenericInstanceType)extRef;
    }

    public MethodReference CreateGetDefaultMethod(
        ModuleDefinition module,
        MethodReference getDefaultValueDefinition,
        TypeReference declaringType
    )
    {
        var importedGetDefaultMethod = module.ImportReference(getDefaultValueDefinition);
        importedGetDefaultMethod.DeclaringType = declaringType;

        return importedGetDefaultMethod;
    }
}