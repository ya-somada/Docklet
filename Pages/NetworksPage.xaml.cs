using System.Collections.ObjectModel;

namespace Docklet.Pages;

/// <summary>
/// ネットワーク一覧を表示する画面
/// </summary>
public sealed partial class NetworksPage : Page
{
    private CancellationTokenSource? _loadCts;
    private bool _busy;

    private readonly ObservableCollection<NetworkInfo> _networks = [];

    /// <summary>
    /// ネットワーク画面を初期化します。
    /// </summary>
    public NetworksPage()
    {
        InitializeComponent();
        RefreshButtonText.Text = LocalizationService.GetString("Networks_Refresh.Content");
        NetworkList.ItemsSource = _networks;
    }

    /// <summary>
    /// 画面表示時に一覧を読み込みます。
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

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: NetworkInfo network })
        {
            _ = ConfirmAndRemoveAsync(network);
        }
    }

    private async Task ConfirmAndRemoveAsync(NetworkInfo network)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = LocalizationService.GetString("Networks_RemoveConfirmTitle"),
            Content = string.Format(LocalizationService.GetString("Networks_RemoveConfirmMessage"), network.Name),
            PrimaryButtonText = LocalizationService.GetString("Networks_Remove.Text"),
            CloseButtonText = LocalizationService.GetString("Networks_Cancel"),
            DefaultButton = ContentDialogButton.Close,
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await RemoveAsync(network);
        }
    }

    private async Task RemoveAsync(NetworkInfo network)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        NetworkList.IsHitTestVisible = false;
        RefreshButton.IsEnabled = false;
        StatusInfoBar.IsOpen = false;

        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;

        try
        {
            await Docklet.App.Current.Docker.Networks.RemoveAsync(network.Name, token);
            await LoadAsync(silent: true);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            StatusInfoBar.Severity = InfoBarSeverity.Error;
            StatusInfoBar.Title = LocalizationService.GetString("Networks_RemoveErrorTitle");
            StatusInfoBar.Message = ex.Message;
            StatusInfoBar.IsOpen = true;
        }
        finally
        {
            _busy = false;
            NetworkList.IsHitTestVisible = true;
            RefreshButton.IsEnabled = true;
        }
    }

    private async Task LoadAsync(bool silent = false)
    {
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;

        if (!silent)
        {
            ListProgress.IsActive = true;
            ListProgress.Visibility = Visibility.Visible;
            NetworkListView.Visibility = Visibility.Collapsed;
            EmptyText.Visibility = Visibility.Collapsed;
            StatusInfoBar.IsOpen = false;
        }

        RefreshButton.IsEnabled = false;

        try
        {
            var items = await Docklet.App.Current.Docker.Networks.ListAsync(token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            _networks.Clear();
            foreach (var item in items)
            {
                _networks.Add(item);
            }

            if (_networks.Count == 0)
            {
                EmptyText.Visibility = Visibility.Visible;
                NetworkListView.Visibility = Visibility.Collapsed;
            }
            else
            {
                EmptyText.Visibility = Visibility.Collapsed;
                NetworkListView.Visibility = Visibility.Visible;
            }
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
            if (!silent)
            {
                EmptyText.Visibility = Visibility.Visible;
            }
        }
        finally
        {
            if (!silent)
            {
                ListProgress.IsActive = false;
                ListProgress.Visibility = Visibility.Collapsed;
            }

            if (!_busy)
            {
                RefreshButton.IsEnabled = true;
            }
        }
    }
}
