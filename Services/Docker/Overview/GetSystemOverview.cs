namespace Docklet.Services.Docker.Overview;

/// <summary>
/// Docker エンジンの概要を取得するメソッド
/// </summary>
internal static class GetSystemOverview
{
    /// <summary>
    /// Agent メソッド名
    /// </summary>
    public const string MethodName = "system.overview";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Docker エンジンの概要を取得します。
    /// </summary>
    /// <param name="agent">Agent クライアント</param>
    /// <param name="cancellationToken">キャンセル トークン</param>
    /// <returns>概要情報</returns>
    public static async Task<SystemOverview> InvokeAsync(AgentClient agent, CancellationToken cancellationToken = default)
    {
        var result = await agent.InvokeAsync(MethodName, cancellationToken).ConfigureAwait(false);
        var parsed = result.Deserialize<SystemOverview>(JsonOptions);
        return parsed ?? throw new InvalidOperationException("Docker から概要情報を取得できませんでした。");
    }
}
