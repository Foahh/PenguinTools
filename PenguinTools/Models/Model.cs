using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using PenguinTools.Core.Metadata;
using umgr = PenguinTools.Chart.Models.umgr;

namespace PenguinTools.Models;

public abstract class Model : ObservableValidator
{
    // workaround for hiding ObservableValidator's property
    [Browsable(false)] [JsonIgnore] public new bool HasErrors { get; set; }

    protected void SetPropertyReadOnly(string propertyName, bool readOnly)
    {
        var descriptor = TypeDescriptor.GetProperties(GetType())[propertyName];
        if (descriptor == null)
            throw new ArgumentException($"Property '{propertyName}' not found on type '{GetType().Name}'.");
        if (descriptor.Attributes[typeof(ReadOnlyAttribute)] is not ReadOnlyAttribute attribute)
            throw new InvalidOperationException($"Property '{propertyName}' does not have a ReadOnlyAttribute.");
        var rf = GetBackingField(typeof(ReadOnlyAttribute).GetProperty(nameof(ReadOnlyAttribute.IsReadOnly))!);
        if (rf == null)
            throw new InvalidOperationException($"Property '{propertyName}' does not have a backing field.");
        rf.SetValue(attribute, readOnly);
    }

    private static FieldInfo? GetBackingField(PropertyInfo pi)
    {
        if (!pi.CanRead) return null;
        var getMethod = pi.GetGetMethod(true);
        if (getMethod == null || !getMethod.IsDefined(typeof(CompilerGeneratedAttribute), true)) return null;
        var flags = BindingFlags.NonPublic | (getMethod.IsStatic ? BindingFlags.Static : BindingFlags.Instance);
        var bf = pi.DeclaringType?.GetField($"<{pi.Name}>k__BackingField", flags);
        if (bf == null) return null;
        return bf.IsDefined(typeof(CompilerGeneratedAttribute), true) ? bf : null;
    }
}

public abstract class MetaModel : Model
{
    [Browsable(false)] public abstract umgr.Chart Mgxc { get; }

    [Browsable(false)] public Meta Meta => Mgxc.Meta;
}