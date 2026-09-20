namespace Docklet.Pages.Container;

/// <summary>
/// コンテナー詳細画面への遷移引数
/// </summary>
/// <param name="Container">対象コンテナー</param>
/// <param name="OpenLogs">ログタブを開くとき true</param>
public sealed record ContainerDetailNav(ContainerInfo Container, bool OpenLogs = false);
