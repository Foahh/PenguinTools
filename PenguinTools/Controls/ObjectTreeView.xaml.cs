using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;

namespace PenguinTools.Controls;

public partial class ObjectTreeView : UserControl
{
    public static readonly DependencyProperty SelectedObjectProperty = DependencyProperty.Register(nameof(SelectedObject), typeof(object), typeof(ObjectTreeView), new PropertyMetadata(null, OnObjectChanged));
    public static readonly DependencyProperty TreeNodesProperty = DependencyProperty.Register(nameof(TreeNodes), typeof(List<ObjectTreeNode>), typeof(ObjectTreeView), new PropertyMetadata(null));

    public ObjectTreeView()
    {
        InitializeComponent();
    }

    public object SelectedObject
    {
        get => GetValue(SelectedObjectProperty);
        set => SetValue(SelectedObjectProperty, value);
    }

    public List<ObjectTreeNode> TreeNodes
    {
        get => (List<ObjectTreeNode>)GetValue(TreeNodesProperty);
        set => SetValue(TreeNodesProperty, value);
    }

    private static void OnObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var tree = ObjectTreeNode.CreateTree(e.NewValue);
        if (d is not ObjectTreeView otv) return;
        otv.TreeNodes = [tree];
    }
}

public class ExceptionJsonConverter<TExceptionType> : JsonConverter<TExceptionType> where TExceptionType : Exception
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(Exception).IsAssignableFrom(typeToConvert);
    }

    public override TExceptionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    public override void Write(Utf8JsonWriter writer, TExceptionType value, JsonSerializerOptions options)
    {
        var properties = value.GetType().GetProperties().Select(uu => new { uu.Name, Value = uu.GetValue(value) }).Where(uu => uu.Name != nameof(Exception.TargetSite));
        if (options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull) properties = properties.Where(uu => uu.Value != null);
        var props = properties.ToList();
        if (props.Count == 0) return;

        writer.WriteStartObject();
        foreach (var prop in props)
        {
            writer.WritePropertyName(prop.Name);
            JsonSerializer.Serialize(writer, prop.Value, options);
        }
        writer.WriteEndObject();
    }
}

public class ObjectTreeNode
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        Converters = { new ExceptionJsonConverter<Exception>() },
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    public string Name { get; set; } = string.Empty;
    public string? Value { get; set; }
    public List<ObjectTreeNode> Children { get; set; } = [];

    public static ObjectTreeNode CreateTree(object obj, string? rootName = null)
    {
        var json = JsonSerializer.Serialize(obj, JsonSerializerOptions);
        using var doc = JsonDocument.Parse(json);
        var rootElement = doc.RootElement;
        var root = new ObjectTreeNode
        {
            Name = rootName ?? obj.GetType().Name,
            Value = GetValueString(rootElement)
        };
        BuildTree(rootElement, root);
        return root;
    }

    private static void BuildTree(JsonElement element, ObjectTreeNode node)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                var child = new ObjectTreeNode
                {
                    Name = prop.Name,
                    Value = GetValueString(prop.Value)
                };
                node.Children.Add(child);
                BuildTree(prop.Value, child);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in element.EnumerateArray())
            {
                var child = new ObjectTreeNode
                {
                    Name = $"[{index}]",
                    Value = GetValueString(item)
                };
                node.Children.Add(child);
                BuildTree(item, child);
                index++;
            }
        }
    }

    private static string? GetValueString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True or JsonValueKind.False => element.GetBoolean().ToString(),
            JsonValueKind.Null => "null",
            JsonValueKind.Object => "{}",
            JsonValueKind.Array => $"[{element.GetArrayLength()}]",
            _ => element.ToString()
        };
    }
}