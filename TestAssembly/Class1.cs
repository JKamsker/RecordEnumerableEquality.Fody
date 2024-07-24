using System;
using System.Collections.Generic;

namespace TestAssembly;

public record MainClass
{
    public string Property1 { get; set; }

    public IEnumerable<SubClass> SubClasses { get; set; } = new HashSet<SubClass>();
}

public record SubClass
{
    public string Property2 { get; set; }
    public string Property3 { get; set; }
}

//public record MainClassEnumerables
//{
//    public string Property1 { get; set; }

//    public IEnumerable<SubClass> SubClasses { get; set; } = new List<SubClass>();
//}