namespace Docklet.Services.Docker.Images;

/// <summary>
/// イメージ関連の Docker 操作
/// </summary>
public sealed class ImageService
{
    private readonly AgentClient _agent;

    /// <summary>
    /// Agent クライアントを指定して初期化します。
    /// </summary>
    /// <param name="agent">Agent クライアント</param>
    public ImageService(AgentClient agent)
    {
        _agent = agent;
    }

    /// <summary>
    /// イメージ一覧を取得します。
    /// </summary>
    /// <param name="cancellationToken">キャンセル トークン</param>
    /// <returns>イメージ一覧</returns>
    public Task<IReadOnlyList<ImageInfo>> ListAsync(CancellationToken cancellationToken = default) =>
        ListImages.InvokeAsync(_agent, cancellationToken);

    /// <summary>
    /// イメージを削除します。
    /// </summary>
    /// <param name="id">イメージ ID</param>
    /// <param name="cancellationToken">キャンセル トークン</param>
    public Task RemoveAsync(string id, CancellationToken cancellationToken = default) =>
        RemoveImage.InvokeAsync(_agent, id, cancellationToken);
}
