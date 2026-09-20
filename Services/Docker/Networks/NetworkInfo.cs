namespace Docklet.Services.Docker.Networks;

/// <summary>
/// Docker ネットワークの一覧表示用データ
/// </summary>
/// <param name="Id">ネットワーク ID</param>
/// <param name="Name">ネットワーク名</param>
/// <param name="Driver">ドライバー</param>
/// <param name="Scope">スコープ</param>
/// <param name="CreatedAt">作成日時</param>
/// <param name="Subnet">サブネット (CIDR)</param>
/// <param name="Gateway">ゲートウェイ</param>
/// <param name="ContainerCount">接続中のコンテナー数 (停止中も含む)</param>
public sealed record NetworkInfo(
    string Id,
    string Name,
    string Driver,
    string Scope,
    DateTimeOffset? CreatedAt,
    string Subnet,
    string Gateway,
    int ContainerCount)
{
    /// <summary>
    /// Docker の既定ネットワーク (bridge / host / none) かどうか。
    /// これらは Docker Engine 側の制約で削除できません。
    /// </summary>
    public bool IsBuiltIn => Name is "bridge" or "host" or "none";

    /// <summary>
    /// 削除できる状態かどうか (コンテナーが接続中でないか)
    /// </summary>
    public bool CanRemove => ContainerCount == 0;
}
