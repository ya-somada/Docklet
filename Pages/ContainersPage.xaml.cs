using System.Collections.ObjectModel;
using Docklet.Pages.Container;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Docklet.Pages;

/// <summary>
/// コンテナー一覧を表示する画面
/// </summary>
public sealed partial class ContainersPage : Page
{
    private CancellationTokenSource? _loadCts;

    private readonly ObservableCollection<ContainerInfo> _containers = [];

    /// <summary>
    /// コンテナー画面を初期化します。
    /// </summary>
    public ContainersPage()
    {
        InitializeComponent();
        RefreshButtonText.Text = LocalizationService.GetString("Containers_Refresh.Content");
        ContainerList.ItemsSource = _containers;
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

    private bool _busy;

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        _ = LoadAsync();
    }

    private void ContainerCard_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: ContainerInfo container })
        {
            return;
        }

        if (e.OriginalSource is DependencyObject source && IsInsideButton(source))
        {
            return;
        }

        OpenDetail(container);
    }

    private void LogsButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: ContainerInfo container })
        {
            OpenDetail(container, openLogs: true);
        }
    }

    private void OpenDetail(ContainerInfo container, bool openLogs = false)
    {
        try
        {
            Frame.Navigate(typeof(ContainerDetailPage), new ContainerDetailNav(container, openLogs));
        }
        catch (Exception ex)
        {
            StatusInfoBar.Severity = InfoBarSeverity.Error;
            StatusInfoBar.Title = LocalizationService.GetString("ContainerDetail_InspectErrorTitle");
            StatusInfoBar.Message = ex.Message;
            StatusInfoBar.IsOpen = true;
        }
    }

    private void TerminalButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: ContainerInfo container })
        {
            return;
        }

        try
        {
            TerminalLauncher.Open(container.Id);
        }
        catch (Exception ex)
        {
            StatusInfoBar.Severity = InfoBarSeverity.Error;
            StatusInfoBar.Title = LocalizationService.GetString("Containers_TerminalErrorTitle");
            StatusInfoBar.Message = ex.Message;
            StatusInfoBar.IsOpen = true;
        }
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: ContainerInfo container })
        {
            _ = ChangeStateAsync(container, start: true);
        }
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: ContainerInfo container })
        {
            _ = ChangeStateAsync(container, start: false);
        }
    }

    private async Task ChangeStateAsync(ContainerInfo container, bool start)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        ContainerList.IsHitTestVisible = false;
        RefreshButton.IsEnabled = false;
        StatusInfoBar.IsOpen = false;

        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;

        try
        {
            if (start)
            {
                await Docklet.App.Current.Docker.Containers.StartAsync(container.Id, token);
            }
            else
            {
                await Docklet.App.Current.Docker.Containers.StopAsync(container.Id, token);
            }

            await LoadAsync(silent: true);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            StatusInfoBar.Severity = InfoBarSeverity.Error;
            StatusInfoBar.Title = LocalizationService.GetString(
                start ? "Containers_StartErrorTitle" : "Containers_StopErrorTitle");
            StatusInfoBar.Message = ex.Message;
            StatusInfoBar.IsOpen = true;
        }
        finally
        {
            _busy = false;
            ContainerList.IsHitTestVisible = true;
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
            ContainerListView.Visibility = Visibility.Collapsed;
            EmptyText.Visibility = Visibility.Collapsed;
            StatusInfoBar.IsOpen = false;
        }

        RefreshButton.IsEnabled = false;

        try
        {
            var items = await Docklet.App.Current.Docker.Containers.ListAsync(token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            _containers.Clear();
            foreach (var item in items)
            {
                _containers.Add(item);
            }

            if (_containers.Count == 0)
            {
                EmptyText.Visibility = Visibility.Visible;
                ContainerListView.Visibility = Visibility.Collapsed;
            }
            else
            {
                EmptyText.Visibility = Visibility.Collapsed;
                ContainerListView.Visibility = Visibility.Visible;
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

    private static bool IsInsideButton(DependencyObject source)
    {
        for (var current = source; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is Button)
            {
                return true;
            }
        }

        return false;
    }
}
