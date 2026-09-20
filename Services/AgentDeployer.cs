namespace Docklet.Services;

/// <summary>
/// 同梱の Agent をホームディレクトリへ配置するクラス
/// </summary>
public sealed class AgentDeployer
{
    /// <summary>
    /// Agent のファイル名
    /// </summary>
    public const string AgentFileName = "docklet-agent";

    /// <summary>
    /// ホーム直下の配置先ディレクトリ名
    /// </summary>
    public const string RemoteDirectoryName = ".docklet";

    /// <summary>
    /// アプリに同梱されている Agent のパスを返します。
    /// </summary>
    /// <returns>同梱 Agent のフルパス</returns>
    public static string GetBundledAgentPath() =>
        Path.Combine(AppContext.BaseDirectory, "agent", AgentFileName);

    /// <summary>
    /// Agent を ~/.docklet へ配置します。
    /// </summary>
    /// <param name="cancellationToken">キャンセル トークン</param>
    public async Task DeployAsync(CancellationToken cancellationToken = default)
    {
        var sourcePath = GetBundledAgentPath();
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("同梱の Agent が見つかりません。", sourcePath);
        }

        // TODO: 後で存在かつバージョン一致ならスキップする

        var result = await WslProcess.RunAsync(
            [
                "-e", "sh", "-c",
                "umask 077 && mkdir -p \"$HOME/.docklet\" && chmod 700 \"$HOME/.docklet\" && cp -f \"$(wslpath -a \"$1\")\" \"$HOME/.docklet/docklet-agent\" && chmod 700 \"$HOME/.docklet/docklet-agent\"",
                "_",
                sourcePath
            ],
            cancellationToken).ConfigureAwait(false);

        if (result.ExitCode != 0)
        {
            var detail = string.IsNullOrWhiteSpace(result.Stderr) ? result.Stdout : result.Stderr;
            if (string.IsNullOrWhiteSpace(detail))
            {
                detail = "ホームの .docklet へ Agent を配置できませんでした。";
            }

            throw new InvalidOperationException(detail);
        }
    }
}
