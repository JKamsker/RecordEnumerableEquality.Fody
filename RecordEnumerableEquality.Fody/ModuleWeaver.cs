using Fody;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using RecordEnumerableEquality.Fody.Utils;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace RecordEnumerableEquality.Fody;

public partial class ModuleWeaver : BaseModuleWeaver
{
    private EqualityComparerInstructionHelper _helper;
    private EqualityInstructionFactory _equalityInstructionFactory;

    public ModuleWeaver()
    {
    }

    public ModuleWeaverSettings Settings { get; set; }
    public ExternalDefinitions ExternalDefinitions { get; private set; }


    public override void Execute()
    {
        EnsureInitialized();

        foreach (var type in ModuleDefinition.EnumerateAllTypes())
        {
            ProcessRecordType(type);
        }
    }

    private void EnsureInitialized()
    {
        Settings = ModuleWeaverSettings.Load(Config);

        if (ExternalDefinitions != null)
        {
            return;
        }


        ExternalDefinitions = ExternalDefinitions.FromModule(ModuleDefinition);
        _helper = new EqualityComparerInstructionHelper(ExternalDefinitions);
        _equalityInstructionFactory = new EqualityInstructionFactory(ExternalDefinitions, _helper);
    }


    private void ProcessRecordType(TypeDefinition type)
    {
        if (!type.IsRecord())
        {
            return;
        }

        foreach (var method in type.Methods)
        {
            var isEqualsMethod = method.Name == "Equals"
                                 && method.Parameters.Count == 1
                                 && method.Parameters[0].ParameterType.Name == type.Name;

            var isGetHashCodeMethod = method.Name == "GetHashCode"
                                      && method.Parameters.Count == 0;

            if (!isEqualsMethod && !isGetHashCodeMethod)
            {
                continue;
            }

            ProcessMethod(method);
        }
    }


    private void ProcessMethod(MethodDefinition method)
    {
        foreach (var instruction in _equalityInstructionFactory.FindEqualityInstructions(method))
        {
            if (!ShouldReplace(instruction))
            {
                continue;
            }


            instruction.InvokeReplacement();
        }
    }


    // Behavior: Disabled, IsExplicitlyEnabled: null or false: skip
    // Behavior: Disabled, IsExplicitlyEnabled: true: process
    // Behavior: Enabled, IsExplicitlyEnabled: null or true: process
    // Behavior: Enabled, IsExplicitlyEnabled: false: skip
    private bool ShouldReplace(EqualityInstruction instruction)
    {
        return (Settings.DefaultBehavior == DefaultBehavior.Enabled && instruction.IsExplicitlyEnabled is null or true)
               || (Settings.DefaultBehavior == DefaultBehavior.Disabled && instruction.IsExplicitlyEnabled is true);
    }


    public override IEnumerable<string> GetAssembliesForScanning()
    {
        yield return "netstandard";
        yield return "mscorlib";
    }
}