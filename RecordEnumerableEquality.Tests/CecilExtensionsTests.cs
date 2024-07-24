using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RecordEnumerableEquality.Fody;
using Mono.Cecil;
using Xunit;

namespace Tests;

public class CecilExtensionsTests
{
    private readonly ModuleDefinition _module;

    public CecilExtensionsTests()
    {
        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(Path.GetDirectoryName(typeof(object).Assembly.Location));
        var readerParameters = new ReaderParameters { AssemblyResolver = resolver };

        _module = ModuleDefinition.ReadModule(typeof(CecilExtensionsTests).Module.FullyQualifiedName, readerParameters);
    }

    [Fact]
    public void Test_SimpleInterface()
    {
        var type = _module.ImportReference(typeof(ClassWithSimpleInterface));
        var expectedIntereface = _module.ImportReference(typeof(ISimpleInterface));

        var interfaces = BaseTypeResolver.GetInterfaces(type).Select(t => t.FullName).ToList();

        // Assert.Contains(typeof(ISimpleInterface).FullName, interfaces);
        Assert.Contains(expectedIntereface.FullName, interfaces);
    }

    [Fact]
    public void Test_GenericInterface()
    {
        var type = _module.ImportReference(typeof(ClassWithGenericInterface));
        var expectedIntereface = _module.ImportReference(typeof(IGenericInterface<string>));

        var allInterfaces = BaseTypeResolver.GetInterfaces(type);
        var interfaceNames = allInterfaces.Select(t => t.FullName).ToList();

        Assert.Contains(expectedIntereface.FullName, interfaceNames);
    }

    [Fact]
    public void Test_InterfaceFromBaseClass()
    {
        var type = _module.ImportReference(typeof(DerivedClass));
        var expectedIntereface = _module.ImportReference(typeof(IBaseInterface));

        var interfaces = BaseTypeResolver.GetInterfaces(type).Select(t => t.FullName).ToList();

        Assert.Contains(expectedIntereface.FullName, interfaces);
    }

    [Fact]
    public void Test_MultipleInterfaces()
    {
        var type = _module.ImportReference(typeof(ClassWithMultipleInterfaces));
        var expectedIntereface1 = _module.ImportReference(typeof(IInterface1));
        var expectedIntereface2 = _module.ImportReference(typeof(IInterface2));

        var interfaces = BaseTypeResolver.GetInterfaces(type).Select(t => t.FullName).ToList();

        // Assert.Contains(typeof(IInterface1).FullName, interfaces);
        // Assert.Contains(typeof(IInterface2).FullName, interfaces);

        Assert.Contains(expectedIntereface1.FullName, interfaces);
        Assert.Contains(expectedIntereface2.FullName, interfaces);
    }

    [Fact]
    public void Test_ResolvedGenericTypeArguments()
    {
        var type = _module.ImportReference(typeof(ClassWithResolvedGenerics));
        var interfaces = BaseTypeResolver.GetInterfaces(type).Select(t => t.FullName).ToList();

        Assert.Contains("System.Collections.Generic.IEnumerable`1<System.String>", interfaces);
    }

    [Fact]
    public void Test()
    {
        // CecilExtensions.GetGenericArgsMap
        // Type: Dictionary<TKey, TValue>
        // superTypeMap: {Dictionary<string, TypeReference>} Count = 2
        //     [0] = {KeyValuePair<string, TypeReference>} [TKey, System.Int32]
        //     [1] = {KeyValuePair<string, TypeReference>} [TValue, System.String]
        // mappedFromSuperType = {List<TypeReference>} Count = 2
        //     [0] = {TypeReference} System.Int32
        //     [1] = {TypeReference} System.String

        // Expected: {Dictionary<string, TypeReference>} Count = 2
        //     [0] = {KeyValuePair<string, TypeReference>} [TKey, System.Int32]
        //     [1] = {KeyValuePair<string, TypeReference>} [TValue, System.String]

        // Act
        var type = _module.ImportReference(typeof(Dictionary<int, string>));
        // var superType = _module.ImportReference(typeof(Dictionary<,>));
        var superTypeMap = new Dictionary<string, TypeReference>
        {
            { "TKey", _module.ImportReference(typeof(int)) },
            { "TValue", _module.ImportReference(typeof(string)) }
        };
        var mappedFromSuperType = new List<TypeReference>
        {
            _module.ImportReference(typeof(int)),
            _module.ImportReference(typeof(string))
        };

        var genericArgsMap = BaseTypeResolver.GetGenericArgsMap(type, superTypeMap, mappedFromSuperType);

        // Assert
        Assert.Equal(2, genericArgsMap.Count);
        Assert.Equal("System.Int32", genericArgsMap["TKey"].FullName);
        Assert.Equal("System.String", genericArgsMap["TValue"].FullName);
    }

