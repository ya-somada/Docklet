using System.Diagnostics;
using System.Text;
using System.Text.Json.Serialization;

namespace Docklet.Services;

/// <summary>
/// Agent を常駐させ、stdin/stdout の JSON でやり取りするクラス
/// </summary>
public sealed class AgentClient : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly SemaphoreSlim _startGate = new(1, 1);
    private readonly SemaphoreSlim _invokeGate = new(1, 1);
    private readonly AgentDeployer _deployer = new();
    private Process? _process;
    private StreamWriter? _stdin;
    private StreamReader? _stdout;
    private bool _started;

    /// <summary>
    /// Agent を配置して起動します。
    /// </summary>
    /// <param name="cancellationToken">キャンセル トークン</param>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _startGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_started && _process is { HasExited: false })
            {
                return;
            }

            await _deployer.DeployAsync(cancellationToken).ConfigureAwait(false);
            StartProcess();
            _started = true;
        }
        finally
        {
            _startGate.Release();
        }
    }

    /// <summary>
    /// Agent メソッドを呼び出し、result を返します。
    /// </summary>
    /// <param name="method">メソッド名</param>
    /// <param name="cancellationToken">キャンセル トークン</param>
    /// <returns>result オブジェクト</returns>
    public Task<JsonElement> InvokeAsync(string method, CancellationToken cancellationToken = default) =>
        InvokeAsync(method, paramsPayload: null, cancellationToken);

    /// <summary>
    /// Agent メソッドを呼び出し、result を返します。
    /// </summary>
    /// <param name="method">メソッド名</param>
    /// <param name="paramsPayload">params</param>
    /// <param name="cancellationToken">キャンセル トークン</param>
    /// <returns>result オブジェクト</returns>
    public async Task<JsonElement> InvokeAsync(string method, object? paramsPayload, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(method);

        await _invokeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        var responseReceived = false;
        try
        {
            await StartAsync(cancellationToken).ConfigureAwait(false);
            if (_process is null or { HasExited: true } || _stdin is null || _stdout is null)
            {
                throw new InvalidOperationException("Agent が起動していません。");
            }

            var request = new AgentRequest(Guid.NewGuid().ToString("N"), method, paramsPayload);
            await _stdin.WriteLineAsync(JsonSerializer.Serialize(request, JsonOptions)).ConfigureAwait(false);
            await _stdin.FlushAsync(cancellationToken).ConfigureAwait(false);

            var line = await _stdout.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(line))
            {
                throw new InvalidOperationException("Agent から応答がありません。");
            }

            var response = JsonSerializer.Deserialize<AgentResponse>(line, JsonOptions)
                ?? throw new InvalidOperationException("Agent の応答を解析できません。");
            if (!string.Equals(response.Id, request.Id, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Agent の応答 ID が要求と一致しません。");
            }

            responseReceived = true;
            if (!response.Ok)
            {
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(response.Error)
                    ? "Agent がエラーを返しました。"
                    : response.Error);
            }

            return response.Result;
        }
        catch when (!responseReceived)
        {
            // 中断した行や遅れて届く応答を次の要求が読み取らないように接続を破棄する。
            // 操作は再送しない。開始・停止・削除は Agent 側で既に実行された可能性がある。
            await DisposeAsync().ConfigureAwait(false);
            throw;
        }
        finally
        {
            _invokeGate.Release();
        }
    }

    /// <summary>
    /// Agent プロセスを終了します。
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _startGate.WaitAsync().ConfigureAwait(false);
        try
        {
            try
            {
                _stdin?.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (IOException)
            {
            }

            if (_process is { HasExited: false })
            {
                try
                {
                    _process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                }
                catch (System.ComponentModel.Win32Exception)
                {
                }
            }

            _process?.Dispose();
            _stdout?.Dispose();
            _process = null;
            _stdin = null;
            _stdout = null;
            _started = false;
        }
        finally
        {
            _startGate.Release();
        }
    }

    private void StartProcess()
    {
        _stdin?.Dispose();
        _stdout?.Dispose();
        _process?.Dispose();

        var remotePath = $"$HOME/{AgentDeployer.RemoteDirectoryName}/{AgentDeployer.AgentFileName}";
        var startInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.SystemDirectory, "wsl.exe"),
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        startInfo.Environment["WSL_UTF8"] = "1";
        startInfo.ArgumentList.Add("-e");
        startInfo.ArgumentList.Add("sh");
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add($"exec \"{remotePath}\"");

        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        try
        {
            if (!process.Start())
            {
                process.Dispose();
                throw new InvalidOperationException("Agent を起動できませんでした。");
            }
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            process.Dispose();
            throw new InvalidOperationException("WSL が見つかりません。", ex);
        }

        _process = process;
        _stdin = process.StandardInput;
        _stdin.NewLine = "\n";
        _stdout = process.StandardOutput;
        DrainStandardError(process);
    }

    private static void DrainStandardError(Process process)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                while (await process.StandardError.ReadLineAsync().ConfigureAwait(false) is { } line)
                {
                    Debug.WriteLine($"[docklet-agent] {line}");
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        });
    }

    private sealed record AgentRequest(string Id, string Method, object? Params);

    private sealed class AgentResponse
    {
        public string Id { get; set; } = "";
        public bool Ok { get; set; }
        public JsonElement Result { get; set; }
        public string? Error { get; set; }
    }
}
