using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace RecordEnumerableEquality.Fody.Utils;

public static class Extensions
{
    public static IEnumerable<TypeDefinition> EnumerateAllTypes(this ModuleDefinition module)
    {
        return module.Types.SelectMany(t => t.EnumerateAllNestedTypes().Prepend(t));
    }

    private static IEnumerable<TypeDefinition> EnumerateAllNestedTypes(this TypeDefinition type)
    {
        foreach (var nestedType in type.NestedTypes)
        {
            yield return nestedType;

            foreach (var nestedNestedType in nestedType.EnumerateAllNestedTypes())
            {
                yield return nestedNestedType;
            }
        }
    }

    public static bool TryGetFirstOrDefault<T>(this IEnumerable<T> source, out T result)
    {
        using var enumerator = source.GetEnumerator();
        if (enumerator.MoveNext())
        {
            result = enumerator.Current;
            return true;
        }

        result = default!;
        return false;
    }
}