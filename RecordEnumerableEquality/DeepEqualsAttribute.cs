using System;

namespace RecordEnumerableEquality;

public class DeepEqualsAttribute : Attribute
{
    public bool Enabled { get; set; }
    
    public DeepEqualsAttribute(bool enabled = true)
    {
        Enabled = enabled;
    }
}