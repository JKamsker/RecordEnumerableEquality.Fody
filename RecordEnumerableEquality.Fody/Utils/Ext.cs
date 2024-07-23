using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace RecordEnumerableEquality.Fody;

public record GenericArgumentResult(
    TypeReference RequestedType,
    TypeReference DirectSubclass,
    TypeReference[] GenericArguments);

public static class GenericTypeHelper
{
    /// <summary>
    /// Returns TEntity of EntityTypeBuilder{TEntity}, 
    /// even from EntityTypeBuilderOfEntity : EntityTypeBuilder{TEntity}.
    /// </summary>
    /// <param name="concreteType">For e.g EntityTypeBuilderOfEntity.</param>
    /// <param name="genericDefinition">for e.g typeof(EntityTypeBuilder{}).</param>
    /// <returns>An enumerable of generic arguments of <paramref name="genericDefinition"/>.</returns>
    public static IEnumerable<GenericArgumentResult> GetGenericArgumentsOfTypeDefinition(
        TypeReference concreteType,
        TypeReference genericDefinition
    )
    {
        // var resolved = concreteType.Resolve();

        var nt = concreteType.GetBaseTypes(true);
            // .ToList();
        
        // return EnumerateBaseTypesAndInterfaces(concreteType)
        // return resolved.GetBaseTypes(true)
        return nt
            .Where(x => x.IsGenericInstance)
            .Where(x => x.GetElementType().FullName == genericDefinition.FullName)
            .Select(x => new GenericArgumentResult(
                RequestedType: concreteType,
                DirectSubclass: x,
                GenericArguments: ((GenericInstanceType)x).GenericArguments.ToArray()
            ));
    }

    // same as GetGenericArgumentsOfTypeDefinition but returns only the first result
    public static GenericArgumentResult? GetFirstGenericArgumentsOfTypeDefinition(
        TypeReference concreteType,
        TypeReference genericDefinition,
        Func<GenericArgumentResult, bool>? predicate = null
    )
    {
        var result = GetGenericArgumentsOfTypeDefinition(concreteType, genericDefinition);
        if (predicate != null)
        {
            result = result.Where(predicate);
        }

        return result.FirstOrDefault();
    }

    // private static IEnumerable<TypeReference> EnumerateBaseTypesAndInterfaces(TypeReference? type, bool returnInput = true)
    // {
    //     if (type == null)
    //     {
    //         yield break;
    //     }
    //
    //     if (returnInput)
    //     {
    //         yield return type;
    //     }
    //
    //     if (type is GenericInstanceType genericInstanceType)
    //     {
    //         
    //     }
    //     
    //
    //     var resolvedType = type.Resolve();
    //
    //     var current = resolvedType.BaseType;
    //     while (current != null)
    //     {
    //         var currentResolved = current.Resolve();
    //         foreach (var interfaceType in currentResolved.Interfaces)
    //         {
    //             yield return interfaceType.InterfaceType;
    //         }
    //
    //         yield return current;
    //         current = current.Resolve().BaseType;
    //     }
    //
    //     foreach (var interfaceType in resolvedType.Interfaces)
    //     {
    //         yield return interfaceType.InterfaceType;
    //     }
    // }

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