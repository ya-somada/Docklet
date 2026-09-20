namespace Docklet.Services.Docker.Containers;

/// <summary>
/// 起動中コンテナー数を取得するメソッド
/// </summary>
internal static class RunningCount
{
    /// <summary>
    /// Agent メソッド名
    /// </summary>
    public const string MethodName = "containers.runningCount";

    /// <summary>
    /// 実行中のコンテナー数を取得します。
    /// </summary>
    /// <param name="agent">Agent クライアント</param>
    /// <param name="cancellationToken">キャンセル トークン</param>
    /// <returns>実行中のコンテナー数</returns>
    public static async Task<int> InvokeAsync(AgentClient agent, CancellationToken cancellationToken = default)
    {
        var result = await agent.InvokeAsync(MethodName, cancellationToken).ConfigureAwait(false);
        if (result.ValueKind != JsonValueKind.Object || !result.TryGetProperty("count", out var count))
        {
            throw new InvalidOperationException("Docker からコンテナー数を取得できませんでした。");
        }

        return count.GetInt32();
    }
}
