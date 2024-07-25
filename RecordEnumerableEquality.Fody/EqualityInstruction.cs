using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using RecordEnumerableEquality.Fody.Utils;

namespace RecordEnumerableEquality.Fody;

public class EqualityInstruction
{
    private protected ExternalDefinitions _externalDefinitions;

    public int Offset { get; private set; }
    public Collection<Instruction> Instructions { get; private set; }
    public ILProcessor Processor { get; private set; }
    public GetDefaultComparerInstructionResult DefaultComparerInstructionResult { get; private set; }
    public MethodDefinition MethodDefinition { get; private set; }

    private protected Lazy<GenericInstanceType> _newComparerType;

    private protected Lazy<PropertyDefinition?> _property;

    private protected EqualityInstruction()
    {
        _newComparerType = new(() => CreateNewCreateType());
        _property = new(() => FindProperty());
    }

    public static EqualityInstruction Create(
        int offset,
        MethodDefinition methodDefinition,
        ILProcessor processor,
        GetDefaultComparerInstructionResult result,
        ExternalDefinitions externalDefinitions
    )
    {
        var instructions = methodDefinition.Body.Instructions;

        var isEquals = EqualsEqualityInstruction.IsPatternValid(instructions, offset);
        var isGetHashCode = HashCodeEqualityInstruction.IsPatternValid(instructions, offset);

        // return CtorImpl();
        if (isEquals)
        {
            return CtorImpl<EqualsEqualityInstruction>();
        }
        else if (isGetHashCode)
        {
            return CtorImpl<HashCodeEqualityInstruction>();
        }

        return Init(new EqualityInstruction());


        EqualityInstruction CtorImpl<T>()
            where T : EqualityInstruction, new()
        {
            return Init(new T());
        }

        EqualityInstruction Init(EqualityInstruction eq)
        {
            eq.Offset = offset;
            eq.Instructions = instructions;
            eq.Processor = processor;
            eq.DefaultComparerInstructionResult = result;
            eq.MethodDefinition = methodDefinition;

            eq._externalDefinitions = externalDefinitions;
            return eq;
        }
    }

    public void ReplaceGetDefaultInstruction()
    {
        var module = DefaultComparerInstructionResult.MethodReference!.Module;

        var getDefaultMethod = DefaultComparerInstructionResult.CreateGetDefaultMethod
        (
            module,
            _externalDefinitions.GetDefaultValueDef,
            _newComparerType.Value
        );

        Processor.Replace(DefaultComparerInstructionResult.Instruction, Processor.Create(OpCodes.Call, getDefaultMethod));
    }
    
    private GenericInstanceType CreateNewCreateType()
    {
        return DefaultComparerInstructionResult.CreateComparerType
        (
            DefaultComparerInstructionResult.MethodReference!.Module,
            _externalDefinitions.ValueComparerDefinition
        );
    }

    // Get custom attributes from property.
    // The challenge here, is that we do not have the property reference, but only the backing field reference.
    // We need to find the property reference from the backing field reference.
    public IEnumerable<CustomAttribute> CustomAttributes
        => _property.Value?.CustomAttributes ?? Enumerable.Empty<CustomAttribute>();

    public bool? IsExplicitlyEnabled
    {
        get
        {
            var attrib = CustomAttributes.FirstOrDefault(x => x.AttributeType.Name == "DeepEqualsAttribute");
            if (attrib == null)
            {
                return null;
            }

            if (attrib.ConstructorArguments.TryGetFirstOrDefault(out var arg))
            {
                return (bool)arg.Value;
            }

            return null;
        }
    }

    private PropertyDefinition? FindProperty()
    {
        var loadFiledInstructions = Instructions
                .Skip(Offset).Take(5)
                .Where(i => i.OpCode == OpCodes.Ldfld)
            ;

        var fields = loadFiledInstructions
            .Select(i => i.Operand)
            .OfType<FieldDefinition>()

            // We should get 2 fields but their definition should be equal
            .Distinct();

        var field = fields.SingleOrDefault();

        if (field == null)
        {
            return null;
        }

        return FindPropertyByBackingField(MethodDefinition.DeclaringType.Properties, field);
    }

    private PropertyDefinition FindPropertyByBackingField(Collection<PropertyDefinition> properties, FieldDefinition backingField)
    {
        foreach (var property in properties)
        {
            var getMethod = property.GetMethod;
            var setMethod = property.SetMethod;

            var accessesThroughGetter =
                getMethod == null || getMethod.Body.Instructions.Any(instr => instr.Operand == backingField);
            var accessesThroughSetter =
                setMethod == null || setMethod.Body.Instructions.Any(instr => instr.Operand == backingField);

            if (getMethod == null && setMethod == null)
            {
                continue;
            }

            if (accessesThroughGetter && accessesThroughSetter)
            {
                return property;
            }
        }

        return null;
    }


    public virtual bool IsPatternValid() => false;

    public virtual void InvokeReplacement()
    {
        throw new NotSupportedException();
    }
}