using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CsvHelper;
using HighWire16Stacks.Utilities;

namespace HighWire16Stacks.Core
{
    internal static class FResource
    {
        private static readonly BitmapSource IconBitmap;
        private static readonly IDictionary<int, Int32Rect>   IconPosition = new SortedDictionary<int, Int32Rect>();
        private static readonly IDictionary<int, ImageSource> IconCollection = new SortedDictionary<int, ImageSource>();

        public static readonly IList<FStatus> StatusList = new SortedList<FStatus>();
        public static readonly IDictionary<int, FStatus> StatusListDic = new SortedDictionary<int, FStatus>();

        static FResource()
        {
            var bitmap = Properties.Resources.icons;
            BitmapData scan0 = null;

            try
            {
                scan0 = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                IconBitmap = BitmapSource.Create(
                    scan0.Width,
                    scan0.Height,
                    96,
                    96,
                    PixelFormats.Bgra32,
                    null,
                    scan0.Scan0,
                    scan0.Stride * scan0.Height,
                    scan0.Stride);
            }
            finally
            { 
                if (scan0 != null)
                    bitmap.UnlockBits(scan0);
            }


            IconCollection.Add(0, null);
        }

        public static void ReadResources()
        {            
            StringReader reader;
            CsvReader csv;

            using (reader = new StringReader(Properties.Resources.icons_pos))
            {
                int id, x, y;

                using (csv = new CsvReader(reader))
                {
                    while (csv.Read())
                    {
                        if (csv.TryGetField<int>(0, out id) &&
                            csv.TryGetField<int>(1, out x) &&
                            csv.TryGetField<int>(2, out y))
                        {
                            IconPosition.Add(id, new Int32Rect(x, y, 24, 32));
                        }
                    }
                }                
            }

            FStatus status = new FStatus(0, null, null, 0, 0, false, false, false);
            StatusList.Add(status);
            StatusListDic.Add(0, status);

            using (reader = new StringReader(Properties.Resources.status_exh_ko))
            {
                int     id;
                string  name;
                string  desc;
                int     icon;
                int     buffStack;
                int     isBad;
                bool    isNonExpries;

                using (csv = new CsvReader(reader))
                {
                    while (csv.Read())
                    {
                        if (csv.TryGetField<int>   ((int)('A' - 'A'), out id)           &&
                            csv.TryGetField<string>((int)('B' - 'A'), out name)         && !string.IsNullOrEmpty(name)    &&
                            csv.TryGetField<string>((int)('C' - 'A'), out desc)         &&
                            csv.TryGetField<int>   ((int)('D' - 'A'), out icon)         && IconPosition.ContainsKey(icon) &&
                            csv.TryGetField<int>   ((int)('E' - 'A'), out buffStack)    &&
                            csv.TryGetField<int>   ((int)('F' - 'A'), out isBad)        &&
                            csv.TryGetField<bool>  ((int)('O' - 'A'), out isNonExpries))
                        {
                            status = new FStatus(id, name, desc, icon, buffStack, isBad == 2, isNonExpries, Settings.Instance.IsChecked(id));
                            StatusList.Add(status);
                            StatusListDic.Add(id, status);
                        }
                    }
                }
            }
        }

        public static ImageSource GetImage(int iconId)
        {
            lock (IconCollection)
            {
                if (IconCollection.ContainsKey(iconId))
                    return IconCollection[iconId];
                else
                {
                    try
                    {
                        var image = new CroppedBitmap(IconBitmap, IconPosition[iconId]);
                        IconCollection.Add(iconId, image);

                        return image;
                    }
                    catch
                    {
                        return null;
                    }

                }
            }
        }
    }
}
