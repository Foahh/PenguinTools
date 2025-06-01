using Microsoft.Extensions.DependencyInjection;
using PenguinTools.Common.Asset;
using PenguinTools.Common.Metadata;
using PenguinTools.Models;
using System.ComponentModel;
using Xceed.Wpf.Toolkit.PropertyGrid;

namespace PenguinTools.Controls;

public partial class MetaPropertyGrid : MyPropertyGrid
{
    private readonly HashSet<PropertyDefinition> hidden = [];

    public MetaPropertyGrid()
    {
        InitializeComponent();
        SelectedObjectChanged += (s, e) =>
        {
            if (e.OldValue is Model oldModel) oldModel.PropertyChanged -= Model_PropertyChanged;
            if (e.NewValue is not Model newModel) return;
            newModel.PropertyChanged += Model_PropertyChanged;
            ControlPropertyVisibility(newModel);
        };
    }

    public AssetManager AssetManager => App.Services.GetRequiredService<AssetManager>();

    private void Model_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not Model model) return;
        if (e.PropertyName is nameof(Meta.Difficulty) or nameof(Meta.IsCustomStage)) ControlPropertyVisibility(model);
    }

    private void RestorePropertyGrid()
    {
        var currentDefs = new HashSet<PropertyDefinition>(PropertyDefinitions);
        var defsToAdd = hidden.Where(def => !currentDefs.Contains(def)).ToList();
        foreach (var def in defsToAdd) PropertyDefinitions.Add(def);
        hidden.Clear();
    }

    private void ControlPropertyVisibility(Model? model)
    {
        if (model == null) return;
        RestorePropertyGrid();

        var csProperty = model.GetType().GetProperty(nameof(Meta.IsCustomStage));
        if (csProperty != null)
        {
            var value = (bool)(csProperty.GetValue(model) ?? false);
            if (value) HidePropertyDefinitions(nameof(Meta.Stage));
            else HidePropertyDefinitions(nameof(Meta.StageId), nameof(Meta.NotesFieldLine), nameof(Meta.BgiFilePath));
        }

        var diffProperty = model.GetType().GetProperty(nameof(Meta.Difficulty));
        if (diffProperty != null)
        {
            var value = diffProperty.GetValue(model);
            if (value is not Difficulty diff) return;
            if (diff == Difficulty.WorldsEnd) HidePropertyDefinitions(nameof(Meta.Level));
            else HidePropertyDefinitions(nameof(Meta.WeTag), nameof(Meta.WeDifficulty));
            if (diff is not (Difficulty.Ultima or Difficulty.WorldsEnd)) HidePropertyDefinitions(nameof(Meta.UnlockEventId));
        }
    }

    private void HidePropertyDefinitions(params string[] names)
    {
        if (names.Length == 0) return;
        var nameSet = new HashSet<string>(names, StringComparer.Ordinal);
        var toRemove = PropertyDefinitions.Where(def => def.TargetProperties.Cast<string>().Any(nameSet.Contains)).ToList();
        foreach (var def in toRemove.Where(def => hidden.Add(def))) PropertyDefinitions.Remove(def);
    }
}