using System.ComponentModel;
using System.Diagnostics;

namespace Docklet.Services;

/// <summary>
/// コンテナーに exec 接続する外部ターミナルを起動するクラス
/// </summary>
public static class TerminalLauncher
{
    /// <summary>
    /// 指定したコンテナーに exec 接続するターミナルを開きます。
    /// Windows Terminal があればそこで開き、無ければ既定のコンソールを開きます。
    /// 接続先は設定の Docker (WSL 内) / wslc に従います。
    /// </summary>
    /// <param name="containerId">コンテナー ID</param>
    public static void Open(string containerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerId);
        if (containerId.Length is < 12 or > 64 || !containerId.All(char.IsAsciiHexDigit))
        {
            throw new ArgumentException("コンテナー ID は 12～64 桁の 16 進数で指定してください。", nameof(containerId));
        }

        var (fileName, arguments) = AppSettings.Backend == ContainerBackend.Wslc
            ? BuildWslcCommand(containerId)
            : BuildDockerCommand(containerId);
        var terminalPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "WindowsApps", "wt.exe");

        if (TryStart(terminalPath, [fileName, .. arguments]))
        {
            return;
        }

        StartProcess(fileName, arguments);
    }

    private static (string FileName, List<string> Arguments) BuildDockerCommand(string containerId)
    {
        // bash の有無を確かめるのに sh へ依存すると、sh 自体が無いイメージ (distroless/scratch 系) で
        // bash があっても一切試されないまま失敗するため、外側 (WSL 側) の sh で exec ごと 2 回に分けて試す。
        // ID はシェルのコードに埋め込まず、位置引数として渡す。
        // GUI と同じソケットを使い、Docker の既定 context による接続先の違いを防ぐ。
        const string shellCommand = "docker --host unix:///var/run/docker.sock exec -it \"$1\" bash || docker --host unix:///var/run/docker.sock exec -it \"$1\" sh";
        var wslPath = Path.Combine(Environment.SystemDirectory, "wsl.exe");
        return (wslPath, ["-e", "sh", "-c", shellCommand, "docklet", containerId]);
    }

    private static (string FileName, List<string> Arguments) BuildWslcCommand(string containerId)
    {
        // wslc は Windows 側のコマンドなので cmd 経由で bash → sh の順に試す。
        // コンテナー ID は 16 進数に検証済みのため、コマンド文字列へ埋め込んでも安全。
        var cmdPath = Path.Combine(Environment.SystemDirectory, "cmd.exe");
        return (cmdPath, ["/c", $"wslc exec -it {containerId} bash || wslc exec -it {containerId} sh"]);
    }

    private static bool TryStart(string fileName, List<string> arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false,
            };
            foreach (var arg in arguments)
            {
                startInfo.ArgumentList.Add(arg);
            }

            using var process = Process.Start(startInfo);
            return process is not null;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }

    private static void StartProcess(string fileName, List<string> arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
        };
        foreach (var arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        try
        {
            Process.Start(startInfo);
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException($"{Path.GetFileName(fileName)} が見つかりません。", ex);
        }
    }
}
