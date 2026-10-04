using System.Globalization;
using System.Text.Json.Nodes;

namespace Docklet.Services;

/// <summary>
/// wslc.exe を呼び出し、Agent と同じ形の result を返すクラス
/// </summary>
internal static class WslcBackend
{
    private const string FileName = "wslc.exe";
    private const string LogTail = "1000";

    private static readonly JsonSerializerOptions ResultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// wslc を実行できるか確認します。
    /// </summary>
    /// <param name="cancellationToken">キャンセル トークン</param>
    public static async Task CheckAsync(CancellationToken cancellationToken = default) =>
        _ = await RunAsync(["version"], cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Agent メソッドと同名の操作を wslc で実行し、result を返します。
    /// </summary>
    /// <param name="method">メソッド名</param>
    /// <param name="paramsPayload">params</param>
    /// <param name="cancellationToken">キャンセル トークン</param>
    /// <returns>result オブジェクト</returns>
    public static async Task<JsonElement> InvokeAsync(string method, object? paramsPayload, CancellationToken cancellationToken)
    {
        object result = method switch
        {
            "system.overview" => await GetOverviewAsync(cancellationToken).ConfigureAwait(false),
            "containers.runningCount" => new { Count = (await ListContainersAsync(all: false, cancellationToken).ConfigureAwait(false)).Count },
            "containers.list" => await ListContainerItemsAsync(cancellationToken).ConfigureAwait(false),
            "containers.start" => await RunEmptyAsync(["start", GetId(paramsPayload)], cancellationToken).ConfigureAwait(false),
            "containers.stop" => await RunEmptyAsync(["stop", GetId(paramsPayload)], cancellationToken).ConfigureAwait(false),
            "containers.inspect" => await InspectContainerAsync(GetId(paramsPayload), cancellationToken).ConfigureAwait(false),
            "containers.logs" => await GetLogsAsync(GetId(paramsPayload), cancellationToken).ConfigureAwait(false),
            "images.list" => await ListImagesAsync(cancellationToken).ConfigureAwait(false),
            "images.remove" => await RunEmptyAsync(["rmi", GetId(paramsPayload)], cancellationToken).ConfigureAwait(false),
            "volumes.list" => await ListVolumesAsync(cancellationToken).ConfigureAwait(false),
            "volumes.remove" => await RunEmptyAsync(["volume", "rm", GetName(paramsPayload)], cancellationToken).ConfigureAwait(false),
            "networks.list" => await ListNetworksAsync(cancellationToken).ConfigureAwait(false),
            "networks.remove" => await RunEmptyAsync(["network", "rm", GetName(paramsPayload)], cancellationToken).ConfigureAwait(false),
            _ => throw new InvalidOperationException($"wslc ではメソッド '{method}' に対応していません。"),
        };

        return JsonSerializer.SerializeToElement(result, ResultOptions);
    }

    private static async Task<object> GetOverviewAsync(CancellationToken cancellationToken)
    {
        var containersTask = ListContainersAsync(all: true, cancellationToken);
        var imagesTask = RunJsonLinesAsync(["images", "--format", "json"], cancellationToken);
        var volumesTask = RunJsonLinesAsync(["volume", "list", "--format", "json"], cancellationToken);
        var networksTask = RunJsonLinesAsync(["network", "list", "--format", "json"], cancellationToken);
        var infoTask = RunAsync(["info", "--format", "json"], cancellationToken);
        await Task.WhenAll(containersTask, imagesTask, volumesTask, networksTask, infoTask).ConfigureAwait(false);

        var containers = containersTask.Result;
        var version = JsonNode.Parse(infoTask.Result)?["Server"]?["SessionManagerVersion"]?.GetValue<string>();
        return new
        {
            RunningContainers = containers.Count(item => Str(item, "State") == "running"),
            TotalContainers = containers.Count,
            Images = imagesTask.Result.Count,
            Volumes = volumesTask.Result.Count,
            Networks = networksTask.Result.Count(item => Str(item, "Name") != "none"),
            ServerVersion = version ?? "",
            OperatingSystem = "WSL Containers (wslc)",
        };
    }

    private static async Task<object> ListContainerItemsAsync(CancellationToken cancellationToken)
    {
        var containers = await ListContainersAsync(all: true, cancellationToken).ConfigureAwait(false);
        return new
        {
            Containers = containers.ConvertAll(item => new
            {
                Id = Str(item, "ID"),
                Name = FirstName(Str(item, "Names"), Str(item, "ID")),
                State = Str(item, "State"),
                Status = Str(item, "Status"),
            }),
        };
    }

    private static async Task<object> InspectContainerAsync(string id, CancellationToken cancellationToken)
    {
        var inspectTask = RunAsync(["inspect", id], cancellationToken);
        var listTask = ListContainersAsync(all: true, cancellationToken);
        await Task.WhenAll(inspectTask, listTask).ConfigureAwait(false);

        var raw = JsonNode.Parse(inspectTask.Result)?.AsArray().FirstOrDefault()
            ?? throw new InvalidOperationException("wslc からコンテナー詳細を取得できませんでした。");
        var fullId = Str(raw, "Id");
        var state = Str(raw["State"], "Status");
        var statusText = listTask.Result.FirstOrDefault(item => fullId.StartsWith(Str(item, "ID"), StringComparison.Ordinal));

        var env = (raw["Config"]?["Env"]?.AsArray() ?? [])
            .Select(entry => entry?.GetValue<string>() ?? "")
            .Select(entry => entry.Split('=', 2))
            .Where(parts => parts[0].Length > 0)
            .Select(parts => new { Name = parts[0], Value = parts.Length > 1 ? parts[1] : "" })
            .OrderBy(item => item.Name, StringComparer.Ordinal)
            .ToList();

        var ports = new List<PortItem>();
        if (raw["Ports"] is JsonObject portMap)
        {
            foreach (var (key, bindings) in portMap)
            {
                var parts = key.Split('/', 2);
                if (parts.Length != 2 || !int.TryParse(parts[0], out var containerPort))
                {
                    continue;
                }

                var items = bindings?.AsArray() ?? [];
                if (items.Count == 0)
                {
                    ports.Add(new PortItem(containerPort, parts[1], "", ""));
                    continue;
                }

                ports.AddRange(items.Select(binding =>
                    new PortItem(containerPort, parts[1], Str(binding, "HostIp"), Str(binding, "HostPort"))));
            }
        }

        var mounts = (raw["Mounts"]?.AsArray() ?? []).Select(mount =>
        {
            var type = Str(mount, "Type");
            var name = Str(mount, "Name");
            return new
            {
                Type = type,
                Source = type == "volume" && name.Length > 0 ? name : Str(mount, "Source"),
                Destination = Str(mount, "Destination"),
                ReadOnly = !(mount?["ReadWrite"]?.GetValue<bool>() ?? true),
            };
        }).ToList();

        return new
        {
            Id = fullId,
            Name = Str(raw, "Name").TrimStart('/'),
            State = state,
            StatusText = statusText is null ? state : Str(statusText, "Status"),
            Created = NormalizeTime(Str(raw, "Created")),
            StartedAt = NormalizeTime(Str(raw["State"], "StartedAt")),
            Env = env,
            Ports = ports.OrderBy(port => port.ContainerPort).ThenBy(port => port.Protocol, StringComparer.Ordinal).ThenBy(port => port.HostPort, StringComparer.Ordinal).ToList(),
            Mounts = mounts,
        };
    }

    private static async Task<object> GetLogsAsync(string id, CancellationToken cancellationToken)
    {
        var text = await RunAsync(["logs", "--timestamps", "--tail", LogTail, id], cancellationToken).ConfigureAwait(false);
        return new { Text = text };
    }

    private static async Task<object> ListImagesAsync(CancellationToken cancellationToken)
    {
        var rows = await RunJsonLinesAsync(["images", "--no-trunc", "--format", "json"], cancellationToken).ConfigureAwait(false);
        if (rows.Count == 0)
        {
            return new { Images = Array.Empty<object>() };
        }

        // 一覧の Size は "8.42MB" のような丸めた文字列なので、inspect で正確な値を取得する。
        var ids = rows.Select(row => Str(row, "ID")).Distinct().ToList();
        var inspectText = await RunAsync(["inspect", "--type", "image", .. ids], cancellationToken).ConfigureAwait(false);
        var details = (JsonNode.Parse(inspectText)?.AsArray() ?? [])
            .ToDictionary(item => Str(item, "Id"), item => item!, StringComparer.Ordinal);

        var images = ids.ConvertAll(id =>
        {
            var tags = rows.Where(row => Str(row, "ID") == id)
                .Select(row => $"{Str(row, "Repository")}:{Str(row, "Tag")}")
                .Where(tag => tag != ":" && !tag.StartsWith("<none>", StringComparison.Ordinal))
                .Distinct();
            details.TryGetValue(id, out var detail);
            return new
            {
                Id = id,
                Tags = string.Join(", ", tags),
                CreatedAt = NormalizeTime(Str(detail, "Created")),
                Size = detail?["Size"]?.GetValue<long>() ?? 0,
                ContainerCount = int.TryParse(Str(rows.First(row => Str(row, "ID") == id), "Containers"), out var count) ? count : 0,
            };
        });

        return new { Images = images };
    }

    private static async Task<object> ListVolumesAsync(CancellationToken cancellationToken)
    {
        var rows = await RunJsonLinesAsync(["volume", "list", "--format", "json"], cancellationToken).ConfigureAwait(false);
        var names = rows.Select(row => Str(row, "Name")).ToList();
        var created = new Dictionary<string, string>(StringComparer.Ordinal);
        if (names.Count > 0)
        {
            var inspectText = await RunAsync(["volume", "inspect", .. names], cancellationToken).ConfigureAwait(false);
            foreach (var item in JsonNode.Parse(inspectText)?.AsArray() ?? [])
            {
                created[Str(item, "Name")] = Str(item, "CreatedAt");
            }
        }

        return new
        {
            Volumes = rows.ConvertAll(row => new
            {
                Name = Str(row, "Name"),
                Driver = Str(row, "Driver"),
                Mountpoint = Str(row, "Mountpoint"),
                CreatedAt = created.GetValueOrDefault(Str(row, "Name"), ""),
            }),
        };
    }

    private static async Task<object> ListNetworksAsync(CancellationToken cancellationToken)
    {
        var rowsTask = RunJsonLinesAsync(["network", "list", "--no-trunc", "--format", "json"], cancellationToken);
        var containersTask = ListContainersAsync(all: true, cancellationToken);
        await Task.WhenAll(rowsTask, containersTask).ConfigureAwait(false);
        var rows = rowsTask.Result;

        var inspected = new Dictionary<string, JsonNode>(StringComparer.Ordinal);
        if (rows.Count > 0)
        {
            var inspectText = await RunAsync(["network", "inspect", .. rows.Select(row => Str(row, "Name"))], cancellationToken)
                .ConfigureAwait(false);
            foreach (var item in JsonNode.Parse(inspectText)?.AsArray() ?? [])
            {
                inspected[Str(item, "Name")] = item!;
            }
        }

        var counts = containersTask.Result
            .SelectMany(item => Str(item, "Networks").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .GroupBy(name => name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        return new
        {
            Networks = rows.ConvertAll(row =>
            {
                var name = Str(row, "Name");
                inspected.TryGetValue(name, out var detail);
                var config = (detail?["IPAM"]?["Config"]?.AsArray() ?? []).ToList();
                return new
                {
                    Id = Str(row, "ID"),
                    Name = name,
                    Driver = Str(row, "Driver"),
                    Scope = Str(row, "Scope"),
                    CreatedAt = NormalizeTime(Str(detail, "Created")),
                    Subnet = string.Join(", ", config.Select(item => Str(item, "Subnet")).Where(value => value.Length > 0)),
                    Gateway = string.Join(", ", config.Select(item => Str(item, "Gateway")).Where(value => value.Length > 0)),
                    ContainerCount = counts.GetValueOrDefault(name),
                };
            }),
        };
    }

    private static Task<List<JsonNode>> ListContainersAsync(bool all, CancellationToken cancellationToken) =>
        RunJsonLinesAsync(all
            ? ["list", "--all", "--no-trunc", "--format", "json"]
            : ["list", "--no-trunc", "--format", "json"], cancellationToken);

    /// <summary>
    /// wslc の --format json (1 行 1 オブジェクト) の出力を解析します。
    /// </summary>
    private static async Task<List<JsonNode>> RunJsonLinesAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var text = await RunAsync(arguments, cancellationToken).ConfigureAwait(false);
        return text
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => JsonNode.Parse(line))
            .OfType<JsonNode>()
            .ToList();
    }

    private static async Task<object> RunEmptyAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        _ = await RunAsync(arguments, cancellationToken).ConfigureAwait(false);
        return new { };
    }

