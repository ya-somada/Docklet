namespace Docklet.Services.Docker.Containers;

/// <summary>
/// Docker コンテナーの状態
/// </summary>
public enum ContainerStatus
{
    /// <summary>不明な状態</summary>
    Unknown = 0,

    /// <summary>作成済み</summary>
    Created,

    /// <summary>実行中</summary>
    Running,

    /// <summary>一時停止</summary>
    Paused,

    /// <summary>再起動中</summary>
    Restarting,

    /// <summary>削除中</summary>
    Removing,

    /// <summary>終了</summary>
    Exited,

    /// <summary>デッド</summary>
    Dead,
}

/// <summary>
/// <see cref="ContainerStatus"/> の変換
/// </summary>
internal static class ContainerStatusParser
{
    /// <summary>
    /// Docker の状態文字列を列挙値に変換します。
    /// </summary>
    /// <param name="value">Docker の State 文字列</param>
    /// <returns>対応する状態。不明な値は <see cref="ContainerStatus.Unknown"/></returns>
    public static ContainerStatus Parse(string? value)
    {
        if (Enum.TryParse(value, ignoreCase: true, out ContainerStatus status))
        {
            return status;
        }

        return ContainerStatus.Unknown;
    }
}
