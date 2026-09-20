namespace Docklet.Services.Docker.Containers;

/// <summary>
/// コンテナー一覧を取得するメソッド
/// </summary>
internal static class ListContainers
{
    /// <summary>
    /// Agent メソッド名
    /// </summary>
    public const string MethodName = "containers.list";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// コンテナー一覧を取得します。
    /// </summary>
    /// <param name="agent">Agent クライアント</param>
    /// <param name="cancellationToken">キャンセル トークン</param>
    /// <returns>コンテナー一覧</returns>
    public static async Task<IReadOnlyList<ContainerInfo>> InvokeAsync(AgentClient agent, CancellationToken cancellationToken = default)
    {
        var result = await agent.InvokeAsync(MethodName, cancellationToken).ConfigureAwait(false);
        var parsed = result.Deserialize<Result>(JsonOptions);
        if (parsed?.Containers is null)
        {
            throw new InvalidOperationException("Docker からコンテナー一覧を取得できませんでした。");
        }

        return parsed.Containers
            .ConvertAll(item => item.ToInfo())
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private sealed class Result
    {
        public List<RawContainer> Containers { get; set; } = [];
    }

    private sealed class RawContainer
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string State { get; set; } = "";
        public string Status { get; set; } = "";

        public ContainerInfo ToInfo() =>
            new(Id, Name, ContainerStatusParser.Parse(State), Status);
    }
}
