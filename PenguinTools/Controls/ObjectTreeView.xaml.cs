using PenguinTools.Converters;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;

namespace PenguinTools.Controls;

public partial class ObjectTreeView : UserControl
{
    public static readonly DependencyProperty JsonRepresentationProperty = DependencyProperty.Register(nameof(JsonRepresentation), typeof(string), typeof(ObjectTreeView));
    public static readonly DependencyProperty SelectedObjectProperty = DependencyProperty.Register(nameof(SelectedObject), typeof(object), typeof(ObjectTreeView), new PropertyMetadata(null, OnObjectChanged));
    public static readonly DependencyProperty TreeNodesProperty = DependencyProperty.Register(nameof(TreeNodes), typeof(List<ObjectTreeNode>), typeof(ObjectTreeView), new PropertyMetadata(null));

    public ObjectTreeView()
    {
        InitializeComponent();
    }

    public string? JsonRepresentation
    {
        get => (string)GetValue(JsonRepresentationProperty);
        set => SetValue(JsonRepresentationProperty, value);
    }

    public object SelectedObject
    {
        get => GetValue(SelectedObjectProperty);
        set => SetValue(SelectedObjectProperty, value);
    }

    public List<ObjectTreeNode>? TreeNodes
    {
        get => (List<ObjectTreeNode>)GetValue(TreeNodesProperty);
        set => SetValue(TreeNodesProperty, value);
    }

    private static void OnObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ObjectTreeView otv) return;
        if (e.NewValue == null)
        {
            otv.TreeNodes = null;
            otv.JsonRepresentation = null;
            return;
        }
        var (json, tree) = ObjectTreeNode.CreateTree(e.NewValue);
        otv.TreeNodes = [tree];
        otv.JsonRepresentation = json;
    }

    private void MenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(JsonRepresentation))
        {
            return;
        }

        try
        {
            Clipboard.SetText(JsonRepresentation);
        }
        catch
        {
            // do nothing
        }
    }
}

public class ObjectTreeNode
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        Converters =
        {
            new ExceptionJsonConverter<Exception>()
        },
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    public string Name { get; set; } = string.Empty;
    public string? Value { get; set; }
    public List<ObjectTreeNode> Children { get; set; } = [];

    public static (string, ObjectTreeNode) CreateTree(object obj, string? rootName = null)
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
        return (json, root);
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