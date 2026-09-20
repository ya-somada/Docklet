using System.Collections.ObjectModel;
using System.Globalization;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using Windows.UI.Core;

namespace Docklet.Pages.Container;

/// <summary>
/// コンテナー詳細画面
/// </summary>
public sealed partial class ContainerDetailPage : Page
{
    private ContainerInfo? _container;
    private CancellationTokenSource? _inspectCts;
    private CancellationTokenSource? _logsCts;
    private bool _inspectLoaded;
    private bool _logsLoaded;
    private readonly ObservableCollection<EnvVarItem> _envVars = [];
    private readonly ObservableCollection<PortItem> _ports = [];
    private readonly ObservableCollection<MountItem> _mounts = [];
    private readonly List<TextRange> _logsMatchRanges = [];
    private int _logsCurrentMatchIndex = -1;

    /// <summary>
    /// 詳細画面を初期化します。
    /// </summary>
    public ContainerDetailPage()
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            try
            {
                File.WriteAllText(
                    Path.Combine(Path.GetTempPath(), "docklet-unhandled.log"),
                    ex.ToString());
            }
            catch (Exception)
            {
            }

            throw;
        }

        DetailRefreshText.Text = LocalizationService.GetString("Containers_Refresh.Content");
        EnvList.ItemsSource = _envVars;
        PortsList.ItemsSource = _ports;
        MountsList.ItemsSource = _mounts;
        DetailSelector.SelectionChanged += DetailSelector_SelectionChanged;
        Loaded += ContainerDetailPage_Loaded;
    }

    /// <summary>
    /// 選択したコンテナーを表示します。
    /// </summary>
    /// <param name="e">ナビゲーション イベントの引数</param>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var openLogs = false;
        if (e.Parameter is ContainerDetailNav nav)
        {
            _container = nav.Container;
            openLogs = nav.OpenLogs;
        }
        else if (e.Parameter is ContainerInfo container)
        {
            _container = container;
        }

        TitleText.Text = _container?.Name ?? string.Empty;
        ShowListOverview();
        DetailSelector.SelectedItem = openLogs ? LogsItem : OverviewItem;
    }

    /// <summary>
    /// 画面を離れるときに取得をキャンセルします。
    /// </summary>
    /// <param name="e">ナビゲーション イベントの引数</param>
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        _inspectCts?.Cancel();
        _logsCts?.Cancel();
        base.OnNavigatedFrom(e);
    }

    private void ContainerDetailPage_Loaded(object sender, RoutedEventArgs e)
    {
        LoadVisibleTab();
    }

    private void ParentCrumb_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
            return;
        }

        Frame.Navigate(typeof(ContainersPage));
    }

    private void ParentCrumb_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        ParentCrumb.Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
    }

    private void ParentCrumb_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        ParentCrumb.Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
    }

    private void LogsFindAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (CurrentTab != DetailTab.Logs)
        {
            return;
        }

        args.Handled = true;
        LogsSearchBox.Focus(FocusState.Programmatic);
    }

    private void DetailSelector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (OverviewPanel is null || EnvPanel is null || LogsPanel is null)
        {
            return;
        }

        ApplyTabVisibility();
        LoadVisibleTab();
    }

    private void DetailRefreshButton_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentTab == DetailTab.Logs)
        {
            _logsLoaded = false;
            _ = LoadLogsAsync();
            return;
        }

        _inspectLoaded = false;
        _ = LoadInspectAsync();
    }

    private DetailTab CurrentTab
    {
        get
        {
            if (DetailSelector.SelectedItem == LogsItem)
            {
                return DetailTab.Logs;
            }

            if (DetailSelector.SelectedItem == EnvItem)
            {
                return DetailTab.Env;
            }

            return DetailTab.Overview;
        }
    }

    private void ApplyTabVisibility()
    {
        var tab = CurrentTab;
        OverviewPanel.Visibility = tab == DetailTab.Overview ? Visibility.Visible : Visibility.Collapsed;
        EnvPanel.Visibility = tab == DetailTab.Env ? Visibility.Visible : Visibility.Collapsed;
        LogsPanel.Visibility = tab == DetailTab.Logs ? Visibility.Visible : Visibility.Collapsed;
    }

    private void LoadVisibleTab()
    {
        if (CurrentTab == DetailTab.Logs)
        {
            if (!_logsLoaded)
            {
                _ = LoadLogsAsync();
            }

            return;
        }

        if (!_inspectLoaded)
        {
            _ = LoadInspectAsync();
        }
    }

    private void ShowListOverview()
    {
        if (_container is null || OverviewIdText is null)
        {
            return;
        }

        OverviewIdText.Text = _container.Id;
        OverviewNameText.Text = _container.Name;
        ApplyStatus(_container.Status, _container.StatusText);
        OverviewCreatedText.Text = MissingValue();
        OverviewStartedText.Text = MissingValue();
    }

    private async Task LoadInspectAsync()
    {
        if (_container is null)
        {
            return;
        }

        _inspectCts?.Cancel();
        _inspectCts = new CancellationTokenSource();
        var token = _inspectCts.Token;
        RunOnUi(() =>
        {
            OverviewInfoBar.IsOpen = false;
            EnvInfoBar.IsOpen = false;
            DetailRefreshButton.IsEnabled = false;
        });

        try
        {
            var inspect = await Docklet.App.Current.Docker.Containers.InspectAsync(_container.Id, token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            RunOnUi(() =>
            {
                OverviewIdText.Text = inspect.Id;
                OverviewNameText.Text = inspect.Name;
                TitleText.Text = inspect.Name;
                ApplyStatus(inspect.Status, inspect.StatusText);
                OverviewCreatedText.Text = FormatDate(inspect.Created);
                OverviewStartedText.Text = FormatDate(inspect.StartedAt);
                ApplyEnv(inspect.Env);
                ApplyPorts(inspect.Ports);
                ApplyMounts(inspect.Mounts);
                _inspectLoaded = true;
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            RunOnUi(() =>
            {
                var bar = CurrentTab == DetailTab.Env ? EnvInfoBar : OverviewInfoBar;
                bar.Severity = InfoBarSeverity.Error;
                bar.Title = LocalizationService.GetString("ContainerDetail_InspectErrorTitle");
                bar.Message = ex.Message;
                bar.IsOpen = true;
            });
        }
        finally
        {
            RunOnUi(() => DetailRefreshButton.IsEnabled = true);
        }
    }

    private void ApplyEnv(IReadOnlyList<EnvVar> env)
    {
        _envVars.Clear();
        for (var i = 0; i < env.Count; i++)
        {
            _envVars.Add(new EnvVarItem
            {
                Name = env[i].Name,
                Value = env[i].Value,
                DividerVisibility = i < env.Count - 1 ? Visibility.Visible : Visibility.Collapsed,
            });
        }

        var empty = _envVars.Count == 0;
        EnvEmptyText.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        EnvListView.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ApplyPorts(IReadOnlyList<PortMapping> ports)
    {
        _ports.Clear();
        for (var i = 0; i < ports.Count; i++)
        {
            var port = ports[i];
            _ports.Add(new PortItem
            {
                ContainerPort = $"{port.ContainerPort}/{port.Protocol}",
                HostBinding = FormatHostBinding(port),
                DividerVisibility = i < ports.Count - 1 ? Visibility.Visible : Visibility.Collapsed,
            });
        }

        var empty = _ports.Count == 0;
        PortsEmptyText.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        PortsListView.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
    }

    private static string FormatHostBinding(PortMapping port)
    {
        if (string.IsNullOrWhiteSpace(port.HostPort))
        {
            return MissingValue();
        }

        var host = string.IsNullOrWhiteSpace(port.HostIp) || port.HostIp is "0.0.0.0" or "::"
            ? "localhost"
            : port.HostIp;
        return $"{host}:{port.HostPort}";
    }

    private void ApplyMounts(IReadOnlyList<MountInfo> mounts)
    {
        _mounts.Clear();
        for (var i = 0; i < mounts.Count; i++)
        {
            var mount = mounts[i];
            _mounts.Add(new MountItem
            {
                Source = mount.Source,
                Destination = mount.Destination,
                Mode = mount.ReadOnly ? "ro" : "rw",
                DividerVisibility = i < mounts.Count - 1 ? Visibility.Visible : Visibility.Collapsed,
            });
        }

        var empty = _mounts.Count == 0;
        MountsEmptyText.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        MountsListView.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ApplyStatus(ContainerStatus status, string statusText)
    {
        var converter = (IValueConverter)Resources["ContainerStatusConverter"];
        OverviewStatusText.Text = converter.Convert(status, typeof(string), null, string.Empty) as string ?? string.Empty;
        if (converter.Convert(status, typeof(Brush), "Background", string.Empty) is Brush background)
        {
            OverviewStatusBadge.Background = background;
        }

        if (converter.Convert(status, typeof(Brush), "Foreground", string.Empty) is Brush foreground)
        {
            OverviewStatusText.Foreground = foreground;
        }

        OverviewStatusDetailText.Text = statusText;
        OverviewStatusDetailText.Visibility = string.IsNullOrWhiteSpace(statusText)
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private static string FormatDate(DateTimeOffset? value)
    {
        if (value is null)
        {
            return MissingValue();
        }

        var culture = CultureInfo.GetCultureInfo(LocalizationService.CurrentLanguage);
        return value.Value.ToLocalTime().ToString("G", culture);
    }

    private static string MissingValue() =>
        LocalizationService.GetString("ContainerDetail_NotAvailable");

    private async Task LoadLogsAsync()
    {
        if (_container is null)
        {
            return;
        }

        _logsCts?.Cancel();
        _logsCts = new CancellationTokenSource();
        var token = _logsCts.Token;

        RunOnUi(() =>
        {
            LogsProgress.IsActive = true;
            LogsProgress.Visibility = Visibility.Visible;
            LogsView.Visibility = Visibility.Collapsed;
            LogsEmptyText.Visibility = Visibility.Collapsed;
            LogsInfoBar.IsOpen = false;
            DetailRefreshButton.IsEnabled = false;
        });

        try
        {
            var text = await Docklet.App.Current.Docker.Containers.GetLogsAsync(_container.Id, token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            RunOnUi(() =>
            {
                _logsLoaded = true;
                if (string.IsNullOrWhiteSpace(text))
                {
                    LogsEmptyText.Visibility = Visibility.Visible;
                }
                else
                {
                    LogsText.Text = text.TrimEnd();
                    LogsView.Visibility = Visibility.Visible;
                    UpdateLogsSearch(LogsSearchBox.Text);
                }
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            RunOnUi(() =>
            {
                LogsInfoBar.Severity = InfoBarSeverity.Error;
                LogsInfoBar.Title = LocalizationService.GetString("ContainerDetail_LogsErrorTitle");
                LogsInfoBar.Message = ex.Message;
                LogsInfoBar.IsOpen = true;
                LogsEmptyText.Visibility = Visibility.Visible;
            });
        }
        finally
        {
            RunOnUi(() =>
            {
                LogsProgress.IsActive = false;
                LogsProgress.Visibility = Visibility.Collapsed;
                DetailRefreshButton.IsEnabled = true;
            });
        }
    }

    private void LogsSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
        {
            return;
        }

        UpdateLogsSearch(sender.Text);
    }

    private void LogsSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var isShiftDown = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift)
            .HasFlag(CoreVirtualKeyStates.Down);
        GoToLogsMatch(isShiftDown ? -1 : 1);
    }

    private void UpdateLogsSearch(string query)
    {
        _logsMatchRanges.Clear();
        _logsCurrentMatchIndex = -1;

        if (!string.IsNullOrEmpty(query))
        {
            var text = LogsText.Text;
            var index = 0;
            while (index <= text.Length - query.Length)
            {
                var found = text.IndexOf(query, index, StringComparison.OrdinalIgnoreCase);
                if (found < 0)
                {
                    break;
                }

                _logsMatchRanges.Add(new TextRange { StartIndex = found, Length = query.Length });
                index = found + query.Length;
            }

            if (_logsMatchRanges.Count > 0)
            {
                _logsCurrentMatchIndex = 0;
            }
        }

        ApplyLogsHighlight();
        ScrollToCurrentLogsMatch();
    }

    private void GoToLogsMatch(int direction)
    {
        if (_logsMatchRanges.Count == 0)
        {
            return;
        }

        _logsCurrentMatchIndex = (_logsCurrentMatchIndex + direction + _logsMatchRanges.Count) % _logsMatchRanges.Count;
        ApplyLogsHighlight();
        ScrollToCurrentLogsMatch();
    }

    private void ApplyLogsHighlight()
    {
        LogsText.TextHighlighters.Clear();
        if (_logsMatchRanges.Count == 0)
        {
            return;
        }

        var normal = new TextHighlighter
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 241, 168)),
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0)),
        };
        var current = new TextHighlighter
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 152, 0)),
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
        };

        for (var i = 0; i < _logsMatchRanges.Count; i++)
        {
            (i == _logsCurrentMatchIndex ? current : normal).Ranges.Add(_logsMatchRanges[i]);
        }

        if (normal.Ranges.Count > 0)
        {
            LogsText.TextHighlighters.Add(normal);
        }

        if (current.Ranges.Count > 0)
        {
            LogsText.TextHighlighters.Add(current);
        }
    }

    private void ScrollToCurrentLogsMatch()
    {
        if (_logsCurrentMatchIndex < 0 || _logsCurrentMatchIndex >= _logsMatchRanges.Count)
        {
            return;
        }

        LogsText.UpdateLayout();
        if (LogsText.ActualHeight <= 0)
        {
            return;
        }

        var text = LogsText.Text;
        var matchStart = _logsMatchRanges[_logsCurrentMatchIndex].StartIndex;
        var lineIndex = 0;
        var totalLines = 1;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '\n')
            {
                continue;
            }

            totalLines++;
            if (i < matchStart)
            {
                lineIndex++;
            }
        }

        var lineHeight = LogsText.ActualHeight / totalLines;
        var targetY = Math.Max(0, (lineIndex * lineHeight) - (LogsView.ViewportHeight / 2));
        LogsView.ChangeView(null, (float)targetY, null);
    }

    private void RunOnUi(Action action)
    {
        if (DispatcherQueue.HasThreadAccess)
        {
            action();
            return;
        }

        DispatcherQueue.TryEnqueue(() => action());
    }

    private enum DetailTab
    {
        Overview,
        Env,
        Logs,
    }
}

