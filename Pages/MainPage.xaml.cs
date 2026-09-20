namespace Docklet.Pages;

/// <summary>
/// 左メニューを表示し、選択に応じてページを切り替えるクラス
/// </summary>
public sealed partial class MainPage : Page
{
    /// <summary>
    /// メイン ページを初期化します。
    /// </summary>
    public MainPage()
    {
        InitializeComponent();
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyLanguage();
        LocalizationService.LanguageChanged += OnLanguageChanged;
        if (NavView.SelectedItem is null)
        {
            NavView.SelectedItem = NavView.MenuItems[0];
        }
    }

    private void NavView_Unloaded(object sender, RoutedEventArgs e)
    {
        LocalizationService.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        ApplyLanguage();
        if (ContentFrame.CurrentSourcePageType is Type pageType)
        {
            ContentFrame.Navigate(pageType);
        }
    }

    private void ApplyLanguage()
    {
        HomeNavItem.Content = LocalizationService.GetString("Nav_Home/Content");
        ContainersNavItem.Content = LocalizationService.GetString("Nav_Containers/Content");
        ImagesNavItem.Content = LocalizationService.GetString("Nav_Images/Content");
        VolumesNavItem.Content = LocalizationService.GetString("Nav_Volumes/Content");
        NetworksNavItem.Content = LocalizationService.GetString("Nav_Networks/Content");
        if (NavView.SettingsItem is NavigationViewItem settingsItem)
        {
            settingsItem.Content = LocalizationService.GetString("Nav_Settings");
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        Type pageType;
        if (args.IsSettingsSelected)
        {
            pageType = typeof(SettingsPage);
        }
        else if (args.SelectedItemContainer?.Tag is string tag)
        {
            pageType = tag switch
            {
                "home" => typeof(HomePage),
                "containers" => typeof(ContainersPage),
                "images" => typeof(ImagesPage),
                "volumes" => typeof(VolumesPage),
                "networks" => typeof(NetworksPage),
                _ => typeof(HomePage)
            };
        }
        else
        {
            return;
        }

        if (ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.Navigate(pageType);
            ContentFrame.BackStack.Clear();
            UpdateBackButton();
        }
    }

    /// <summary>
    /// 1 つ前の画面へ戻ります。
    /// </summary>
    /// <returns>戻れたとき true</returns>
    public bool TryGoBack()
    {
        if (!ContentFrame.CanGoBack)
        {
            return false;
        }

        ContentFrame.GoBack();
        return true;
    }

    private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
    {
        UpdateBackButton();
    }

    private void UpdateBackButton()
    {
        Docklet.App.Current.Window?.SetBackEnabled(ContentFrame.CanGoBack);
    }
}
