namespace Docklet.Services.Docker.Containers;

/// <summary>
/// コンテナー関連の Docker 操作
/// </summary>
public sealed class ContainerService
{
    private readonly AgentClient _agent;

    /// <summary>
    /// Agent クライアントを指定して初期化します。
    /// </summary>
    /// <param name="agent">Agent クライアント</param>
    public ContainerService(AgentClient agent)
    {
        _agent = agent;
    }

    /// <summary>
    /// 実行中のコンテナー数を取得します。
    /// </summary>
    /// <param name="cancellationToken">キャンセル トークン</param>
    /// <returns>実行中のコンテナー数</returns>
    public Task<int> GetRunningCountAsync(CancellationToken cancellationToken = default) =>
        RunningCount.InvokeAsync(_agent, cancellationToken);

    /// <summary>
    /// コンテナー一覧を取得します。
    /// </summary>
    /// <param name="cancellationToken">キャンセル トークン</param>
    /// <returns>コンテナー一覧</returns>
    public Task<IReadOnlyList<ContainerInfo>> ListAsync(CancellationToken cancellationToken = default) =>
        ListContainers.InvokeAsync(_agent, cancellationToken);

    /// <summary>
    /// コンテナーを起動します。
    /// </summary>
    /// <param name="id">コンテナー ID</param>
    /// <param name="cancellationToken">キャンセル トークン</param>
    public Task StartAsync(string id, CancellationToken cancellationToken = default) =>
        StartContainer.InvokeAsync(_agent, id, cancellationToken);

    /// <summary>
    /// コンテナーを停止します。
    /// </summary>
    /// <param name="id">コンテナー ID</param>
    /// <param name="cancellationToken">キャンセル トークン</param>
    public Task StopAsync(string id, CancellationToken cancellationToken = default) =>
        StopContainer.InvokeAsync(_agent, id, cancellationToken);

    /// <summary>
    /// コンテナー詳細を取得します。
    /// </summary>
    /// <param name="id">コンテナー ID</param>
    /// <param name="cancellationToken">キャンセル トークン</param>
    /// <returns>コンテナー詳細</returns>
    public Task<InspectInfo> InspectAsync(string id, CancellationToken cancellationToken = default) =>
        InspectContainer.InvokeAsync(_agent, id, cancellationToken);

    /// <summary>
    /// コンテナーログを取得します。
    /// </summary>
    /// <param name="id">コンテナー ID</param>
    /// <param name="cancellationToken">キャンセル トークン</param>
    /// <returns>ログ本文</returns>
    public Task<string> GetLogsAsync(string id, CancellationToken cancellationToken = default) =>
        GetContainerLogs.InvokeAsync(_agent, id, cancellationToken);
}
