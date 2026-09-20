using System.Collections.ObjectModel;

namespace Docklet.Pages;

/// <summary>
/// ボリューム一覧を表示する画面
/// </summary>
public sealed partial class VolumesPage : Page
{
    private CancellationTokenSource? _loadCts;
    private bool _busy;

    private readonly ObservableCollection<VolumeInfo> _volumes = [];

    /// <summary>
    /// ボリューム画面を初期化します。
    /// </summary>
    public VolumesPage()
    {
        InitializeComponent();
        RefreshButtonText.Text = LocalizationService.GetString("Volumes_Refresh.Content");
        VolumeList.ItemsSource = _volumes;
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
        if (sender is FrameworkElement { Tag: VolumeInfo volume })
        {
            _ = ConfirmAndRemoveAsync(volume);
        }
    }

    private async Task ConfirmAndRemoveAsync(VolumeInfo volume)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = LocalizationService.GetString("Volumes_RemoveConfirmTitle"),
            Content = string.Format(LocalizationService.GetString("Volumes_RemoveConfirmMessage"), volume.Name),
            PrimaryButtonText = LocalizationService.GetString("Volumes_Remove.Text"),
            CloseButtonText = LocalizationService.GetString("Volumes_Cancel"),
            DefaultButton = ContentDialogButton.Close,
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await RemoveAsync(volume);
        }
    }

    private async Task RemoveAsync(VolumeInfo volume)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        VolumeList.IsHitTestVisible = false;
        RefreshButton.IsEnabled = false;
        StatusInfoBar.IsOpen = false;

        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;

        try
        {
            await Docklet.App.Current.Docker.Volumes.RemoveAsync(volume.Name, token);
            await LoadAsync(silent: true);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            StatusInfoBar.Severity = InfoBarSeverity.Error;
            StatusInfoBar.Title = LocalizationService.GetString("Volumes_RemoveErrorTitle");
            StatusInfoBar.Message = ex.Message;
            StatusInfoBar.IsOpen = true;
        }
        finally
        {
            _busy = false;
            VolumeList.IsHitTestVisible = true;
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
            VolumeListView.Visibility = Visibility.Collapsed;
            EmptyText.Visibility = Visibility.Collapsed;
            StatusInfoBar.IsOpen = false;
        }

        RefreshButton.IsEnabled = false;

        try
        {
            var items = await Docklet.App.Current.Docker.Volumes.ListAsync(token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            _volumes.Clear();
            foreach (var item in items)
            {
                _volumes.Add(item);
            }

            if (_volumes.Count == 0)
            {
                EmptyText.Visibility = Visibility.Visible;
                VolumeListView.Visibility = Visibility.Collapsed;
            }
            else
            {
                EmptyText.Visibility = Visibility.Collapsed;
                VolumeListView.Visibility = Visibility.Visible;
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
