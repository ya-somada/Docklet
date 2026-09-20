namespace Docklet.Services.Docker.Overview;

/// <summary>
/// Docker エンジン概要関連の操作
/// </summary>
public sealed class OverviewService
{
    private readonly AgentClient _agent;

    /// <summary>
    /// Agent クライアントを指定して初期化します。
    /// </summary>
    /// <param name="agent">Agent クライアント</param>
    public OverviewService(AgentClient agent)
    {
        _agent = agent;
    }

    /// <summary>
    /// Docker エンジンの概要を取得します。
    /// </summary>
    /// <param name="cancellationToken">キャンセル トークン</param>
    /// <returns>概要情報</returns>
    public Task<SystemOverview> GetAsync(CancellationToken cancellationToken = default) =>
        GetSystemOverview.InvokeAsync(_agent, cancellationToken);
}
