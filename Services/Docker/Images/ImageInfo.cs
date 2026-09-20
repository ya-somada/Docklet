namespace Docklet.Services.Docker.Images;

/// <summary>
/// Docker イメージの一覧表示用データ
/// </summary>
/// <param name="Id">イメージ ID ("sha256:..." 形式)</param>
/// <param name="Tags">タグ (複数はカンマ区切り、未タグ付けは空文字)</param>
/// <param name="CreatedAt">作成日時</param>
/// <param name="Size">サイズ (バイト)</param>
/// <param name="ContainerCount">このイメージを使用しているコンテナー数 (停止中も含む)</param>
public sealed record ImageInfo(
    string Id,
    string Tags,
    DateTimeOffset? CreatedAt,
    long Size,
    int ContainerCount)
{
    private const string Sha256Prefix = "sha256:";

    /// <summary>
    /// 短縮 ID (先頭 12 桁)
    /// </summary>
    public string ShortId => Id.StartsWith(Sha256Prefix, StringComparison.Ordinal)
        ? Id[Sha256Prefix.Length..Math.Min(Id.Length, Sha256Prefix.Length + 12)]
        : Id;

    /// <summary>
    /// 一覧の見出しに使う表示名 (タグがなければ短縮 ID)
    /// </summary>
    public string DisplayName => string.IsNullOrEmpty(Tags) ? ShortId : Tags;

    /// <summary>
    /// 人が読みやすい形式のサイズ
    /// </summary>
    public string SizeText => FormatSize(Size);

    private static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var unitIndex = 0;
        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return $"{value:0.##} {units[unitIndex]}";
    }
}
