namespace Docklet.Pages;

/// <summary>
/// 設定画面
/// </summary>
public sealed partial class SettingsPage : Page
{
    private bool _suppressLanguageChange;
    private bool _suppressBackendChange;

    /// <summary>
    /// 設定画面を初期化します。
    /// </summary>
    public SettingsPage()
    {
        InitializeComponent();
        VersionText.Text = $"Docklet {typeof(App).Assembly.GetName().Version?.ToString(3)}";
        SelectCurrentLanguage();
        SelectCurrentBackend();
    }

    private void SelectCurrentBackend()
    {
        _suppressBackendChange = true;
        var current = AppSettings.Backend.ToString();
        foreach (var item in BackendComboBox.Items.OfType<ComboBoxItem>())
        {
            if (item.Tag is string tag && tag == current)
            {
                BackendComboBox.SelectedItem = item;
                break;
            }
        }

        _suppressBackendChange = false;
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

    private async void BackendComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressBackendChange
            || BackendComboBox.SelectedItem is not ComboBoxItem { Tag: string tag }
            || !Enum.TryParse<ContainerBackend>(tag, out var backend)
            || backend == AppSettings.Backend)
        {
            return;
        }

        AppSettings.Backend = backend;

        // 切り替え前の接続 (WSL の Agent) を破棄する。Docker に戻した場合は次の操作で再起動される。
        await App.Current.Agent.DisposeAsync();
    }
}
