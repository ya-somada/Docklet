using System.Diagnostics;
using Docklet.Services.Docker;

namespace Docklet;

/// <summary>
/// アプリケーション全体の起動とウィンドウ管理を行うクラス
/// </summary>
public partial class App : Application
{
    private Window? _window;

    /// <summary>
    /// 現在のアプリケーションを返します。
    /// </summary>
    public static new App Current => (App)Application.Current;

    /// <summary>
    /// メイン ウィンドウ
    /// </summary>
    public MainWindow? Window => _window as MainWindow;

    /// <summary>
    /// Agent クライアント
    /// </summary>
    public AgentClient Agent { get; } = new();

    /// <summary>
    /// Agent 経由の Docker クライアント
    /// </summary>
    public DockerService Docker { get; }

    /// <summary>
    /// アプリケーションを初期化します。
    /// </summary>
    public App()
    {
        LocalizationService.Initialize();
        UnhandledException += App_UnhandledException;
        InitializeComponent();
        Docker = new DockerService(Agent);
    }

    /// <summary>
    /// アプリケーション起動時にメイン ウィンドウを表示し、WSL Agent を起動します。
    /// </summary>
    /// <param name="args">起動イベントの引数</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Closed += Window_Closed;
        _window.Activate();
        _ = StartAgentAsync();
    }

    private static void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Debug.WriteLine(e.Exception);
        try
        {
            File.WriteAllText(
                Path.Combine(Path.GetTempPath(), "docklet-unhandled.log"),
                e.Exception.ToString());
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// Agent を配置して常駐起動します。
    /// </summary>
    private async Task StartAgentAsync()
    {
        try
        {
            await Agent.StartAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"WSL Agent の起動に失敗しました: {ex}");
        }
    }

    private async void Window_Closed(object sender, WindowEventArgs args)
    {
        await Agent.DisposeAsync();
    }
}
