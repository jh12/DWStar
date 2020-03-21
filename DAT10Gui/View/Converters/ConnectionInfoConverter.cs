using System;
using System.Globalization;
using System.Windows.Data;
using DAT10.Core.Setting;

namespace DAT10Gui.View.Converters
{
    public class ConnectionInfoConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 2)
                return null;

            var connectionstring = values[0].ToString();
            var sourceType = values[1].ToString();

            return new ConnectionInfo(connectionstring, sourceType);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
