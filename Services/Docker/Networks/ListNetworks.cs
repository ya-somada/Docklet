namespace Docklet.Services.Docker.Networks;

/// <summary>
/// ネットワーク一覧を取得するメソッド
/// </summary>
internal static class ListNetworks
{
    /// <summary>
    /// Agent メソッド名
    /// </summary>
    public const string MethodName = "networks.list";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// ネットワーク一覧を取得します。
    /// </summary>
    /// <param name="agent">Agent クライアント</param>
    /// <param name="cancellationToken">キャンセル トークン</param>
    /// <returns>ネットワーク一覧</returns>
    public static async Task<IReadOnlyList<NetworkInfo>> InvokeAsync(AgentClient agent, CancellationToken cancellationToken = default)
    {
        var result = await agent.InvokeAsync(MethodName, cancellationToken).ConfigureAwait(false);
        var parsed = result.Deserialize<Result>(JsonOptions);
        if (parsed?.Networks is null)
        {
            throw new InvalidOperationException("Docker からネットワーク一覧を取得できませんでした。");
        }

        return parsed.Networks
            .ConvertAll(item => item.ToInfo())
            .Where(item => item.Name != "none")
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private sealed class Result
    {
        public List<RawNetwork> Networks { get; set; } = [];
    }

    private sealed class RawNetwork
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Driver { get; set; } = "";
        public string Scope { get; set; } = "";
        public string CreatedAt { get; set; } = "";
        public string Subnet { get; set; } = "";
        public string Gateway { get; set; } = "";
        public int ContainerCount { get; set; }

        public NetworkInfo ToInfo() =>
            new(Id, Name, Driver, Scope, ParseCreatedAt(CreatedAt), Subnet, Gateway, ContainerCount);

        private static DateTimeOffset? ParseCreatedAt(string value) =>
            DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
    }
}
