using Fody;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RecordEnumerableEquality.Fody;

public partial class ModuleWeaver : BaseModuleWeaver
{
    private TypeDefinition _valueComparerDefinition;
    private MethodDefinition _getHashCodeDef;
    private MethodDefinition _equalsMethodDef;
    private MethodDefinition _getDefaultValueDef;
    private TypeReference _iEnumerableReference;

    public override void Execute()
    {
        EnsureInitialized();

        foreach (var type in ModuleDefinition.Types)
        {
            ProcessRecordType(type);
        }
    }

    private void EnsureInitialized()
    {
        if (_valueComparerDefinition == null)
        {
            _valueComparerDefinition = new TypeDefinition
            (
                "RecordEnumerableEquality",
                "EnumerableValueComparer`1",
                Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.BeforeFieldInit
            );
            _valueComparerDefinition.GenericParameters.Add(new GenericParameter("T", _valueComparerDefinition));

            // Better would be  ModuleDefinition.AssemblyReferences.Where(x => x.Name == "BasicFodyAddin").SingleOrDefault();
            // But the weaver doesnt have the Assembly at hand
            _valueComparerDefinition.Scope = new AssemblyNameReference("RecordEnumerableEquality", null);

            var imported = ModuleDefinition.ImportReference(_valueComparerDefinition);
            var resolved = imported.Resolve();

            _getHashCodeDef = resolved.Methods.Single(m => m.Name == "GetHashCode");
            _equalsMethodDef = resolved.Methods.Single(m => m.Name == "Equals");
            _getDefaultValueDef = resolved.Methods.Single(m => m.Name == "get_Default");
            _iEnumerableReference = ModuleDefinition.ImportReference(typeof(IEnumerable<>));
        }
    }

    private void ProcessNestedTypes(TypeDefinition type)
    {
        foreach (var nestedType in type.NestedTypes)
        {
            ProcessRecordType(nestedType);
        }
    }

    private void ProcessRecordType(TypeDefinition type)
    {
        if (!type.IsRecord())
        {
            ProcessNestedTypes(type);
            return;
        }

        foreach (var method in type.Methods)
        {
            var isEqualsMethod = method.Name == "Equals"
                                 && method.Parameters.Count == 1
                                 && method.Parameters[0].ParameterType.Name == type.Name;

            if (isEqualsMethod)
            {
                ProcessEqualsMethod(method);
                continue;
            }

            var isGetHashCodeMethod = method.Name == "GetHashCode"
                                      && method.Parameters.Count == 0;
            if (isGetHashCodeMethod)
            {
                ProcessGetHashCodeMethod(method);
                continue;
            }
        }

        ProcessNestedTypes(type);
    }

    private void ProcessEqualsMethod(MethodDefinition method)
    {
        //var ienumerableReference = method.Module.ImportReference(typeof(IEnumerable<>));

        var processor = method.Body.GetILProcessor();
        var instructions = method.Body.Instructions;

        for (int i = 0; i < instructions.Count; i++)
        {
            var instruction = instructions[i];
            var isDefaultComparerCall = TryReplaceGetDefaultComparer(method, instruction, processor, out var comparerType);
            if (!isDefaultComparerCall)
            {
                continue;
            }

            // Skip nextd 4 instructions, assuming they are not callvirt instructions
            Assert(instructions[i + 1].OpCode != OpCodes.Callvirt);
            Assert(instructions[i + 2].OpCode != OpCodes.Callvirt);
            Assert(instructions[i + 3].OpCode != OpCodes.Callvirt);
            Assert(instructions[i + 4].OpCode != OpCodes.Callvirt);

            i += 5;

            Assert(instructions[i].OpCode == OpCodes.Callvirt);

            //var equalsMethodDef = comparerType.Resolve().Methods.Single(m => m.Name == "Equals");

            var importedEqualsMethod = method.Module.ImportReference(_equalsMethodDef);
            importedEqualsMethod.DeclaringType = comparerType;

            var newInstruction = processor.Create(OpCodes.Callvirt, importedEqualsMethod);

            processor.Replace(instructions[i], newInstruction);
        }
    }

