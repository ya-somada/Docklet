namespace Docklet.Services.Docker.Containers;

/// <summary>
/// コンテナーログを取得するメソッド
/// </summary>
internal static class GetContainerLogs
{
    /// <summary>
    /// Agent メソッド名
    /// </summary>
    public const string MethodName = "containers.logs";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// コンテナーログを取得します。
    /// </summary>
    /// <param name="agent">Agent クライアント</param>
    /// <param name="id">コンテナー ID</param>
    /// <param name="cancellationToken">キャンセル トークン</param>
    /// <returns>ログ本文</returns>
    public static async Task<string> InvokeAsync(AgentClient agent, string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var result = await agent.InvokeAsync(MethodName, new Params(id), cancellationToken).ConfigureAwait(false);
        var parsed = result.Deserialize<Result>(JsonOptions);
        return parsed?.Text ?? "";
    }

    private sealed record Params(string Id);

    private sealed class Result
    {
        public string Text { get; set; } = "";
    }
}
