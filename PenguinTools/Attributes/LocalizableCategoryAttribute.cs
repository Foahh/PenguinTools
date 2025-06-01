using System.ComponentModel;

namespace PenguinTools.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class LocalizableCategoryAttribute(string categoryKey, Type resourceType) : CategoryAttribute(categoryKey)
{
    public Type ResourceType { get; } = resourceType;

    protected override string? GetLocalizedString(string value)
    {
        return ResourceType.GetPropertyValue(value, base.GetLocalizedString(value));
    }
}