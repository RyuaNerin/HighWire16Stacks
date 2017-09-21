using System;
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
        public static readonly string Icon2xPath = Path.Combine(Path.GetDirectoryName(App.ExeLocation), Path.GetFileNameWithoutExtension(App.ExeLocation) + "@2x.png");
        public static readonly string Icon2xUrl  = "https://raw.githubusercontent.com/RyuaNerin/HighWire16Stacks/master/HighWire16Stacks/Resources/waifu2x.png";

        private static readonly BitmapSource IconBitmap;
        private static          BitmapSource IconBitmap2x;
        private static readonly IDictionary<int, Int32Rect>   IconPosition = new SortedDictionary<int, Int32Rect>();
        private static readonly IDictionary<int, Int32Rect>   IconPosition2x = new SortedDictionary<int, Int32Rect>();
        private static readonly IDictionary<int, ImageSource> IconCollection = new SortedDictionary<int, ImageSource>();
        private static readonly IDictionary<int, ImageSource> IconCollection2x = new SortedDictionary<int, ImageSource>();

        public static readonly IList<FStatus> StatusList = new SortedList<FStatus>();
        public static readonly IDictionary<int, FStatus> StatusListDic = new SortedDictionary<int, FStatus>();

        static FResource()
        {
            IconBitmap   = CreateBitmapSource(Properties.Resources.icons);

            if (CheckWaifu2x())
                LoadWaifu2x();

            IconCollection.Add(0, null);
        }
        public static void Load()
        { }
        private static BitmapSource CreateBitmapSource(Bitmap bitmap)
        {
            return (BitmapSource)App.Current.Dispatcher.Invoke(new Func<Bitmap, BitmapSource>(CreateBitmapSourcePriv), bitmap);
        }
        private static BitmapSource CreateBitmapSourcePriv(Bitmap bitmap)
        {
            BitmapData scan0 = null;

            try
            {
                scan0 = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                return BitmapSource.Create(
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
            catch
            {
                return null;
            }
            finally
            {
                if (scan0 != null)
                    bitmap.UnlockBits(scan0);
            }
        }

        public static bool CheckWaifu2x()
        {
            return File.Exists(Icon2xPath);
        }
        public static bool LoadWaifu2x()
        {
            if (IconBitmap2x != null)
                return true;

            using (var bitmap = new Bitmap(Icon2xPath))
            {
                var bs = CreateBitmapSource(bitmap);
                IconBitmap2x = bs;

                IconCollection2x.Clear();

                return bs != null;
            }
        }
        public static bool Waifu2xLoaded => IconBitmap2x != null;

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
                            IconPosition  .Add(id, new Int32Rect(x,     y,     24,     32    ));
                            IconPosition2x.Add(id, new Int32Rect(x * 2, y * 2, 24 * 2, 32 * 2));
                        }
                    }
                }                
            }

            FStatus status = new FStatus(0, null, null, 0, 0, false, false, false, false);
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
                bool    isFists;

                using (csv = new CsvReader(reader))
                {
                    while (csv.Read())
                    {
                        if (csv.TryGetField<int>   ((int)('A' - 'A'), out id)           &&
                            csv.TryGetField<string>((int)('B' - 'A'), out name)         && !string.IsNullOrEmpty(name)    &&
                            csv.TryGetField<string>((int)('C' - 'A'), out desc)         && !string.IsNullOrEmpty(desc)    &&
                            csv.TryGetField<int>   ((int)('D' - 'A'), out icon)         && IconPosition.ContainsKey(icon) &&
                            csv.TryGetField<int>   ((int)('E' - 'A'), out buffStack)    &&
                            csv.TryGetField<int>   ((int)('F' - 'A'), out isBad)        &&
                            csv.TryGetField<bool>  ((int)('O' - 'A'), out isNonExpries) &&
                            csv.TryGetField((int)('P' - 'A'), out isFists))
                        {
                            status = new FStatus(id, name, desc, icon, buffStack, isBad == 2, isNonExpries, isFists, Settings.Instance.Checked.Contains(id));
                            StatusList.Add(status);
                            StatusListDic.Add(id, status);
                        }
                    }
                }
            }
        }

        public static ImageSource GetImage(int statusId, bool use2x)
        {
            if (statusId == 0) return null;

            var dic = use2x ? IconCollection2x : IconCollection;

            if (IconBitmap2x == null) use2x = false;
            var img = use2x ? IconBitmap2x     : IconBitmap;
            var pos = use2x ? IconPosition2x   : IconPosition;

            lock (dic)
            {
                if (dic.ContainsKey(statusId))
                    return dic[statusId];
                else
                {
                    try
                    {
                        var image = new CroppedBitmap(img, pos[statusId]);
                        dic.Add(statusId, image);

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
