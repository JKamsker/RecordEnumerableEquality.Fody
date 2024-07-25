using System;
using System.Xml.Linq;

namespace RecordEnumerableEquality.Fody;

public class ModuleWeaverSettings
{
    public DefaultBehavior DefaultBehavior { get; set; } = DefaultBehavior.Enabled;

    public ModuleWeaverSettings()
    {
    }

    public static ModuleWeaverSettings Load(XElement xElement)
    {
        var settings = new ModuleWeaverSettings();
        if (xElement == null)
        {
            return settings;
        }

        var defaultBehavior = xElement.Attribute(nameof(DefaultBehavior));
        if (defaultBehavior != null)
        {
            settings.DefaultBehavior = (DefaultBehavior)Enum.Parse(typeof(DefaultBehavior), defaultBehavior.Value);
        }

        return settings;
    }
}

public enum DefaultBehavior
{
    /// <summary>
    /// Enables the Weaver to deep compare Enumerables for all records by default
    /// To disable the deep comparison for a specific record, or property, use the [RecordEnumerableEquality(RecordEnumerableEqualityBehavior.Disabled)] attribute
    /// </summary>
    Enabled,

    /// <summary>
    /// Disables the Weaver to deep compare Enumerables for all records by default
    /// To enable the deep comparison for a specific record, or property, use the [RecordEnumerableEquality(RecordEnumerableEqualityBehavior.Enabled)] attribute
    /// </summary>
    Disabled
}