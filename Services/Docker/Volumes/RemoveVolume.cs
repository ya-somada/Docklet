namespace Docklet.Services.Docker.Volumes;

/// <summary>
/// ボリュームを削除するメソッド
/// </summary>
internal static class RemoveVolume
{
    /// <summary>
    /// Agent メソッド名
    /// </summary>
    public const string MethodName = "volumes.remove";

    /// <summary>
    /// ボリュームを削除します。
    /// </summary>
    /// <param name="agent">Agent クライアント</param>
    /// <param name="name">ボリューム名</param>
    /// <param name="cancellationToken">キャンセル トークン</param>
    public static Task InvokeAsync(AgentClient agent, string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return agent.InvokeAsync(MethodName, new Params(name), cancellationToken);
    }

    private sealed record Params(string Name);
}
