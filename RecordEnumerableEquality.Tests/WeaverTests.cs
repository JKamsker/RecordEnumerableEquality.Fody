using Fody;
using Mono.Cecil;
using RecordEnumerableEquality.Fody;
using RecordEnumerableEquality.Tests.Utils;
using System.Collections;

namespace RecordEnumerableEquality.Tests;

public partial class WeaverTests
{
    [Theory]
    [InlineData("List", "new List<SubClass>()")]
    [InlineData("HashSet", "new HashSet<SubClass>()")]
    [InlineData("Array", "null")]
    [InlineData("IEnumerable", "new List<SubClass>()")]
    [InlineData("ICollection", "new List<SubClass>()")]
    public void TestDifferentCollectionTypes(string collectionType, string collectionInitialization)
    {
        var realCollectionType = collectionType == "Array" ? "SubClass[]" : $"{collectionType}<SubClass>";

        string code = $$"""
                        using System;
                        using System.Collections.Generic;

                        namespace TestAssembly
                        {
                            public record MainClass
                            {
                                public string Property1 { get; set; }
                        
                                public {{realCollectionType}} SubClasses { get; set; } = {{collectionInitialization}};
                            }
                        
                            public record SubClass
                            {
                                public string Property2 { get; set; }
                                public string Property3 { get; set; }
                            }
                        }
                        """;

        var weavingTask = new ModuleWeaver();
        using var assembly = AssemblyGenerator.Generate(code);
        TestResult testResult = weavingTask.ExecuteTestRun(assembly.Location, runPeVerify: false);

        OpenDnSpyIfFailed(testResult, ShouldBeEqual);
    }

    // Test a custom collection type
    [Fact]
    public void TestCustomCollectionType()
    {
        string code = """
                      using System;
                      using System.Collections.Generic;

                      namespace TestAssembly
                      {
                          public record MainClass
                          {
                              public string Property1 { get; set; }
                      
                              public CustomCollection<SubClass> SubClasses { get; set; } = new CustomCollection<SubClass>();
                          }
                      
                          public record SubClass
                          {
                              public string Property2 { get; set; }
                              public string Property3 { get; set; }
                          }
                      
                          public class CustomCollection<T> : List<T>
                          {
                          }
                      }
                      """;

        var weavingTask = new ModuleWeaver();
        using var assembly = AssemblyGenerator.Generate(code);
        TestResult testResult = weavingTask.ExecuteTestRun(assembly.Location, runPeVerify: false);

        OpenDnSpyIfFailed(testResult, ShouldBeEqual);
    }
    
    private static void OpenDnSpyIfFailed(TestResult testResult, Action<TestResult> action)
    {
        try
        {
            action(testResult);
        }
        catch (Exception)
        {
            // testResult.OpenInDnSpy();
            throw;
        }
    }
    
    private static void ShouldBeEqual(TestResult testResult)
    {
        var (eq, hash1, hash2) = ValidateResults(testResult);

        Assert.Equal(hash1, hash2);
        Assert.True(eq);
    }
    
    private static void ShouldNotBeEqual(TestResult testResult)
    {
        var (eq, hash1, hash2) = ValidateResults(testResult);

        Assert.False(eq);
        Assert.NotEqual(hash1, hash2);
    }

    private static (bool eq, int hash1, int hash2) ValidateResults(TestResult testResult)
    {
        var mainType = testResult.Assembly.GetType("TestAssembly.MainClass");
        var subType = testResult.Assembly.GetType("TestAssembly.SubClass");

        var main1 = CreateMainInstance(mainType, subType);
        var main2 = CreateMainInstance(mainType, subType);
        var eq = (bool)main1.Equals(main2);

        // GetHashCode
        var hash1 = (int)main1.GetHashCode();
        var hash2 = (int)main2.GetHashCode();
        return (eq, hash1, hash2);
    }

    private static dynamic CreateMainInstance(Type mainType, Type subType)
    {
        var instance = (dynamic)Activator.CreateInstance(mainType);
        instance.Property1 = "Hello World";

        var subInstance = CreateSubInstance(subType);
        var subInstance2 = CreateSubInstance(subType, "HelloX", "WorldX!");
        if (instance.SubClasses is IEnumerable)
        {
            instance.SubClasses.Add(subInstance);
            instance.SubClasses.Add(subInstance2);
            return instance;
        }

        // Get the SubClasses property info
        var subClassesProperty = mainType.GetProperty("SubClasses");
        if (subClassesProperty == null)
        {
            throw new InvalidOperationException("The main type does not contain a SubClasses property.");
        }

        // Determine the type of SubClasses
        var subClassesType = subClassesProperty.PropertyType;

        // Initialize SubClasses if necessary

        if (subClassesType.IsArray)
        {
            var newSubClassesArray = Array.CreateInstance(subType, 2);

            newSubClassesArray.SetValue(subInstance, 0);
            newSubClassesArray.SetValue(subInstance2, 1);

            subClassesProperty.SetValue(instance, newSubClassesArray);
        }
        else if (typeof(IList).IsAssignableFrom(subClassesType))
        {
            if (instance.SubClasses == null)
            {
                instance.SubClasses = Activator.CreateInstance(typeof(List<>).MakeGenericType(subType));
            }

            // Add sub instances to the list
            ((IList)instance.SubClasses).Add(subInstance);
            ((IList)instance.SubClasses).Add(subInstance2);
        }
        else if (typeof(ICollection).IsAssignableFrom(subClassesType))
        {
            if (instance.SubClasses == null)
            {
                //instance.SubClasses = Activator.CreateInstance(typeof(Collection<>).MakeGenericType(subType));
                instance.SubClasses = Activator.CreateInstance(subClassesType);
            }

            // Add sub instances to the list
            ((IList)instance.SubClasses).Add(subInstance);
            ((IList)instance.SubClasses).Add(subInstance2);
        }
        else
        {
            throw new InvalidOperationException("SubClasses must be a list or an array.");
        }

        return instance;
    }

    private static dynamic CreateSubInstance(Type subType, string property2 = "Hello", string property3 = "World!")
    {
        var subInstance = (dynamic)Activator.CreateInstance(subType);
        subInstance.Property2 = property2;
        subInstance.Property3 = property3;
        return subInstance;
    }
}