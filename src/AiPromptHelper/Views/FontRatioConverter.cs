using System.Globalization;
using System.Windows;
using System.Windows.Data;
namespace AiPromptHelper.Views;
public sealed class FontRatioConverter:IValueConverter
{
    public object Convert(object value,Type targetType,object parameter,CultureInfo culture)=>value is double size?size*(double)parameter:14.6667;
    public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>throw new NotSupportedException();
}
