using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
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
        public static readonly IDictionary<int, FStatus> IconToStatus = new SortedDictionary<int, FStatus>();

        private static class NativeMethods
        {
            [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DeleteObject([In] IntPtr hObject);
        }

        static FResource()
        {
            var handle = Properties.Resources.icons.GetHbitmap();
            try
            {
                IconBitmap = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                NativeMethods.DeleteObject(handle);
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

            using (var reader = new StringReader(Properties.Resources.status_exh_ko))
            {
                var csv = new CsvReader(reader);
                FStatus status;

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
