using System.Diagnostics;

namespace Docklet.Services;

/// <summary>
/// パッケージ ID に依存しない、ユーザーごとの設定保存。
/// </summary>
internal static class AppSettings
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Docklet");
    private static readonly string LanguagePath = Path.Combine(SettingsDirectory, "language.txt");

    public static string? Language
    {
        get
        {
            try
            {
                return File.Exists(LanguagePath) ? File.ReadAllText(LanguagePath).Trim() : null;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Debug.WriteLine($"表示言語の設定を読み込めません: {ex.Message}");
                return null;
            }
        }
        set
        {
            try
            {
                Directory.CreateDirectory(SettingsDirectory);
                File.WriteAllText(LanguagePath, value ?? string.Empty);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Debug.WriteLine($"表示言語の設定を保存できません: {ex.Message}");
            }
        }
    }
}
