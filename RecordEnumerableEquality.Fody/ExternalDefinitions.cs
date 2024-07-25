using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace RecordEnumerableEquality.Fody;

public class ExternalDefinitions
{
    public TypeDefinition ValueComparerDefinition { get; private set; }
    public MethodDefinition GetHashCodeDef { get; private set; }
    public MethodDefinition EqualsMethodDef { get; private set; }
    public MethodDefinition GetDefaultValueDef { get; private set; }
    public TypeReference IEnumerableReference { get; private set; }

    public static ExternalDefinitions FromModule(ModuleDefinition ModuleDefinition)
    {
        var valueComparerDefinition = new TypeDefinition
        (
            "RecordEnumerableEquality",
            "EnumerableValueComparer`1",
            Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.BeforeFieldInit
        );
        valueComparerDefinition.GenericParameters.Add(new GenericParameter("T", valueComparerDefinition));

        // Better would be  ModuleDefinition.AssemblyReferences.Where(x => x.Name == "BasicFodyAddin").SingleOrDefault();
        // But the weaver doesnt have the Assembly at hand
        valueComparerDefinition.Scope = new AssemblyNameReference("RecordEnumerableEquality", null);

        var imported = ModuleDefinition.ImportReference(valueComparerDefinition);
        var resolved = imported.Resolve();

        var getHashCodeDef = resolved.Methods.Single(m => m.Name == "GetHashCode");
        var equalsMethodDef = resolved.Methods.Single(m => m.Name == "Equals");
        var getDefaultValueDef = resolved.Methods.Single(m => m.Name == "get_Default");
        var iEnumerableReference = ModuleDefinition.ImportReference(typeof(IEnumerable<>));
        
        return new ExternalDefinitions
        {
            ValueComparerDefinition = resolved,
            GetHashCodeDef = getHashCodeDef,
            EqualsMethodDef = equalsMethodDef,
            GetDefaultValueDef = getDefaultValueDef,
            IEnumerableReference = iEnumerableReference
        };
    }
}