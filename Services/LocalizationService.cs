using Microsoft.Windows.ApplicationModel.Resources;
using Microsoft.Windows.Globalization;

namespace Docklet.Services;

/// <summary>
/// 画面文言の言語切り替えを扱うクラス
/// </summary>
public static class LocalizationService
{
    /// <summary>
    /// 日本語
    /// </summary>
    public const string Japanese = "ja-JP";

    /// <summary>
    /// 英語
    /// </summary>
    public const string English = "en-US";

    private static ResourceLoader? _loader;

    /// <summary>
    /// 言語が切り替わったときに発生します。
    /// </summary>
    public static event Action? LanguageChanged;

    /// <summary>
    /// 現在の表示言語
    /// </summary>
    public static string CurrentLanguage
    {
        get
        {
            var selected = ApplicationLanguages.PrimaryLanguageOverride;
            if (IsSupported(selected))
            {
                return Normalize(selected);
            }

            if (AppSettings.Language is string saved && IsSupported(saved))
            {
                return Normalize(saved);
            }

            foreach (var language in ApplicationLanguages.Languages)
            {
                if (IsSupported(language))
                {
                    return Normalize(language);
                }
            }

            return Japanese;
        }
    }

    /// <summary>
    /// 保存済みの言語を起動時に適用します。
    /// </summary>
    public static void Initialize()
    {
        var language = CurrentLanguage;
        ApplicationLanguages.PrimaryLanguageOverride = language;
        _loader = new ResourceLoader();
    }

    /// <summary>
    /// 表示言語を切り替えます。
    /// </summary>
    /// <param name="language">言語タグ</param>
    public static void SetLanguage(string language)
    {
        var normalized = Normalize(language);
        if (!IsSupported(normalized) || string.Equals(CurrentLanguage, normalized, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        ApplicationLanguages.PrimaryLanguageOverride = normalized;
        AppSettings.Language = normalized;
        _loader = new ResourceLoader();
        LanguageChanged?.Invoke();
    }

    /// <summary>
    /// リソース文字列を取得します。
    /// </summary>
    /// <param name="name">リソース名</param>
    /// <returns>文言</returns>
    public static string GetString(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var key = name.Replace('.', '/');
        try
        {
            var value = (_loader ??= new ResourceLoader()).GetString(key);
            return string.IsNullOrEmpty(value) ? name : value;
        }
        catch (Exception)
        {
            return name;
        }
    }

    private static bool IsSupported(string? language) =>
        !string.IsNullOrWhiteSpace(language)
        && (language.StartsWith("ja", StringComparison.OrdinalIgnoreCase)
            || language.StartsWith("en", StringComparison.OrdinalIgnoreCase));

    private static string Normalize(string language) =>
        language.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? English : Japanese;
}
