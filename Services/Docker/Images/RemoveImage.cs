namespace Docklet.Services.Docker.Images;

/// <summary>
/// イメージを削除するメソッド
/// </summary>
internal static class RemoveImage
{
    /// <summary>
    /// Agent メソッド名
    /// </summary>
    public const string MethodName = "images.remove";

    /// <summary>
    /// イメージを削除します。
    /// </summary>
    /// <param name="agent">Agent クライアント</param>
    /// <param name="id">イメージ ID</param>
    /// <param name="cancellationToken">キャンセル トークン</param>
    public static Task InvokeAsync(AgentClient agent, string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return agent.InvokeAsync(MethodName, new Params(id), cancellationToken);
    }

    private sealed record Params(string Id);
}