    [Fact]
    public void Test_Generics_Resolved()
    {
        // CecilExtensions.GetGenericArgsMap
        // Type: System.Collections.Generic.ICollection`1<System.Collections.Generic.KeyValuePair`2<TKey,TValue>>
        // superTypeMap: {Dictionary<string, TypeReference>} Count = 2
        //     [0] = {KeyValuePair<string, TypeReference>} [TKey, System.Int32]
        //     [1] = {KeyValuePair<string, TypeReference>} [TValue, System.String]
        // mappedFromSuperType = {List<TypeReference>} Count = 2
        //     [0] = {TypeReference} System.Int32
        //     [1] = {TypeReference} System.String

        // Expected: {System.Collections.Generic.ICollection`1<System.Collections.Generic.KeyValuePair`2<System.Int32,System.String>>}
        //     [0] = {KeyValuePair<string, TypeReference>} [TKey, System.Int32]

        // Act
        var type = _module.ImportReference(typeof(ICollection<KeyValuePair<int, string>>));
        var innestParam = (GenericInstanceType)(((GenericInstanceType)type).GenericArguments[0]);

        // Make ICollection<KeyValuePair<int, string>> to ICollection<KeyValuePair<TKey, TValue>>
        innestParam.GenericArguments.Clear();
        innestParam.GenericArguments.Add(new GenericParameter("TKey", innestParam));
        innestParam.GenericArguments.Add(new GenericParameter("TValue", innestParam));

        var superTypeMap = new Dictionary<string, TypeReference>
        {
            { "TKey", _module.ImportReference(typeof(int)) },
            { "TValue", _module.ImportReference(typeof(string)) }
        };

        var mappedFromSuperType = new List<TypeReference>
        {
            _module.ImportReference(typeof(int)),
            _module.ImportReference(typeof(string))
        };

        var genericArgsMap = BaseTypeResolver.GetGenericArgsMap(type, superTypeMap, mappedFromSuperType);

        // Assert
        Assert.Equal(1, genericArgsMap.Count);
        Assert.Equal("System.Collections.Generic.KeyValuePair`2<System.Int32,System.String>", genericArgsMap["T"].FullName);
        // Assert.Equal("System.Collections.Generic.KeyValuePair`2<int,string>", genericArgsMap["T"].FullName);
    }

    public interface ISimpleInterface
    {
    }

    public interface IGenericInterface<T>
    {
    }

    public interface IBaseInterface
    {
    }

    public interface IInterface1
    {
    }

    public interface IInterface2
    {
    }

    public class ClassWithSimpleInterface : ISimpleInterface
    {
    }

    public class ClassWithGenericInterface : IGenericInterface<string>
    {
    }

    public class BaseClass : IBaseInterface
    {
    }

    public class DerivedClass : BaseClass
    {
    }

    public class ClassWithMultipleInterfaces : IInterface1, IInterface2
    {
    }

    public class ClassWithResolvedGenerics : System.Collections.Generic.IEnumerable<string>
    {
        public System.Collections.IEnumerator GetEnumerator() => throw new NotImplementedException();

        IEnumerator<string> IEnumerable<string>.GetEnumerator() => throw new NotImplementedException();
    }
}