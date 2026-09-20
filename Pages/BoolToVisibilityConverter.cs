using Microsoft.UI.Xaml.Data;

namespace Docklet.Pages;

/// <summary>
/// bool を Visibility に変換します。ConverterParameter に "Invert" を指定すると真偽を反転します。
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var flag = value is bool typed && typed;
        if (string.Equals(parameter as string, "Invert", StringComparison.Ordinal))
        {
            flag = !flag;
        }

        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
