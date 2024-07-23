using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace RecordEnumerableEquality.Fody;

public static class CecilExtensions
{
    public static IEnumerable<TypeReference> GetAllInterfaces(TypeReference type)
    {
        return GetBaseTypes(type.Resolve(), true).Where(x => x.IsInterface());
    }

    // The following code was inspired by https://web.archive.org/web/20160412001031/http://blog.stevesindelar.cz/mono-cecil-how-to-get-all-base-types-and-interfaces-with-resolved-generic-arguments
    // Through https://mono-cecil.narkive.com/ijCNWVif/how-to-get-generic-arguments-of-a-base-type-or-an-interface

    private const string CannotResolveMessage = "Cannot resolve type {0} from type {1}";


    public static IEnumerable<TypeReference> GetBaseTypes(
        this TypeDefinition type,
        bool includeIfaces
    )
    {
        Contract.Requires(type != null);
        Contract.Requires(
            type.IsInterface == false,
            "GetBaseTypes is not valid for interfaces");

        var result = new List<TypeReference>();
        var current = type;
        var mappedFromSuperType = new List<TypeReference>();
        var previousGenericArgsMap =
            GetGenericArgsMap(
                type,
                new Dictionary<string, TypeReference>(),
                mappedFromSuperType);
        Contract.Assert(mappedFromSuperType.Count == 0);

        do
        {
            var currentBase = current.BaseType;
            if (currentBase is GenericInstanceType)
            {
                previousGenericArgsMap =
                    GetGenericArgsMap(
                        current.BaseType,
                        previousGenericArgsMap,
                        mappedFromSuperType);
                if (mappedFromSuperType.Any())
                {
                    currentBase = ((GenericInstanceType)currentBase)
                        .ElementType.MakeGenericInstanceType(
                            previousGenericArgsMap
                                .Select(x => x.Value)
                                .ToArray());
                    mappedFromSuperType.Clear();
                }
            }
            else
            {
                previousGenericArgsMap =
                    new Dictionary<string, TypeReference>();
            }

            result.Add(currentBase);

            if (includeIfaces)
            {
                result.AddRange(BuildIFaces(current, previousGenericArgsMap));
            }

            current = current.BaseType.SafeResolve(
                string.Format(
                    CannotResolveMessage,
                    current.BaseType.FullName,
                    current.FullName));
        } while (current.IsEqual(typeof(object)) == false);

        return result;
    }

    private static bool IsEqual(
        this TypeReference type,
        Type typeToCompare
    )
    {
        return type.FullName == typeToCompare.FullName;
    }

    private static IEnumerable<TypeReference> BuildIFaces(
        TypeDefinition type,
        IDictionary<string, TypeReference> genericArgsMap
    )
    {
        var mappedFromSuperType = new List<TypeReference>();
        foreach (var iface in type.Interfaces)
        {
            var result = iface;
            if (iface.InterfaceType is GenericInstanceType genIfaceType)
            {
                var map =
                    GetGenericArgsMap(
                        iface.InterfaceType,
                        genericArgsMap,
                        mappedFromSuperType);
                if (mappedFromSuperType.Any())
                {
                    var genericArgs = map.Select(x => x.Value).ToArray();
                    var res = genIfaceType.ElementType
                        .MakeGenericInstanceType(genericArgs);

                    yield return res;
                    continue;
                }
            }

            // yield return result;
            // yield return result.InterfaceType;
            var fixedType = FixGenericArgs(result.InterfaceType, genericArgsMap);
            yield return fixedType;

        }
    }

