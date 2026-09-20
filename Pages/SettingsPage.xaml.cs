namespace Docklet.Pages;

/// <summary>
/// 設定画面
/// </summary>
public sealed partial class SettingsPage : Page
{
    private bool _suppressLanguageChange;

    /// <summary>
    /// 設定画面を初期化します。
    /// </summary>
    public SettingsPage()
    {
        InitializeComponent();
        VersionText.Text = $"Docklet {typeof(App).Assembly.GetName().Version?.ToString(3)}";
        SelectCurrentLanguage();
    }

    private void SelectCurrentLanguage()
    {
        _suppressLanguageChange = true;
        var current = LocalizationService.CurrentLanguage;
        foreach (var item in LanguageComboBox.Items.OfType<ComboBoxItem>())
        {
            if (item.Tag is string tag && string.Equals(tag, current, StringComparison.OrdinalIgnoreCase))
            {
                LanguageComboBox.SelectedItem = item;
                break;
            }
        }

        _suppressLanguageChange = false;
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressLanguageChange)
        {
            return;
        }

        if (LanguageComboBox.SelectedItem is ComboBoxItem { Tag: string language })
        {
            LocalizationService.SetLanguage(language);
        }
    }
}
