using System.Collections.Generic;
using Mono.Cecil;

namespace RecordEnumerableEquality.Fody;

public class EqualityInstructionFactory
{
    private readonly ExternalDefinitions _externalDefinitions;
    private readonly EqualityComparerInstructionHelper _helper;

    public EqualityInstructionFactory(ExternalDefinitions externalDefinitions, EqualityComparerInstructionHelper helper)
    {
        _externalDefinitions = externalDefinitions;
        _helper = helper;
    }

    public IEnumerable<EqualityInstruction> FindEqualityInstructions(MethodDefinition method)
    {
        var processor = method.Body.GetILProcessor();
        var instructions = method.Body.Instructions;

        for (int i = 0; i < instructions.Count; i++)
        {
            var instruction = instructions[i];

            var defaultComparerCall = _helper.TryGetDefaultComparerReference(instruction);
            if (defaultComparerCall == GetDefaultComparerInstructionResult.Fail)
            {
                continue;
            }

            yield return EqualityInstruction.Create(i, method, processor, defaultComparerCall, _externalDefinitions);
        }
    }
}