namespace Docklet.Services.Docker.Networks;

/// <summary>
/// ネットワークを削除するメソッド
/// </summary>
internal static class RemoveNetwork
{
    /// <summary>
    /// Agent メソッド名
    /// </summary>
    public const string MethodName = "networks.remove";

    /// <summary>
    /// ネットワークを削除します。
    /// </summary>
    /// <param name="agent">Agent クライアント</param>
    /// <param name="name">ネットワーク名</param>
    /// <param name="cancellationToken">キャンセル トークン</param>
    public static Task InvokeAsync(AgentClient agent, string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return agent.InvokeAsync(MethodName, new Params(name), cancellationToken);
    }

    private sealed record Params(string Name);
}
