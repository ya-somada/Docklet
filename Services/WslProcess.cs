using System.Diagnostics;
using System.Text;

namespace Docklet.Services;

/// <summary>
/// wsl.exe を起動してコマンドを実行するクラス
/// </summary>
internal static class WslProcess
{
    /// <summary>
    /// wsl.exe を実行します。
    /// </summary>
    /// <param name="arguments">wsl.exe に渡す引数</param>
    /// <param name="cancellationToken">キャンセル トークン</param>
    /// <returns>終了コードと標準出力・標準エラー</returns>
    public static Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default) =>
        RunAsync(Path.Combine(Environment.SystemDirectory, "wsl.exe"), arguments, cancellationToken);

    /// <summary>
    /// 指定した実行ファイルを実行します。wslc.exe など WSL 関連の CLI に使います。
    /// </summary>
    /// <param name="fileName">実行ファイル名またはパス</param>
    /// <param name="arguments">渡す引数</param>
    /// <param name="cancellationToken">キャンセル トークン</param>
    /// <returns>終了コードと標準出力・標準エラー</returns>
    public static async Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        startInfo.Environment["WSL_UTF8"] = "1";
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException($"{Path.GetFileName(fileName)} を起動できませんでした。");
            }
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            throw new InvalidOperationException($"{Path.GetFileName(fileName)} が見つかりません。", ex);
        }

        try
        {
            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            return (process.ExitCode, (await stdoutTask).Trim(), (await stderrTask).Trim());
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (System.ComponentModel.Win32Exception)
        {
        }
    }
}
