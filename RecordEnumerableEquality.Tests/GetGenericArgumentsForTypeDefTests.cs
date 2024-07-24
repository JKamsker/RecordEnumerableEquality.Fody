using RecordEnumerableEquality.Fody;
using Xunit;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;

namespace Tests;

public class GetGenericArgumentsForTypeDefTests
{
    private ModuleDefinition module;

    public GetGenericArgumentsForTypeDefTests()
    {
        // Load a module for testing
        // module = ModuleDefinition.ReadModule(typeof(TypeReferenceExtensionsTests).Module.FullyQualifiedName);
        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(Path.GetDirectoryName(typeof(object).Assembly.Location));
        var readerParameters = new ReaderParameters { AssemblyResolver = resolver };
        module = ModuleDefinition.ReadModule(typeof(GetGenericArgumentsForTypeDefTests).Module.FullyQualifiedName, readerParameters);
    }

    [Fact]
    public void GetGenericArgumentsForInterface_ListOfString_ImplementsIEnumerableOfString()
    {
        // Arrange
        var listStringType = module.ImportReference(typeof(List<string>));
        var iEnumerableType = module.ImportReference(typeof(IEnumerable<>));

        // Act
        var genericArguments = listStringType.GetGenericArgumentsForInterface(iEnumerableType).FirstOrDefault();

        // Assert
        Assert.NotNull(genericArguments);
        Assert.Single(genericArguments);
        Assert.Equal("System.String", genericArguments[0].FullName);
    }
    
    
    [Fact]
    public void GetGenericArgumentsForInterface_ForTypeDef_Returns_GenericArguments()
    {
        // Arrange
        var listStringType = module.ImportReference(typeof(List<string>));
        var iEnumerableType = module.ImportReference(typeof(List<>));

        // Act
        var allGenericArguments = listStringType
                .GetGenericArgumentsForInterface(iEnumerableType);
        
        var genericArguments = allGenericArguments.SingleOrDefault();

        // Assert
        Assert.NotNull(genericArguments);
        Assert.Single(genericArguments);
        Assert.Equal("System.String", genericArguments[0].FullName);
    }

    [Fact]
    public void GetGenericArgumentsForInterface_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        TypeReference nullType = null;
        var iEnumerableType = module.ImportReference(typeof(IEnumerable<>));

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullType.GetGenericArgumentsForInterface(iEnumerableType));
    }

    [Fact]
    public void GetGenericArgumentsForInterface_NullInterfaceType_ThrowsArgumentNullException()
    {
        // Arrange
        var listStringType = module.ImportReference(typeof(List<string>));
        TypeReference nullInterfaceType = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => listStringType.GetGenericArgumentsForInterface(nullInterfaceType));
    }

    [Fact]
    public void GetGenericArgumentsForInterface_ClassImplementsMultipleInterfaces_ReturnsCorrectGenericArguments()
    {
        // Arrange
        var dictionaryType = module.ImportReference(typeof(Dictionary<int, string>));
        var iEnumerableType = module.ImportReference(typeof(IEnumerable<>));

        // Act
        var allGenericArguments = dictionaryType.GetGenericArgumentsForInterface(iEnumerableType);
        var genericArguments = allGenericArguments.FirstOrDefault();

        // Assert
        Assert.NotNull(genericArguments);
        Assert.Single(genericArguments);
        Assert.Equal("System.Collections.Generic.KeyValuePair`2<System.Int32,System.String>", genericArguments[0].FullName);
    }

    [Fact]
    public void GetGenericArgumentsForInterface_NonGenericInterface_ImplementsGenericInterface()
    {
        // Arrange
        var customType = module.ImportReference(typeof(CustomType));
        var iCustomInterface = module.ImportReference(typeof(ICustomInterface<>));

        // Act
        var genericArguments = customType.GetGenericArgumentsForInterface(iCustomInterface).FirstOrDefault();

        // Assert
        Assert.NotNull(genericArguments);
        Assert.Single(genericArguments);
        Assert.Equal("System.String", genericArguments[0].FullName);
    }

    // Given ICustomInterface<string>, expect string
    [Fact]
    public void GetGenericArgumentsForInterface_GenericInterfaceWithTypeArgument_ReturnsCorrectGenericArguments()
    {
        // Arrange
        var customType = module.ImportReference(typeof(ICustomInterface<string>));
        var iCustomInterface = module.ImportReference(typeof(ICustomInterface<>));

        // Act
        var genericArguments = customType.GetGenericArgumentsForInterface(iCustomInterface).FirstOrDefault();

        // Assert
        Assert.NotNull(genericArguments);
        Assert.Single(genericArguments);
        Assert.Equal("System.String", genericArguments[0].FullName);
    }

    public interface ICustomInterface<T>
    { }

    public class CustomType : ICustomInterface<string>
    { }
}