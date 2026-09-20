namespace Docklet.Services.Docker;

/// <summary>
/// Agent 経由で Docker の情報を取得するクラス
/// </summary>
public sealed class DockerService
{
    /// <summary>
    /// コンテナー操作
    /// </summary>
    public ContainerService Containers { get; }

    /// <summary>
    /// ボリューム操作
    /// </summary>
    public VolumeService Volumes { get; }

    /// <summary>
    /// ネットワーク操作
    /// </summary>
    public NetworkService Networks { get; }

    /// <summary>
    /// イメージ操作
    /// </summary>
    public ImageService Images { get; }

    /// <summary>
    /// エンジン概要の取得
    /// </summary>
    public OverviewService Overview { get; }

    /// <summary>
    /// Agent クライアントを指定して初期化します。
    /// </summary>
    /// <param name="agent">Agent クライアント</param>
    public DockerService(AgentClient agent)
    {
        Containers = new ContainerService(agent);
        Volumes = new VolumeService(agent);
        Networks = new NetworkService(agent);
        Images = new ImageService(agent);
        Overview = new OverviewService(agent);
    }
}