    private static async Task<string> RunAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var result = await WslProcess.RunAsync(FileName, arguments, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode == 0)
        {
            // ログは stdout と stderr の両方に出るため、成功時の stderr は logs のときだけ付ける。
            return arguments[0] == "logs" && result.Stderr.Length > 0
                ? string.Join('\n', new[] { result.Stdout, result.Stderr }.Where(part => part.Length > 0))
                : result.Stdout;
        }

        // 先頭行にエラーメッセージ、続く行にエラー コードや案内文が出力される。
        var detail = string.IsNullOrWhiteSpace(result.Stderr) ? result.Stdout : result.Stderr;
        var message = detail.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        throw new InvalidOperationException(string.IsNullOrWhiteSpace(message) ? "wslc がエラーを返しました。" : message);
    }

    private static string GetId(object? paramsPayload) => GetString(paramsPayload, "id", "ID が指定されていません。");

    private static string GetName(object? paramsPayload) => GetString(paramsPayload, "name", "名前が指定されていません。");

    private static string GetString(object? paramsPayload, string key, string error)
    {
        var element = JsonSerializer.SerializeToElement(paramsPayload, ResultOptions);
        if (element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(key, out var value)
            && value.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(value.GetString()))
        {
            return value.GetString()!.Trim();
        }

        throw new InvalidOperationException(error);
    }

    private sealed record PortItem(int ContainerPort, string Protocol, string HostIp, string HostPort);

    private static string Str(JsonNode? node, string key) =>
        node?[key] is JsonValue value && value.TryGetValue<string>(out var text) ? text : "";

    private static string FirstName(string names, string id) =>
        names.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault()?.TrimStart('/')
        ?? (id.Length > 12 ? id[..12] : id);

    /// <summary>
    /// 日時文字列を RFC 3339 (UTC) に揃えます。未設定 (0001 年) や解析できない値は null にします。
    /// </summary>
    private static string? NormalizeTime(string value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed) && parsed.Year > 1
            ? parsed.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)
            : null;
}
