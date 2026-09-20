using Microsoft.UI.Xaml.Data;

namespace Docklet.Pages;

/// <summary>
/// イメージの使用中コンテナー数を表示用の文言・可視性・活性状態に変換します。
/// </summary>
public sealed class ImageInUseConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var count = value is int typed ? typed : 0;
        return (parameter as string) switch
        {
            "Visibility" => count > 0 ? Visibility.Visible : Visibility.Collapsed,
            "Enabled" => count == 0,
            _ => string.Format(LocalizationService.GetString("Images_InUse"), count),
        };
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
