using System.ComponentModel;
using System.Reflection;
using Xceed.Wpf.Toolkit.PropertyGrid;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace PenguinTools.Controls;

public class MyPropertyGrid : PropertyGrid
{
    public MyPropertyGrid()
    {
        SelectedObjectChanged += (_, _) =>
        {
            var type = SelectedObject?.GetType();
            if (type == Target) return;
            Target = type;
            GeneratePropertyDefinitions();
        };
        AutoGenerateProperties = false;
    }

    public Type? Target { get; set; }

    private void GeneratePropertyDefinitions()
    {
        if (Target == null)
        {
            PropertyDefinitions.Clear();
            return;
        }

        PropertyDefinitions.Clear();

        var definitions = PropertyDefinitions.Where(def => def.TargetProperties != null);
        var existingProperties = definitions.SelectMany(def => def.TargetProperties.OfType<string>()).ToHashSet(StringComparer.Ordinal);
        var metaProps = Target.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        for (var i = 0; i < metaProps.Length; i++)
        {
            var prop = metaProps[i];
            if (existingProperties.Contains(prop.Name)) continue;
            var order = prop.GetCustomAttribute<PropertyOrderAttribute>();
            var newDef = new PropertyDefinition
            {
                DisplayOrder = order?.Order ?? i,
                TargetProperties = new[] { prop.Name },
                Description = prop.GetCustomAttribute<DescriptionAttribute>()?.Description,
                Category = prop.GetCustomAttribute<CategoryAttribute>()?.Category,
                DisplayName = prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? prop.Name,
                IsBrowsable = prop.GetCustomAttribute<BrowsableAttribute>()?.Browsable ?? true,
                IsExpandable = prop.GetCustomAttribute<ExpandableObjectAttribute>() != null
            };
            PropertyDefinitions.Add(newDef);
        }
    }
}