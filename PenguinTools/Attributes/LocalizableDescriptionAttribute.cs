using System.ComponentModel;

namespace PenguinTools.Attributes;

[AttributeUsage(AttributeTargets.All)]
public class LocalizableDescriptionAttribute(string descriptionKey, Type resourceType) : DescriptionAttribute(descriptionKey)
{
    public Type ResourceType { get; } = resourceType;

    public override string Description => ResourceType.GetPropertyValue(base.Description, base.Description) ?? base.Description;
}