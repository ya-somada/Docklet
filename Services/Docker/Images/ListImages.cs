namespace Docklet.Services.Docker.Images;

/// <summary>
/// イメージ一覧を取得するメソッド
/// </summary>
internal static class ListImages
{
    /// <summary>
    /// Agent メソッド名
    /// </summary>
    public const string MethodName = "images.list";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// イメージ一覧を取得します。
    /// </summary>
    /// <param name="agent">Agent クライアント</param>
    /// <param name="cancellationToken">キャンセル トークン</param>
    /// <returns>イメージ一覧 (作成日時の新しい順)</returns>
    public static async Task<IReadOnlyList<ImageInfo>> InvokeAsync(AgentClient agent, CancellationToken cancellationToken = default)
    {
        var result = await agent.InvokeAsync(MethodName, cancellationToken).ConfigureAwait(false);
        var parsed = result.Deserialize<Result>(JsonOptions);
        if (parsed?.Images is null)
        {
            throw new InvalidOperationException("Docker からイメージ一覧を取得できませんでした。");
        }

        return parsed.Images
            .ConvertAll(item => item.ToInfo())
            .OrderByDescending(item => item.CreatedAt)
            .ToList();
    }

    private sealed class Result
    {
        public List<RawImage> Images { get; set; } = [];
    }

    private sealed class RawImage
    {
        public string Id { get; set; } = "";
        public string Tags { get; set; } = "";
        public string CreatedAt { get; set; } = "";
        public long Size { get; set; }
        public int ContainerCount { get; set; }

        public ImageInfo ToInfo() =>
            new(Id, Tags, ParseCreatedAt(CreatedAt), Size, ContainerCount);

        private static DateTimeOffset? ParseCreatedAt(string value) =>
            DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
    }
}
