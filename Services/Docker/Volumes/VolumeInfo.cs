namespace Docklet.Services.Docker.Volumes;

/// <summary>
/// Docker ボリュームの一覧表示用データ
/// </summary>
/// <param name="Name">ボリューム名</param>
/// <param name="Driver">ドライバー</param>
/// <param name="Mountpoint">マウント先パス</param>
/// <param name="CreatedAt">作成日時</param>
public sealed record VolumeInfo(
    string Name,
    string Driver,
    string Mountpoint,
    DateTimeOffset? CreatedAt);
