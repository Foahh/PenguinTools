using System.Windows;
using PenguinTools.i18n;

namespace PenguinTools.Views;

public partial class UserAssetSetupWindow : Window
{
    public UserAssetSetupWindow()
    {
        InitializeComponent();
        Title = Strings.UserAssetSetup_Title;
    }
}