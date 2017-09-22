using System;
using System.Globalization;
using System.Windows.Data;

namespace HighWire16Stacks.Converters
{
    internal class UStatusTimeConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null ||
                !(value[0] is bool  isCount) ||
                !(value[1] is float remain) || remain == 0 ||
                !(value[2] is bool  showDecimal))
                return " ";

            if (!isCount)
            {

                if (showDecimal)
                {
                    if (remain > 60)
                        return string.Format("{0:##0.0}분", remain / 60);
                    else
                        return string.Format("{0:##0.0}", remain);
                }
                else
                {
                    if (remain > 60)
                        return string.Format("{0:##0}분", Math.Floor(remain / 60));
                    else
                    {
                        var v = Math.Round(remain, 0);

                        return v == 0 ? " " : string.Format("{0:##0}", v);
                    }
                }
            }
            else
            {
                return string.Format("{0:##0}회", remain);
            }
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
