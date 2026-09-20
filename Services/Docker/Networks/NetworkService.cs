namespace Docklet.Services.Docker.Networks;

/// <summary>
/// ネットワーク関連の Docker 操作
/// </summary>
public sealed class NetworkService
{
    private readonly AgentClient _agent;

    /// <summary>
    /// Agent クライアントを指定して初期化します。
    /// </summary>
    /// <param name="agent">Agent クライアント</param>
    public NetworkService(AgentClient agent)
    {
        _agent = agent;
    }

    /// <summary>
    /// ネットワーク一覧を取得します。
    /// </summary>
    /// <param name="cancellationToken">キャンセル トークン</param>
    /// <returns>ネットワーク一覧</returns>
    public Task<IReadOnlyList<NetworkInfo>> ListAsync(CancellationToken cancellationToken = default) =>
        ListNetworks.InvokeAsync(_agent, cancellationToken);

    /// <summary>
    /// ネットワークを削除します。
    /// </summary>
    /// <param name="name">ネットワーク名</param>
    /// <param name="cancellationToken">キャンセル トークン</param>
    public Task RemoveAsync(string name, CancellationToken cancellationToken = default) =>
        RemoveNetwork.InvokeAsync(_agent, name, cancellationToken);
}
