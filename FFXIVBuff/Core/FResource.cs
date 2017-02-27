using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CsvHelper;
using FFXIVBuff.Object;

namespace FFXIVBuff.Core
{
    internal static class FResource
    {
        private static readonly BitmapSource IconBitmap;
        private static readonly IDictionary<int, Int32Rect> IconPosition = new SortedDictionary<int, Int32Rect>();
        private static readonly IDictionary<int, ImageSource> IconCollection = new SortedDictionary<int, ImageSource>();

        public static readonly IList<FStatus> StatusList = new SortedList<FStatus>();
        public static readonly IDictionary<int, FStatus> StatusListDic = new SortedDictionary<int, FStatus>();

        private static class NativeMethods
        {
            [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DeleteObject([In] IntPtr hObject);
        }

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
            using (var reader = new StringReader(Properties.Resources.icons_pos))
            {
                var csv = new CsvReader(reader);
                
                int id, x, y;

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

            FStatus status;

            status = new FStatus(0, null, null, 0, false);
            StatusList.Add(status);
            StatusListDic.Add(0, status);

            using (var reader = new StringReader(Properties.Resources.status_exh_ko))
            {
                var csv = new CsvReader(reader);

                int id;
                int icon;
                string name;
                string desc;
                int isBuff;
                while (csv.Read())
                {
                    if (csv.TryGetField<int>(0, out id) &&
                        csv.TryGetField<string>(1, out name) &&
                        csv.TryGetField<string>(2, out desc) &&
                        csv.TryGetField<int>(3, out icon) &&
                        csv.TryGetField<int>(5, out isBuff) &&
                        IconPosition.ContainsKey(icon) &&
                        !string.IsNullOrEmpty(name))
                    {
                        status = new FStatus(id, name, desc, icon, isBuff == 1);
                        StatusList.Add(status);
                        StatusListDic.Add(id, status);
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
