using Mono.Cecil;

using System.Linq;

namespace RecordEnumerableEquality.Fody.Utils;

internal static class RecordDetector
{
    public static bool IsRecord(this TypeReference typeReference)
    {
        // Resolve the type definition from the type reference
        var typeDefinition = typeReference.Resolve();

        return IsRecord(typeDefinition);
    }

    public static bool IsRecord(this TypeDefinition typeDefinition)
    {
        // Check if the type definition contains the EqualityContract property
        var hasEqualityContract = typeDefinition.Properties.Any(static p =>
            p.Name == "EqualityContract" && p.PropertyType.FullName == "System.Type" && p.GetMethod != null);

        if (!hasEqualityContract)
            return false;

        // Check if the type definition contains a PrintMembers method
        var hasPrintMembers = typeDefinition.Methods.Any(static m =>
            m.Name == "PrintMembers" && m.ReturnType.FullName == "System.Boolean" &&
            m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.Text.StringBuilder");

        if (!hasPrintMembers)
            return false;

        // Check if the type definition contains an overridden ToString method
        var hasToStringOverride = typeDefinition.Methods.Any(static m =>
            m.Name == "ToString" && m.ReturnType.FullName == "System.String" && m.IsVirtual && !m.IsAbstract);

        if (!hasToStringOverride)
            return false;

        // Check if the type definition contains a clone method with a specific pattern
        var hasCloneMethod = typeDefinition.Methods.Any(m =>
            m.Name.Contains("Clone") && m.Parameters.Count == 0 &&
            m.ReturnType.FullName == typeDefinition.FullName);

        if (!hasCloneMethod)
            return false;

        // Check for compiler-generated backing fields
        var compilerGeneratedBackingFields = typeDefinition.Fields
            .Where(static f => f.IsPrivate && f.Name.Contains("k__BackingField"))
            .ToList();

        // Check if properties have corresponding compiler-generated backing fields
        var propertiesWithBackingFields = typeDefinition.Properties
            .Where(p => compilerGeneratedBackingFields.Any(f => f.Name.Contains(p.Name)))
            .ToList();

        if (!propertiesWithBackingFields.Any())
            return false;

        // If all checks passed, it is a record
        return true;
    }
}