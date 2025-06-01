using System.ComponentModel;

namespace PenguinTools.Attributes;

[AttributeUsage(AttributeTargets.All)]
public class LocalizableDisplayNameAttribute(string descriptionKey, Type resourceType) : DisplayNameAttribute(descriptionKey)
{
    public Type ResourceType { get; } = resourceType;

    public override string DisplayName => ResourceType.GetPropertyValue(base.DisplayName, base.DisplayName) ?? base.DisplayName;
}