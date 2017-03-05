using System;
using System.Globalization;
using System.Windows.Data;

namespace FFXIVBuff.Converters
{
    internal class UStatusTimeConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null ||
                value.Length != 3 ||
                !(value[0] is bool) ||
                !(value[1] is float) ||
                !(value[2] is bool))
                return " ";

            // 0 bool   False:분 / True:회
            // 1 float  남은 시간
            // 2 bool   소수점 표시
            
            var isCount     = (bool)value[0];
            var remain      = (float)value[1];
            var showDecimal = (bool)value[2];
            
            if (remain == 0)
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
