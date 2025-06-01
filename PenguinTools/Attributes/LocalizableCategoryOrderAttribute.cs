using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace PenguinTools.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class LocalizableCategoryOrderAttribute : CategoryOrderAttribute
{
    public LocalizableCategoryOrderAttribute(string orderKey, int order, Type resourceType) : base(orderKey, order)
    {
        ResourceType = resourceType;
        var propertyInfo = typeof(CategoryOrderAttribute).GetProperty("CategoryValue");
        propertyInfo?.SetValue(this, ResourceType.GetPropertyValue(orderKey, orderKey));
    }

    public Type ResourceType { get; }
}