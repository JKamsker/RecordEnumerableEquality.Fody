using System.Xml.Linq;
using Fody;
using RecordEnumerableEquality.Fody;
using RecordEnumerableEquality.Tests.Utils;

namespace RecordEnumerableEquality.Tests;

public partial class WeaverTests
{
    [Fact]
    public void Global_Behaviour_Respected()
    {
        const string code =
            """
            using System;
            using System.Collections.Generic;

            namespace TestAssembly;

            public record MainClass
            {
                public string Property1 { get; set; }
                
                public List<SubClass> SubClasses { get; set; } = new List<SubClass>();
            }

            public record SubClass
            {
                public string Property2 { get; set; }
                public string Property3 { get; set; }
            }
            """;

        var xElement = XElement.Parse("<RecordEnumerableEquality DefaultBehavior='Disabled' />");
        var weaver = new ModuleWeaver { Config = xElement };
        using var assembly = AssemblyGenerator.Generate(code);
        var testResult = weaver.ExecuteTestRun(assembly.Location, runPeVerify: false);

        OpenDnSpyIfFailed(testResult, ShouldNotBeEqual);
    }
    
    
    [Fact]
    public void Attribute_Overrides_Global_Behaviour()
    {
        const string code =
            """
            using System;
            using System.Collections.Generic;
            using RecordEnumerableEquality;

            namespace TestAssembly;

            public record MainClass
            {
                public string Property1 { get; set; }
                
                [DeepEquals]
                public List<SubClass> SubClasses { get; set; } = new List<SubClass>();
            }

            public record SubClass
            {
                public string Property2 { get; set; }
                public string Property3 { get; set; }
            }
            """;

        var xElement = XElement.Parse("<RecordEnumerableEquality DefaultBehavior='Disabled' />");
        var weaver = new ModuleWeaver { Config = xElement };
        using var assembly = AssemblyGenerator.Generate(code);
        var testResult = weaver.ExecuteTestRun(assembly.Location, runPeVerify: false);

        OpenDnSpyIfFailed(testResult, ShouldBeEqual);
    }
    
    [Fact]
    public void Attribute_Overrides_Global_Behaviour_Inverted()
    {
        const string code =
            """
            using System;
            using System.Collections.Generic;
            using RecordEnumerableEquality;

            namespace TestAssembly;

            public record MainClass
            {
                public string Property1 { get; set; }
                
                [DeepEquals(false)]
                public List<SubClass> SubClasses { get; set; } = new List<SubClass>();
            }

            public record SubClass
            {
                public string Property2 { get; set; }
                public string Property3 { get; set; }
            }
            """;

        var xElement = XElement.Parse("<RecordEnumerableEquality DefaultBehavior='Enabled' />");
        var weaver = new ModuleWeaver { Config = xElement };
        using var assembly = AssemblyGenerator.Generate(code);
        var testResult = weaver.ExecuteTestRun(assembly.Location, runPeVerify: false);

        OpenDnSpyIfFailed(testResult, ShouldNotBeEqual);
    }
}