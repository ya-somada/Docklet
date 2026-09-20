using System.Globalization;

namespace Docklet.Services.Docker.Containers;

/// <summary>
/// コンテナー詳細を取得するメソッド
/// </summary>
internal static class InspectContainer
{
    /// <summary>
    /// Agent メソッド名
    /// </summary>
    public const string MethodName = "containers.inspect";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// コンテナー詳細を取得します。
    /// </summary>
    /// <param name="agent">Agent クライアント</param>
    /// <param name="id">コンテナー ID</param>
    /// <param name="cancellationToken">キャンセル トークン</param>
    /// <returns>コンテナー詳細</returns>
    public static async Task<InspectInfo> InvokeAsync(AgentClient agent, string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var result = await agent.InvokeAsync(MethodName, new Params(id), cancellationToken).ConfigureAwait(false);
        var parsed = result.Deserialize<RawInspect>(JsonOptions);
        if (parsed is null || string.IsNullOrWhiteSpace(parsed.Id))
        {
            throw new InvalidOperationException("Docker からコンテナー詳細を取得できませんでした。");
        }

        return parsed.ToInfo();
    }

    private sealed record Params(string Id);

    private sealed class RawInspect
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string State { get; set; } = "";
        public string StatusText { get; set; } = "";
        public string? Created { get; set; }
        public string? StartedAt { get; set; }
        public List<EnvVar> Env { get; set; } = [];
        public List<RawPort> Ports { get; set; } = [];
        public List<RawMount> Mounts { get; set; } = [];

        public InspectInfo ToInfo() =>
            new(
                Id,
                Name,
                ContainerStatusParser.Parse(State),
                StatusText,
                ParseDate(Created),
                ParseDate(StartedAt),
                Env ?? [],
                (Ports ?? []).ConvertAll(p => new PortMapping(p.ContainerPort, p.Protocol, p.HostIp, p.HostPort)),
                (Mounts ?? []).ConvertAll(m => new MountInfo(m.Type, m.Source, m.Destination, m.ReadOnly)));
    }

    private sealed class RawPort
    {
        public int ContainerPort { get; set; }
        public string Protocol { get; set; } = "";
        public string? HostIp { get; set; }
        public string? HostPort { get; set; }
    }

    private sealed class RawMount
    {
        public string Type { get; set; } = "";
        public string Source { get; set; } = "";
        public string Destination { get; set; } = "";
        public bool ReadOnly { get; set; }
    }

    private static DateTimeOffset? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : null;
    }
}

/// <summary>
/// コンテナー概要に出す詳細
/// </summary>
/// <param name="Id">コンテナー ID</param>
/// <param name="Name">コンテナー名</param>
/// <param name="Status">状態</param>
/// <param name="StatusText">表示用ステータス</param>
/// <param name="Created">作成日時</param>
/// <param name="StartedAt">起動日時</param>
/// <param name="Env">環境変数</param>
/// <param name="Ports">ポート設定</param>
/// <param name="Mounts">マウント設定</param>
public sealed record InspectInfo(
    string Id,
    string Name,
    ContainerStatus Status,
    string StatusText,
    DateTimeOffset? Created,
    DateTimeOffset? StartedAt,
    IReadOnlyList<EnvVar> Env,
    IReadOnlyList<PortMapping> Ports,
    IReadOnlyList<MountInfo> Mounts);

/// <summary>
/// コンテナーの環境変数 1 件
/// </summary>
/// <param name="Name">変数名</param>
/// <param name="Value">値</param>
public sealed record EnvVar(string Name, string Value);

/// <summary>
/// コンテナー側ポート 1 つに対する公開設定
/// </summary>
/// <param name="ContainerPort">コンテナー側ポート番号</param>
/// <param name="Protocol">プロトコル (tcp/udp)</param>
/// <param name="HostIp">公開先のホスト IP。未公開の場合は null</param>
/// <param name="HostPort">公開先のホストポート。未公開の場合は null</param>
public sealed record PortMapping(int ContainerPort, string Protocol, string? HostIp, string? HostPort);

/// <summary>
/// コンテナーに割り当てられたマウント 1 件
/// </summary>
/// <param name="Type">マウント種別 (bind/volume など)</param>
/// <param name="Source">マウント元 (ホストパス、または名前付きボリューム名)</param>
/// <param name="Destination">コンテナー側のマウント先パス</param>
/// <param name="ReadOnly">読み取り専用かどうか</param>
public sealed record MountInfo(string Type, string Source, string Destination, bool ReadOnly);
