using System;
using System.Diagnostics;
using System.Reflection;

using RecordEnumerableEquality.Fody;

using Fody;

using Xunit;
using Mono.Cecil;

namespace RecordEnumerableEquality.Tests;

public class WeaverTests
{
    [Fact]
    public void Validate1()
    {
        var weavingTask = new ModuleWeaver()
        {
            ResolveAssembly = (assemblyName) =>
            {
                return AssemblyDefinition.ReadAssembly(assemblyName);
            },
        };
        TestResult testResult = weavingTask.ExecuteTestRun("TestAssembly.dll", runPeVerify: false);

        Val(testResult);
    }

    private static void Val(TestResult testResult)
    {
        var mainType = testResult.Assembly.GetType("TestAssembly.MainClass");
        var subType = testResult.Assembly.GetType("TestAssembly.SubClass");

        var main1 = CreateMainInstance(mainType, subType);
        var main2 = CreateMainInstance(mainType, subType);
        var eq = main1.Equals(main2);

        // GetHashCode
        var hash1 = main1.GetHashCode();
        var hash2 = main2.GetHashCode();

        Assert.Equal(hash1, hash2);

        Assert.True(eq);
    }

    private static dynamic CreateMainInstance(Type mainType, Type subType)
    {
        var instance = (dynamic)Activator.CreateInstance(mainType);
        instance.Property1 = "Hello World";

        var subInstance = CreateSubInstance(subType);
        var subInstance2 = CreateSubInstance(subType, "HelloX", "WorldX!");

        instance.SubClasses.Add(subInstance);
        instance.SubClasses.Add(subInstance2);

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