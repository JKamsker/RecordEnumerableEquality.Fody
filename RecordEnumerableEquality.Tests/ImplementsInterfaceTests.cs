using RecordEnumerableEquality.Fody;
using Xunit;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;
using System;
using static RecordEnumerableEquality.Fody.TypeReferenceExtensions;

namespace Tests;

public class ImplementsInterfaceTests
{
    private ModuleDefinition _module;

    public ImplementsInterfaceTests()
    {
        _module = ModuleDefinition.ReadModule(typeof(ImplementsInterfaceTests).Module.FullyQualifiedName);
    }

    [Fact]
    public void Test_NonGeneric_Interface_Implemented()
    {
        // Arrange
        var type = _module.ImportReference(typeof(ClassImplementingNonGenericInterface));
        var interfaceType = _module.ImportReference(typeof(INonGenericInterface));

        // Act
        var result = type.ImplementsInterface(interfaceType);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Test_NonGeneric_Interface_Not_Implemented()
    {
        // Arrange
        var type = _module.ImportReference(typeof(ClassWithoutInterface));
        var interfaceType = _module.ImportReference(typeof(INonGenericInterface));

        // Act
        var result = type.ImplementsInterface(interfaceType);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Test_Generic_Interface_Implemented()
    {
        // Arrange
        var type = _module.ImportReference(typeof(ClassImplementingGenericInterface));
        var specificInterfaceType = _module.ImportReference(typeof(IGenericInterface<string>));
        var genericInterfaceType = _module.ImportReference(typeof(IGenericInterface<>));

        // Act
        var result = type.ImplementsInterface(specificInterfaceType);
        var genericResult = type.ImplementsInterface(genericInterfaceType);

        // Assert
        Assert.True(result);
        Assert.True(genericResult);
    }

    [Fact]
    public void Test_Generic_Interface_Not_Implemented()
    {
        // Arrange
        var type = _module.ImportReference(typeof(ClassWithoutInterface));
        var specificInterfaceType = _module.ImportReference(typeof(IGenericInterface<string>));

        // Act
        var result = type.ImplementsInterface(specificInterfaceType);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Test_Interface_Implemented_In_Base_Class()
    {
        // Arrange
        var type = _module.ImportReference(typeof(DerivedClassImplementingInterface));
        var interfaceType = _module.ImportReference(typeof(INonGenericInterface));

        // Act
        var result = type.ImplementsInterface(interfaceType);

        // Assert
        Assert.True(result);
    }

    // ClassWithIntGerneric implements IGenericInterface<int> but not IGenericInterface<string>
    [Fact]
    public void Test_Generic_Interface_Implemented_With_Different_Generic_Argument()
    {
        // Arrange
        //var module = ModuleDefinition.ReadModule(typeof(ClassWithIntGerneric).Module.FullyQualifiedName);
        var type = _module.ImportReference(typeof(ClassWithIntGerneric));
        var interfaceType = _module.ImportReference(typeof(IGenericInterface<string>));
        var expectedInterface = _module.ImportReference(typeof(IGenericInterface<int>));

        // Act
        var result = type.ImplementsInterface(interfaceType);
        var expected = type.ImplementsInterface(expectedInterface);

        // Assert
        Assert.False(result);
        Assert.True(expected);
    }

    // Array implemnts interface of IEnumerable<> ?
    [Fact]
    public void Test_Array_Implements_Interface_Definition()
    {
        // Arrange
        var type = _module.ImportReference(typeof(ClassWithoutInterface[]));
        var interfaceType = _module.ImportReference(typeof(IEnumerable<>));

        // Act
        var result = type.ImplementsInterface(interfaceType);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Test_Array_Implements_Interface()
    {
        // Arrange
        var type = _module.ImportReference(typeof(ClassWithoutInterface[]));
        var interfaceType = _module.ImportReference(typeof(IEnumerable<ClassWithoutInterface>));
        var negativeInterfaceType = _module.ImportReference(typeof(IEnumerable<string>));

        // Act
        var result = type.ImplementsInterface(interfaceType);
        var negativeResult = type.ImplementsInterface(negativeInterfaceType);

        // Assert
        Assert.True(result);
        Assert.False(negativeResult);
    }

    [Theory]
    [InlineData(typeof(INonGenericInterface), InterfaceType.NonGenericInterface)]
    [InlineData(typeof(IGenericInterface<>), InterfaceType.GenericInterfaceDefinition)]
    [InlineData(typeof(IGenericInterface<string>), InterfaceType.GenericInterfaceWithTypeArgument)]
    [InlineData(typeof(IGenericInterface<int>), InterfaceType.GenericInterfaceWithTypeArgument)]
    public void Verify_Interface_Type(Type input, InterfaceType expectation)
    {
        var typeRef = _module.ImportReference(input);
        var interfaceType = typeRef.DetermineInterfaceType();
        Assert.Equal(expectation, interfaceType);
    }

    [Fact]
    public void DepictStupidity()
    {
        // var typeRef = _module.ImportReference(typeof(IGenericInterface<string>));
        // var typeRef = _module.ImportReference(typeof(ClassImplementingGenericInterface));
        // var interfaces = typeRef.Resolve().Interfaces;

        // var typeRef = _module.ImportReference(typeof(IEnumerable<Dictionary<string, int>>)) as GenericInstanceType;
        
    }
    
    // Helper types for testing
    public interface INonGenericInterface
    { }

    public interface IGenericInterface<T>
    { }

    public class ClassImplementingNonGenericInterface : INonGenericInterface
    { }

    public class ClassWithoutInterface
    { }

    public class ClassImplementingGenericInterface : IGenericInterface<string>
    { }

    public class BaseClassImplementingInterface : INonGenericInterface
    { }

    public class DerivedClassImplementingInterface : BaseClassImplementingInterface
    { }

    // IGenericInterface<int>
    public class ClassWithIntGerneric : IGenericInterface<int>
    { }

    // IGenericInterface<string>
    public class ClassWithStringGerneric : IGenericInterface<string>
    { }
}