namespace Docklet.Services.Docker.Containers;

/// <summary>
/// コンテナーを起動するメソッド
/// </summary>
internal static class StartContainer
{
    /// <summary>
    /// Agent メソッド名
    /// </summary>
    public const string MethodName = "containers.start";

    /// <summary>
    /// コンテナーを起動します。
    /// </summary>
    /// <param name="agent">Agent クライアント</param>
    /// <param name="id">コンテナー ID</param>
    /// <param name="cancellationToken">キャンセル トークン</param>
    public static Task InvokeAsync(AgentClient agent, string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return agent.InvokeAsync(MethodName, new Params(id), cancellationToken);
    }

    private sealed record Params(string Id);
}
