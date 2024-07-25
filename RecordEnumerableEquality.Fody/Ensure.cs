using System.Collections.Generic;
using Fody;
using Mono.Cecil.Cil;

namespace RecordEnumerableEquality.Fody;

public static class Ensure
{
    public static void Assert(bool condition)
    {
        if (!condition)
        {
            throw new WeavingException("Assertion failed");
        }
    }

    // NotEqual: Collection<Instruction>, OpCode
    public static void NotEqual(IEnumerable<Instruction> instructions, OpCode opCode)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.OpCode == opCode)
            {
                throw new WeavingException("Assertion failed");
            }
        }
    }
}