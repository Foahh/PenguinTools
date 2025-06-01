using PenguinTools.Attributes;
using PenguinTools.Common.Metadata;
using PenguinTools.Common.Resources;
using System.ComponentModel.DataAnnotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace PenguinTools.Models;

public class TimeSignatureModel(Meta meta) : Model
{
    [PropertyOrder(0)]
    [LocalizableDisplayName(nameof(Strings.Display_Numerator), typeof(Strings))]
    [Range(1, short.MaxValue)]
    public int Numerator
    {
        get => meta.BgmInitialTimeSignature.Numerator;
        set => SetProperty(meta.BgmInitialTimeSignature.Numerator, value, newValue => meta.BgmInitialTimeSignature = meta.BgmInitialTimeSignature with
        {
            Numerator = newValue
        }, true);
    }

    [PropertyOrder(1)]
    [LocalizableDisplayName(nameof(Strings.Display_Denominator), typeof(Strings))]
    [Range(1, short.MaxValue)]
    public int Denominator
    {
        get => meta.BgmInitialTimeSignature.Denominator;
        set => SetProperty(meta.BgmInitialTimeSignature.Denominator, value, newValue => meta.BgmInitialTimeSignature = meta.BgmInitialTimeSignature with
        {
            Denominator = newValue
        }, true);
    }

    public override string ToString()
    {
        return $"{Numerator}/{Denominator}";
    }
}