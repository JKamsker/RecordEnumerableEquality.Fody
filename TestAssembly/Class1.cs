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