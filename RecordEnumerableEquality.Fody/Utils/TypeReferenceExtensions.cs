using Mono.Cecil;
using Mono.Cecil.Cil;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace RecordEnumerableEquality.Fody;

public static partial class TypeReferenceExtensions
{
    // Not null if returns true attribute

    public static bool ImplementsInterface([NotNullWhen(false)] this TypeReference type, TypeReference interfaceType)
    {
        if (type == null)
        {
            return false;
        }

        // If type is object, it does not implement any interface
        if (type.FullName == "System.Object")
        {
            return false;
        }

        // Are we looking for IInterface<> or IInterface<string> or INonGenericInterface

        var interfaceKind = interfaceType.DetermineInterfaceType();

        // InterfaceType.NonGenericInterface would work on the other path aswell but this one feels better
        // Please submit unit tests to prove me wrong or right ¯\_(ツ)_/¯
        if (interfaceKind is InterfaceType.GenericInterfaceWithTypeArgument or InterfaceType.NonGenericInterface)
        {
            var interfaces = type.GetInterfaces();
            if (interfaces.Select(x => x.FullName).Contains(interfaceType.FullName))
            {
                return true;
            }

            return false;
        }

        // Special case: type.IsArray is true and interfaceType is IEnumerable<>
        if (interfaceKind == InterfaceType.GenericInterfaceDefinition
            && type.IsArray
            && interfaceType.FullName == "System.Collections.Generic.IEnumerable`1")
        {
            return true;
        }

        //if (interfaceKind == InterfaceType.NonGenericInterface)
        //{
        //    //Debugger.Break();
        //}

        // Resolve the type to get its full definition
        var typeDef = type.Resolve();
        if (typeDef == null)
        {
            return false;
        }

        if (interfaceType.Resolve() == typeDef)
        {
            return true;
        }

        // Check all interfaces implemented by this type
        foreach (var iface in typeDef.Interfaces)
        {
            if (TypeMatches(iface.InterfaceType, interfaceType))
            {
                return true;
            }
        }

        // Recursively check base types
        var baseType = typeDef.BaseType;
        if (baseType != null)
        {
            return ImplementsInterface(baseType, interfaceType);
        }

        return false;
    }

    private static bool TypeMatches(TypeReference type, TypeReference interfaceType)
    {
        // Check if both types are the same
        if (type.FullName == interfaceType.FullName)
        {
            return true;
        }

        // Check if the type is a generic instance and the interfaceType is a generic type definition
        if (type is GenericInstanceType genericType && interfaceType.HasGenericParameters)
        {
            if (genericType.ElementType.FullName == interfaceType.FullName)
            {
                return true;
            }
        }

        // Check if both are generic and have the same generic type definition
        if (type is GenericInstanceType genericTypeInstance &&
            interfaceType is GenericInstanceType genericInterfaceTypeInstance)
        {
            if (genericTypeInstance.ElementType.FullName == genericInterfaceTypeInstance.ElementType.FullName)
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsInterface(this TypeReference typeReference)
    {
        return typeReference.Resolve()?.IsInterface ?? false;
    }

    public static IEnumerable<TypeReference> GetEqualityComparerGenericArguments(this Instruction instruction)
    {
        if (instruction.OpCode != OpCodes.Call && instruction.OpCode != OpCodes.Callvirt)
        {
            yield break;
        }

        if (instruction.Operand is not MethodReference methodReference)
        {
            yield break;
        }

        if (!methodReference.DeclaringType.Name.StartsWith("EqualityComparer"))
        {
            yield break;
        }

        if (methodReference.DeclaringType is not GenericInstanceType genericInstanceType)
        {
            yield break;
        }

        foreach (var genericArgument in genericInstanceType.GenericArguments)
        {
            yield return genericArgument;
        }
    }

    public static IEnumerable<TypeReference> GetGenericArguments(this TypeReference typeReference)
    {
        if (typeReference is not GenericInstanceType genericInstanceType)
        {
            yield break;
        }

        foreach (var genericArgument in genericInstanceType.GenericArguments)
        {
            yield return genericArgument;
        }
    }

    public enum InterfaceType
    {
        /// <summary>
        /// Eg INonGenericInterface
        /// </summary>
        NonGenericInterface,

        /// <summary>
        /// Eg IGenericInterface<>
        /// </summary>
        GenericInterfaceDefinition,

        /// <summary>
        /// Eg IGenericInterface<string>
        /// </summary>
        GenericInterfaceWithTypeArgument
    }

    public static InterfaceType DetermineInterfaceType(this Mono.Cecil.TypeReference typeReference)
    {
        var isInterface = typeReference.IsInterface();
        if (!isInterface)
        {
            throw new ArgumentException("Type is not an interface");
        }

        if (typeReference.IsGenericInstance)
        {
            // Case 3: Generic Interface with Type Argument
            return InterfaceType.GenericInterfaceWithTypeArgument;
        }
        else if (typeReference.HasGenericParameters)
        {
            // Case 2: Generic Interface Definition
            return InterfaceType.GenericInterfaceDefinition;
        }
        else
        {
            // Case 1: Non-Generic Interface
            return InterfaceType.NonGenericInterface;
        }
    }
}

public static partial class TypeReferenceExtensions
{
    public static IEnumerable<TypeReference[]> GetGenericArgumentsForInterface(
        this TypeReference type,
        TypeReference interfaceType
    )
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (interfaceType == null)
            throw new ArgumentNullException(nameof(interfaceType));

        var res = GenericArgumentResolver
            .GetGenericArgumentsOfTypeDefinition(type, interfaceType);
        return res.Select(x => x.GenericArguments);
    }
}