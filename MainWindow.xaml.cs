using Docklet.Pages;

namespace Docklet;

/// <summary>
/// タイトル バーとページ表示用フレームを持つメイン ウィンドウ
/// </summary>
public sealed partial class MainWindow : Window
{
    /// <summary>
    /// メイン ウィンドウを初期化します。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"));

        RootFrame.Navigate(typeof(MainPage));
    }

    /// <summary>
    /// タイトル バーの戻るボタンの有効／無効を切り替えます。
    /// </summary>
    /// <param name="enabled">戻れるとき true</param>
    public void SetBackEnabled(bool enabled)
    {
        AppTitleBar.IsBackButtonEnabled = enabled;
    }

    private void AppTitleBar_BackRequested(TitleBar sender, object args)
    {
        if (RootFrame.Content is MainPage main)
        {
            main.TryGoBack();
        }
    }
}
