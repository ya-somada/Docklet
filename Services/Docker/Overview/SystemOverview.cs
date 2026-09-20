namespace Docklet.Services.Docker.Overview;

/// <summary>
/// Docker エンジン全体の概要 (件数・バージョン)
/// </summary>
/// <param name="RunningContainers">起動中のコンテナー数</param>
/// <param name="TotalContainers">コンテナー総数</param>
/// <param name="Images">イメージ数</param>
/// <param name="Volumes">ボリューム数</param>
/// <param name="Networks">ネットワーク数 (既定の "none" を除く)</param>
/// <param name="ServerVersion">Docker Engine のバージョン</param>
/// <param name="OperatingSystem">Docker Engine が動作している OS</param>
public sealed record SystemOverview(
    int RunningContainers,
    int TotalContainers,
    int Images,
    int Volumes,
    int Networks,
    string ServerVersion,
    string OperatingSystem);