    //	/* 0x0000033F 280D00000A   */ IL_0016: call      class [System.Collections]System.Collections.Generic.EqualityComparer`1<!0> class [System.Collections]System.Collections.Generic.EqualityComparer`1<string>::get_Default()
    //	/* 0x00000344 02           */ IL_001B: ldarg.0
    //	/* 0x00000345 7B01000004   */ IL_001C: ldfld     string TheNamespace.MainClass::'<Property1>k__BackingField'
    //	/* 0x0000034A 6F0E00000A   */ IL_0021: callvirt  instance int32 class [System.Collections]System.Collections.Generic.EqualityComparer`1<string>::GetHashCode(!0)

    private void ProcessGetHashCodeMethod(MethodDefinition method)
    {
        var processor = method.Body.GetILProcessor();
        var instructions = method.Body.Instructions;

        for (int i = 0; i < instructions.Count; i++)
        {
            var instruction = instructions[i];

            // Replace IL_0016
            var isGetHashCodeCall = TryReplaceGetDefaultComparer(method, instruction, processor, out var comparerType);
            if (!isGetHashCodeCall)
            {
                continue;
            }

            // Skip IL_001B, IL_001C
            Assert(instructions[++i].OpCode != OpCodes.Callvirt);
            Assert(instructions[++i].OpCode != OpCodes.Callvirt);

            var callInstruction = instructions[++i];
            Assert(callInstruction.OpCode == OpCodes.Callvirt);

            // Replace the call to the default GetHashCode with our custom comparer
            //var getHashCodeMethodDef = comparerType.Resolve().Methods.Single(m => m.Name == "GetHashCode");
            var importedGetHashCodeMethod = method.Module.ImportReference(_getHashCodeDef);
            importedGetHashCodeMethod.DeclaringType = comparerType;

            var newInstruction = processor.Create(OpCodes.Callvirt, importedGetHashCodeMethod);

            processor.Replace(callInstruction, newInstruction);
        }
    }

    private void Assert(bool condition)
    {
        if (!condition)
        {
            throw new WeavingException("Assertion failed");
        }
    }

    private bool TryReplaceGetDefaultComparer(
        MethodDefinition method,
        Instruction instruction,
        ILProcessor processor,
        out GenericInstanceType comparerType
    )
    {
        comparerType = null;

        if (instruction.OpCode != OpCodes.Call)
        {
            return false;
        }

        // Checks if:
        // - EqualityComparer<List<SubClass>>
        // - EqualityComparer<IEnumerable<SubClass>>

        if (instruction.Operand is not MethodReference methodReference)
        {
            return false;
        }

        // Is get_Default method?
        if (methodReference.Name != "get_Default")
        {
            return false;
        }

        //  methodReference.DeclaringType: EqualityComparer<List<SubClass>> ?
        var isEnumerableComparer = methodReference.DeclaringType.Name.StartsWith("EqualityComparer");
        if (!isEnumerableComparer)
        {
            return false;
        }

        //  methodReference.DeclaringType: List<SubClass> implements IEnumerable<SubClass> ?
        var type = methodReference.DeclaringType.GetGenericArguments().SingleOrDefault();
        if (!type.ImplementsInterface(_iEnumerableReference))
        {
            return false;
        }

        // Exclude String
        if (type.FullName == "System.String")
        {
            return false;
        }

        // Get SubClass from List<SubClass>
        var genericArgumentsForEnumerable = type.GetGenericArgumentsForInterface(_iEnumerableReference).ToList();
        var subClass = genericArgumentsForEnumerable.SingleOrDefault()?[0];
        if (subClass == null)
        {
            return false;
        }

        var extRef = _valueComparerDefinition.MakeGenericInstanceType(subClass);

        var newRef = method.Module.ImportReference(extRef);

        comparerType = (GenericInstanceType)newRef;

        //var getDefaultMethod = newRef.Resolve().Methods.First(m => m.Name == "get_Default");

        var importedGetDefaultMethod = method.Module.ImportReference(_getDefaultValueDef);
        importedGetDefaultMethod.DeclaringType = newRef;

        var newInstruction = processor.Create(OpCodes.Call, importedGetDefaultMethod);
        processor.Replace(instruction, newInstruction);

        return true;
    }

    public override IEnumerable<string> GetAssembliesForScanning()
    {
        yield return "netstandard";
        yield return "mscorlib";
    }
}