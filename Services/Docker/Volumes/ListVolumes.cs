namespace Docklet.Services.Docker.Volumes;

/// <summary>
/// ボリューム一覧を取得するメソッド
/// </summary>
internal static class ListVolumes
{
    /// <summary>
    /// Agent メソッド名
    /// </summary>
    public const string MethodName = "volumes.list";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// ボリューム一覧を取得します。
    /// </summary>
    /// <param name="agent">Agent クライアント</param>
    /// <param name="cancellationToken">キャンセル トークン</param>
    /// <returns>ボリューム一覧</returns>
    public static async Task<IReadOnlyList<VolumeInfo>> InvokeAsync(AgentClient agent, CancellationToken cancellationToken = default)
    {
        var result = await agent.InvokeAsync(MethodName, cancellationToken).ConfigureAwait(false);
        var parsed = result.Deserialize<Result>(JsonOptions);
        if (parsed?.Volumes is null)
        {
            throw new InvalidOperationException("Docker からボリューム一覧を取得できませんでした。");
        }

        return parsed.Volumes
            .ConvertAll(item => item.ToInfo())
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private sealed class Result
    {
        public List<RawVolume> Volumes { get; set; } = [];
    }

    private sealed class RawVolume
    {
        public string Name { get; set; } = "";
        public string Driver { get; set; } = "";
        public string Mountpoint { get; set; } = "";
        public string CreatedAt { get; set; } = "";

        public VolumeInfo ToInfo() =>
            new(Name, Driver, Mountpoint, ParseCreatedAt(CreatedAt));

        private static DateTimeOffset? ParseCreatedAt(string value) =>
            DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
    }
}
