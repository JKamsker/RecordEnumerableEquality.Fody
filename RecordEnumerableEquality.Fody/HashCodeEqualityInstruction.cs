using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using RecordEnumerableEquality.Fody.Utils;


namespace RecordEnumerableEquality.Fody;

public class EqualsEqualityInstruction : EqualityInstruction
{
    public override bool IsPatternValid()
    {
        var i = Offset;
        var instructions = Processor.Body.Instructions;

        var valid1 = instructions.Skip(i).Take(4).All(ins => ins.OpCode != OpCodes.Callvirt);
        if (!valid1)
        {
            return false;
        }

        i += 5;
        return instructions[i].OpCode == OpCodes.Callvirt;
    }

    public override void InvokeReplacement()
    {
        ReplaceGetDefaultInstruction();
        ReplaceEqualsCallInstructionX();
    }

    public void ReplaceEqualsCallInstructionX()
    {
        var module = DefaultComparerInstructionResult.MethodReference!.Module;

        var importedEqualsMethod = module.ImportReference(_externalDefinitions.EqualsMethodDef);
        importedEqualsMethod.DeclaringType = _newComparerType.Value;

        var newInstruction = Processor.Create(OpCodes.Call, importedEqualsMethod);
        var instructions = Processor.Body.Instructions;

        var i = Offset;
        Ensure.NotEqual(instructions.Skip(i).Take(4), OpCodes.Callvirt);

        i += 5;
        Ensure.Assert(instructions[i].OpCode == OpCodes.Callvirt);

        Processor.Replace(instructions[i], newInstruction);
    }
    
    public static bool IsPatternValid(Collection<Instruction> instructions, int i)
    {
        var valid1 = instructions.Skip(i).Take(4).All(ins => ins.OpCode != OpCodes.Callvirt);
        if (!valid1)
        {
            return false;
        }

        i += 5;
        return instructions[i].OpCode == OpCodes.Callvirt;
    }
}

public class HashCodeEqualityInstruction : EqualityInstruction
{
    public override bool IsPatternValid()
    {
        var i = Offset;
        var instructions = Processor.Body.Instructions;

        var valid1 = instructions.Skip(i).Take(2).All(ins => ins.OpCode != OpCodes.Callvirt);
        if (!valid1)
        {
            return false;
        }

        i += 2;
        return instructions[i].OpCode == OpCodes.Callvirt;
    }

    public override void InvokeReplacement()
    {
        ReplaceGetDefaultInstruction();
        ReplaceGetHashCodeInstructionX();
    }


    //	/* 0x0000033F 280D00000A   */ IL_0016: call      class [System.Collections]System.Collections.Generic.EqualityComparer`1<!0> class [System.Collections]System.Collections.Generic.EqualityComparer`1<string>::get_Default()
    //	/* 0x00000344 02           */ IL_001B: ldarg.0
    //	/* 0x00000345 7B01000004   */ IL_001C: ldfld     string TheNamespace.MainClass::'<Property1>k__BackingField'
    //	/* 0x0000034A 6F0E00000A   */ IL_0021: callvirt  instance int32 class [System.Collections]System.Collections.Generic.EqualityComparer`1<string>::GetHashCode(!0)

    private void ReplaceGetHashCodeInstructionX()
    {
        var module = DefaultComparerInstructionResult.MethodReference!.Module;

        var importedGetHashCodeMethod = module.ImportReference(_externalDefinitions.GetHashCodeDef);
        importedGetHashCodeMethod.DeclaringType = _newComparerType.Value;

        var instructions = Processor.Body.Instructions;

        var i = Offset;
        Ensure.NotEqual(instructions.Skip(i).Take(2), OpCodes.Callvirt);
        i += 2;

        var callInstruction = instructions[++i];
        Ensure.Assert(callInstruction.OpCode == OpCodes.Callvirt);


        var newInstruction = Processor.Create(OpCodes.Callvirt, importedGetHashCodeMethod);

        Processor.Replace(callInstruction, newInstruction);
    }
    
    public static bool IsPatternValid(Collection<Instruction> instructions, int i)
    {
        var valid1 = instructions.Skip(i).Take(2).All(ins => ins.OpCode != OpCodes.Callvirt);
        if (!valid1)
        {
            return false;
        }

        i += 3;
        return instructions[i].OpCode == OpCodes.Callvirt;
    }
}