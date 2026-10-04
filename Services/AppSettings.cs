using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Docklet.Services;

/// <summary>
/// パッケージ ID に依存しない、ユーザーごとの設定保存。
/// </summary>
internal static class AppSettings
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Docklet");
    private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "settings.json");
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };
    private static readonly object Gate = new();
    private static SettingsData? _data;

    public static string? Language
    {
        get => Read(data => data.Language);
        set => Update(data => data.Language = string.IsNullOrEmpty(value) ? null : value);
    }

    /// <summary>
    /// コンテナーの操作先 (WSL 内の Docker または wslc)
    /// </summary>
    public static ContainerBackend Backend
    {
        get => Read(data => data.Backend);
        set => Update(data => data.Backend = value);
    }

    private static T Read<T>(Func<SettingsData, T> selector)
    {
        lock (Gate)
        {
            return selector(_data ??= Load());
        }
    }

    private static void Update(Action<SettingsData> change)
    {
        lock (Gate)
        {
            _data ??= Load();
            change(_data);
            try
            {
                Directory.CreateDirectory(SettingsDirectory);
                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(_data, JsonOptions));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Debug.WriteLine($"設定を保存できません: {ex.Message}");
            }
        }
    }

    private static SettingsData Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                return JsonSerializer.Deserialize<SettingsData>(File.ReadAllText(SettingsPath), JsonOptions) ?? new();
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            Debug.WriteLine($"設定を読み込めません: {ex.Message}");
        }

        return new();
    }

    private sealed class SettingsData
    {
        public string? Language { get; set; }
        public ContainerBackend Backend { get; set; } = ContainerBackend.Docker;
    }
}

/// <summary>
/// コンテナーの操作先
/// </summary>
public enum ContainerBackend
{
    /// <summary>
    /// WSL 内の Docker Engine
    /// </summary>
    Docker,

    /// <summary>
    /// wslc (WSL コンテナー)
    /// </summary>
    Wslc,
}
