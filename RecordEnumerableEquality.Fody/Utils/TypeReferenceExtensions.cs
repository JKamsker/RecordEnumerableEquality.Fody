using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel;
using System;

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

        // Resolve the type to get its full definition
        var typeDef = type.Resolve();
        if (typeDef == null)
        {
            return false;
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

        var res = GenericTypeHelper.GetGenericArgumentsOfTypeDefinition(type, interfaceType);
        return res.Select(x => x.GenericArguments);
    }
}