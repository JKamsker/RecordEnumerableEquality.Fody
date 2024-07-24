using System;
using System.Collections.Generic;
using System.Linq;

using Mono.Cecil;

namespace RecordEnumerableEquality.Fody;

public record GenericArgumentResolverResult(
    TypeReference RequestedType,
    TypeReference DirectSubclass,
    TypeReference[] GenericArguments);

public static class GenericArgumentResolver
{
    /// <summary>
    /// Returns TEntity of EntityTypeBuilder{TEntity},
    /// even from EntityTypeBuilderOfEntity : EntityTypeBuilder{TEntity}.
    /// </summary>
    /// <param name="concreteType">For e.g EntityTypeBuilderOfEntity.</param>
    /// <param name="genericDefinition">for e.g typeof(EntityTypeBuilder{}).</param>
    /// <returns>An enumerable of generic arguments of <paramref name="genericDefinition"/>.</returns>
    public static IEnumerable<GenericArgumentResolverResult> GetGenericArgumentsOfTypeDefinition(
        TypeReference concreteType,
        TypeReference genericDefinition
    )
    {
        var nt = concreteType.GetBaseTypes(true);
        var genericDefinitionIsBase = concreteType.GetElementType().FullName == genericDefinition.FullName;
        if (genericDefinitionIsBase)
        {
            nt = nt.Append(concreteType);
        }
        
        return nt
            .Where(x => x.IsGenericInstance)
            .Where(x => x.GetElementType().FullName == genericDefinition.FullName)
            .Select(x => new GenericArgumentResolverResult(
                RequestedType: concreteType,
                DirectSubclass: x,
                GenericArguments: ((GenericInstanceType)x).GenericArguments.ToArray()
            ));
    }

    // same as GetGenericArgumentsOfTypeDefinition but returns only the first result
    public static GenericArgumentResolverResult? GetFirstGenericArgumentsOfTypeDefinition(
        TypeReference concreteType,
        TypeReference genericDefinition,
        Func<GenericArgumentResolverResult, bool>? predicate = null
    )
    {
        var result = GetGenericArgumentsOfTypeDefinition(concreteType, genericDefinition);
        if (predicate != null)
        {
            result = result.Where(predicate);
        }

        return result.FirstOrDefault();
    }

    private static IEnumerable<TypeReference> EnumerateBaseTypesAndInterfaces(
        TypeReference? type,
        bool returnInput = true
    )
    {
        if (type == null)
        {
            yield break;
        }

        if (returnInput)
        {
            yield return type;
        }

        if (type is GenericInstanceType genericInstanceType)
        {
            var resolvedType = genericInstanceType.Resolve();

            foreach (var interfaceType in resolvedType.Interfaces)
            {
                var genericInterface = interfaceType.InterfaceType;
                if (genericInterface is GenericInstanceType genericInterfaceType)
                {
                    var instantiatedInterface = new GenericInstanceType(genericInterfaceType.ElementType);
                    foreach (var (arg, pos) in genericInterfaceType.GenericArguments.Select((arg, i) => (arg, i)))
                    {
                        // instantiatedInterface.GenericArguments.Add(genericInstanceType.GenericArguments[arg.GenericParameter.Position]);
                        instantiatedInterface.GenericArguments.Add(
                            genericInstanceType.GenericArguments[pos]);
                    }

                    yield return instantiatedInterface;
                }
                else
                {
                    yield return interfaceType.InterfaceType;
                }
            }

            var current = resolvedType.BaseType;
            while (current != null)
            {
                var currentResolved = current.Resolve();
                foreach (var interfaceType in currentResolved.Interfaces)
                {
                    var genericInterface = interfaceType.InterfaceType;
                    if (genericInterface is GenericInstanceType genericInterfaceType)
                    {
                        var instantiatedInterface = new GenericInstanceType(genericInterfaceType.ElementType);
                        foreach (var (arg, pos) in genericInterfaceType.GenericArguments.Select((arg, i) => (arg, i)))
                        {
                            instantiatedInterface.GenericArguments.Add(
                                genericInstanceType.GenericArguments[pos]);
                        }

                        yield return instantiatedInterface;
                    }
                    else
                    {
                        yield return interfaceType.InterfaceType;
                    }
                }

                yield return current;
                current = current.Resolve().BaseType;
            }
        }
        else
        {
            var resolvedType = type.Resolve();

            foreach (var interfaceType in resolvedType.Interfaces)
            {
                yield return interfaceType.InterfaceType;
            }

            var current = resolvedType.BaseType;
            while (current != null)
            {
                var currentResolved = current.Resolve();
                foreach (var interfaceType in currentResolved.Interfaces)
                {
                    yield return interfaceType.InterfaceType;
                }

                yield return current;
                current = current.Resolve().BaseType;
            }
        }
    }
}