    internal static IDictionary<string, TypeReference> GetGenericArgsMap(
        TypeReference type,
        IDictionary<string, TypeReference> superTypeMap,
        IList<TypeReference> mappedFromSuperType
    )
    {
        var result = new Dictionary<string, TypeReference>();
        if (type is GenericInstanceType == false)
        {
            return result;
        }
        
        var fixedType = FixGenericArgs(type, superTypeMap);
        
        var genericArgs = ((GenericInstanceType)fixedType).GenericArguments;
        var genericPars = ((GenericInstanceType)fixedType)
            .ElementType.SafeResolve(CannotResolveMessage).GenericParameters;
        
        
        /*
         * Now genericArgs contain concrete arguments for the generic
         * parameters (genericPars).
         *
         * However, these concrete arguments don't necessarily have
         * to be concrete TypeReferences, these may be referencec to
         * generic parameters from super type.
         *
         * Example:
         *
         *      Consider following hierarchy:
         *          StringMap<T> : Dictionary<string, T>
         *
         *          StringIntMap : StringMap<int>
         *
         *      What would happen if we walk up the hierarchy from StringIntMap:
         *          -> StringIntMap
         *              - here dont have any generic agrs or params for StringIntMap.
         *              - but when we reesolve StringIntMap we get a
         *					reference to the base class StringMap<int>,
         *          -> StringMap<int>
         *              - this reference will have one generic argument
         *					System.Int32 and it's ElementType,
         *                which is StringMap<T>, has one generic argument 'T'.
         *              - therefore we need to remember mapping T to System.Int32
         *              - when we resolve this class we'll get StringMap<T> and it's base
         *              will be reference to Dictionary<string, T>
         *          -> Dictionary<string, T>
         *              - now *genericArgs* will be System.String and 'T'
         *              - genericPars will be TKey and TValue from Dictionary
         * 					declaration Dictionary<TKey, TValue>
         *              - we know that TKey is System.String and...
         *              - because we have remembered a mapping from T to
         *					System.Int32 and now we see a mapping from TValue to T,
         *              	we know that TValue is System.Int32, which bring us to
         *					conclusion that StringIntMap is instance of
         *          -> Dictionary<string, int>
         */

        for (int i = 0; i < genericArgs.Count; i++)
        {
            var arg = genericArgs[i];
            var param = genericPars[i];
            
            if (arg is GenericParameter)
            {
                TypeReference mapping;
                if (superTypeMap.TryGetValue(arg.Name, out mapping) == false)
                {
                    throw new Exception(
                        string.Format(
                            "GetGenericArgsMap: A mapping from supertype was not found. " +
                            "Program searched for generic argument of name {0} in supertype generic arguments map " +
                            "as it should server as value form generic argument for generic parameter {1} in the type {2}",
                            arg.Name,
                            param.Name,
                            type.FullName));
                }

                mappedFromSuperType.Add(mapping);
                result.Add(param.Name, mapping);
            }
            else
            {
                result.Add(param.Name, arg);
            }
        }

        return result;
    }
    
    // Fixes arg: 
    // eg:
    // Input:
    //      type: ICollection<KeyValuePair<TKey, TValue>>
    //      typeMap: Dictionary<string, TypeReference> { { "TKey", int }, { "TValue", string } }
    // Output:
    //      ICollection<KeyValuePair<int, string>>
    private static TypeReference FixGenericArgs(
        TypeReference type,
        IDictionary<string, TypeReference> typeMap
    )
    {
        if (type is GenericInstanceType genType)
        {
            var genericArgs = genType.GenericArguments;
            var genericType = genType.ElementType;
            var newGenericArgs = new List<TypeReference>();
            foreach (var arg in genericArgs)
            {
                if (arg is GenericParameter genParam)
                {
                    if (typeMap.TryGetValue(genParam.Name, out var mappedType))
                    {
                        newGenericArgs.Add(mappedType);
                    }
                    else
                    {
                        newGenericArgs.Add(arg);
                    }
                }
                else
                {
                    var fixedArg = FixGenericArgs(arg, typeMap);
                    newGenericArgs.Add(fixedArg);
                    // newGenericArgs.Add(arg);
                }
            }

            return genericType.MakeGenericInstanceType(newGenericArgs.ToArray());
        }

        return type;
    }

    private static TypeDefinition SafeResolve(
        this TypeReference type,
        string message
    )
    {
        var resolved = type.Resolve();
        if (resolved == null)
        {
            throw new Exception(message);
        }

        return resolved;
    }


    public static IEnumerable<TypeReference> GetBaseTypes(
        this TypeReference typeReference,
        bool includeIfaces
    )
    {
        var results = new List<TypeReference>();

        var mappedFromSuperType = new List<TypeReference>();
        var previousGenericArgsMap =
            GetGenericArgsMap(typeReference, new Dictionary<string, TypeReference>(), mappedFromSuperType);

        var current = typeReference.Resolve();
        
        do
        {
            if (includeIfaces)
            {
                results.AddRange(BuildIFaces(current, previousGenericArgsMap));
            }
            
            var currentBase = current.BaseType;
            if (currentBase is GenericInstanceType)
            {
                previousGenericArgsMap =
                    GetGenericArgsMap(
                        current.BaseType,
                        previousGenericArgsMap,
                        mappedFromSuperType);
                if (mappedFromSuperType.Any())
                {
                    currentBase = ((GenericInstanceType)currentBase)
                        .ElementType.MakeGenericInstanceType(
                            previousGenericArgsMap
                                .Select(x => x.Value)
                                .ToArray());
                    mappedFromSuperType.Clear();
                }
            }
            else
            {
                previousGenericArgsMap =
                    new Dictionary<string, TypeReference>();
            }

            // yield return currentBase;
            results.Add(currentBase);
            
            current = current.BaseType.SafeResolve(
                string.Format(
                    CannotResolveMessage,
                    current.BaseType.FullName,
                    current.FullName));
        } while (current.IsEqual(typeof(object)) == false);

        return results;
    }
}