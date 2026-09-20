using System.Collections.ObjectModel;

namespace Docklet.Pages;

/// <summary>
/// イメージ一覧を表示する画面
/// </summary>
public sealed partial class ImagesPage : Page
{
    private CancellationTokenSource? _loadCts;
    private bool _busy;

    private readonly ObservableCollection<ImageInfo> _images = [];

    /// <summary>
    /// イメージ画面を初期化します。
    /// </summary>
    public ImagesPage()
    {
        InitializeComponent();
        RefreshButtonText.Text = LocalizationService.GetString("Images_Refresh.Content");
        ImageList.ItemsSource = _images;
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
        if (sender is FrameworkElement { Tag: ImageInfo image })
        {
            _ = ConfirmAndRemoveAsync(image);
        }
    }

    private async Task ConfirmAndRemoveAsync(ImageInfo image)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = LocalizationService.GetString("Images_RemoveConfirmTitle"),
            Content = string.Format(LocalizationService.GetString("Images_RemoveConfirmMessage"), image.DisplayName),
            PrimaryButtonText = LocalizationService.GetString("Images_Remove.Text"),
            CloseButtonText = LocalizationService.GetString("Images_Cancel"),
            DefaultButton = ContentDialogButton.Close,
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await RemoveAsync(image);
        }
    }

    private async Task RemoveAsync(ImageInfo image)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        ImageList.IsHitTestVisible = false;
        RefreshButton.IsEnabled = false;
        StatusInfoBar.IsOpen = false;

        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;

        try
        {
            await Docklet.App.Current.Docker.Images.RemoveAsync(image.Id, token);
            await LoadAsync(silent: true);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            StatusInfoBar.Severity = InfoBarSeverity.Error;
            StatusInfoBar.Title = LocalizationService.GetString("Images_RemoveErrorTitle");
            StatusInfoBar.Message = ex.Message;
            StatusInfoBar.IsOpen = true;
        }
        finally
        {
            _busy = false;
            ImageList.IsHitTestVisible = true;
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
            ImageListView.Visibility = Visibility.Collapsed;
            EmptyText.Visibility = Visibility.Collapsed;
            StatusInfoBar.IsOpen = false;
        }

        RefreshButton.IsEnabled = false;

        try
        {
            var items = await Docklet.App.Current.Docker.Images.ListAsync(token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            _images.Clear();
            foreach (var item in items)
            {
                _images.Add(item);
            }

            if (_images.Count == 0)
            {
                EmptyText.Visibility = Visibility.Visible;
                ImageListView.Visibility = Visibility.Collapsed;
            }
            else
            {
                EmptyText.Visibility = Visibility.Collapsed;
                ImageListView.Visibility = Visibility.Visible;
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