/// <summary>
/// 環境変数タブの 1 行
/// </summary>
public sealed class EnvVarItem
{
    /// <summary>
    /// 変数名
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// 値
    /// </summary>
    public string Value { get; init; } = "";

    /// <summary>
    /// 行下の区切り線
    /// </summary>
    public Visibility DividerVisibility { get; init; }
}

/// <summary>
/// ポートタブの 1 行
/// </summary>
public sealed class PortItem
{
    /// <summary>
    /// コンテナー側ポート ("8080/tcp" のような表記)
    /// </summary>
    public string ContainerPort { get; init; } = "";

    /// <summary>
    /// ホスト側の公開先 ("localhost:8080" のような表記、未公開なら未設定を示す文言)
    /// </summary>
    public string HostBinding { get; init; } = "";

    /// <summary>
    /// 行下の区切り線
    /// </summary>
    public Visibility DividerVisibility { get; init; }
}

/// <summary>
/// マウントタブの 1 行
/// </summary>
public sealed class MountItem
{
    /// <summary>
    /// マウント元 (ホストパス、または名前付きボリューム名)
    /// </summary>
    public string Source { get; init; } = "";

    /// <summary>
    /// コンテナー側のマウント先パス
    /// </summary>
    public string Destination { get; init; } = "";

    /// <summary>
    /// 読み取り専用かどうか ("ro" / "rw")
    /// </summary>
    public string Mode { get; init; } = "";

    /// <summary>
    /// 行下の区切り線
    /// </summary>
    public Visibility DividerVisibility { get; init; }
}
