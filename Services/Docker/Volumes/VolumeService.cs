namespace Docklet.Services.Docker.Volumes;

/// <summary>
/// ボリューム関連の Docker 操作
/// </summary>
public sealed class VolumeService
{
    private readonly AgentClient _agent;

    /// <summary>
    /// Agent クライアントを指定して初期化します。
    /// </summary>
    /// <param name="agent">Agent クライアント</param>
    public VolumeService(AgentClient agent)
    {
        _agent = agent;
    }

    /// <summary>
    /// ボリューム一覧を取得します。
    /// </summary>
    /// <param name="cancellationToken">キャンセル トークン</param>
    /// <returns>ボリューム一覧</returns>
    public Task<IReadOnlyList<VolumeInfo>> ListAsync(CancellationToken cancellationToken = default) =>
        ListVolumes.InvokeAsync(_agent, cancellationToken);

    /// <summary>
    /// ボリュームを削除します。
    /// </summary>
    /// <param name="name">ボリューム名</param>
    /// <param name="cancellationToken">キャンセル トークン</param>
    public Task RemoveAsync(string name, CancellationToken cancellationToken = default) =>
        RemoveVolume.InvokeAsync(_agent, name, cancellationToken);
}
