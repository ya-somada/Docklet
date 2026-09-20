namespace Docklet.Services.Docker.Containers;

/// <summary>
/// Docker コンテナーの一覧表示用データ
/// </summary>
/// <param name="Id">コンテナー ID</param>
/// <param name="Name">コンテナー名</param>
/// <param name="Status">状態</param>
/// <param name="StatusText">表示用ステータス</param>
public sealed record ContainerInfo(
    string Id,
    string Name,
    ContainerStatus Status,
    string StatusText)
{
    /// <summary>
    /// 起動できる状態かどうか
    /// </summary>
    public bool CanStart =>
        Status is ContainerStatus.Created
            or ContainerStatus.Exited
            or ContainerStatus.Dead
            or ContainerStatus.Unknown;

    /// <summary>
    /// 停止できる状態かどうか
    /// </summary>
    public bool CanStop =>
        Status is ContainerStatus.Running
            or ContainerStatus.Paused
            or ContainerStatus.Restarting;

    /// <summary>
    /// ログを見られる状態かどうか
    /// </summary>
    public bool CanViewLogs => CanStop;

    /// <summary>
    /// ターミナルで exec 接続できる状態かどうか
    /// </summary>
    public bool CanOpenTerminal => Status == ContainerStatus.Running;
}
