namespace Docklet.Pages;

/// <summary>
/// Docker エンジンの概要を表示するホーム画面
/// </summary>
public sealed partial class HomePage : Page
{
    private CancellationTokenSource? _loadCts;

    /// <summary>
    /// ホーム画面を初期化します。
    /// </summary>
    public HomePage()
    {
        InitializeComponent();
        RefreshButtonText.Text = LocalizationService.GetString("Home_Refresh.Content");
    }

    /// <summary>
    /// 画面表示時に概要を読み込みます。
    /// </summary>
    /// <param name="e">ナビゲーション イベントの引数</param>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ = LoadAsync();
    }

    /// <summary>
    /// 画面を離れるときに読み込みをキャンセルします。
    /// </summary>
    /// <param name="e">ナビゲーション イベントの引数</param>
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        _loadCts?.Cancel();
        base.OnNavigatedFrom(e);
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;

        OverviewProgress.IsActive = true;
        OverviewProgress.Visibility = Visibility.Visible;
        OverviewView.Visibility = Visibility.Collapsed;
        StatusInfoBar.IsOpen = false;
        RefreshButton.IsEnabled = false;

        try
        {
            var overview = await Docklet.App.Current.Docker.Overview.GetAsync(token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            RunningCountText.Text = overview.RunningContainers.ToString();
            ImagesCountText.Text = overview.Images.ToString();
            VolumesCountText.Text = overview.Volumes.ToString();
            NetworksCountText.Text = overview.Networks.ToString();

            var notAvailable = LocalizationService.GetString("ContainerDetail_NotAvailable");
            EngineVersionText.Text = string.IsNullOrWhiteSpace(overview.ServerVersion)
                ? notAvailable
                : $"Docker {overview.ServerVersion}";
            EngineOsText.Text = string.IsNullOrWhiteSpace(overview.OperatingSystem)
                ? notAvailable
                : overview.OperatingSystem;

            OverviewView.Visibility = Visibility.Visible;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            StatusInfoBar.Severity = InfoBarSeverity.Error;
            StatusInfoBar.Title = LocalizationService.GetString("Home_DockerErrorTitle");
            StatusInfoBar.Message = ex.Message;
            StatusInfoBar.IsOpen = true;
        }
        finally
        {
            OverviewProgress.IsActive = false;
            OverviewProgress.Visibility = Visibility.Collapsed;
            RefreshButton.IsEnabled = true;
        }
    }
}
