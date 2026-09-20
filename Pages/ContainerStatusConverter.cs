using Microsoft.UI.Xaml.Data;

namespace Docklet.Pages;

/// <summary>
/// コンテナー状態を表示用の文言・色に変換します。
/// </summary>
public sealed class ContainerStatusConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var status = value is ContainerStatus typed ? typed : ContainerStatus.Unknown;
        return (parameter as string) switch
        {
            "Foreground" => Brush(status, background: false),
            "Background" => Brush(status, background: true),
            _ => LocalizationService.GetString($"ContainerStatus_{status}"),
        };
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();

    private static object Brush(ContainerStatus status, bool background)
    {
        var key = (status, background) switch
        {
            (ContainerStatus.Running, true) => "SystemFillColorSuccessBackgroundBrush",
            (ContainerStatus.Running, false) => "SystemFillColorSuccessBrush",
            (ContainerStatus.Paused, true) => "SystemFillColorCautionBackgroundBrush",
            (ContainerStatus.Paused, false) => "SystemFillColorCautionBrush",
            (ContainerStatus.Restarting, true) => "SystemFillColorCautionBackgroundBrush",
            (ContainerStatus.Restarting, false) => "SystemFillColorCautionBrush",
            (ContainerStatus.Removing, true) => "SystemFillColorCautionBackgroundBrush",
            (ContainerStatus.Removing, false) => "SystemFillColorCautionBrush",
            (ContainerStatus.Dead, true) => "SystemFillColorCriticalBackgroundBrush",
            (ContainerStatus.Dead, false) => "SystemFillColorCriticalBrush",
            (_, true) => "SystemFillColorNeutralBackgroundBrush",
            (_, false) => "TextFillColorSecondaryBrush",
        };

        return Application.Current.Resources[key];
    }
}
