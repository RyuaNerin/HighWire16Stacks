using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CsvHelper;
using CsvHelper.Configuration;
using HighWire16Stacks.Utilities;
using ICSharpCode.SharpZipLib.Tar;

namespace HighWire16Stacks.Core
{
    internal static class FResource
    {
        public static readonly string ResourcePath = Path.ChangeExtension(App.ExeLocation, ".dat");
        public static readonly string ResourceUrl = "https://raw.githubusercontent.com/RyuaNerin/HighWire16Stacks/master/Resources/Resources.tar";

        private static BitmapSource IconBitmap;
        private static BitmapSource IconBitmap2x;
        private static readonly IDictionary<int, Int32Rect> IconPosition = new SortedDictionary<int, Int32Rect>();
        private static readonly IDictionary<int, Int32Rect> IconPosition2x = new SortedDictionary<int, Int32Rect>();
        private static readonly IDictionary<int, ImageSource> IconCollection = new SortedDictionary<int, ImageSource>();
        private static readonly IDictionary<int, ImageSource> IconCollection2x = new SortedDictionary<int, ImageSource>();

        public static readonly IList<FStatus> StatusList = new SortedList<FStatus>();
        public static readonly IDictionary<int, FStatus> StatusListDic = new SortedDictionary<int, FStatus>();

        static FResource()
        {
            IconCollection.Add(0, null);
            IconCollection2x.Add(0, null);
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

        public static ImageSource GetImage(int statusId, bool use2x)
        {
            if (statusId == 0) return null;

            var dic = use2x ? IconCollection2x : IconCollection;
            var img = use2x ? IconBitmap2x : IconBitmap;
            var pos = use2x ? IconPosition2x : IconPosition;

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

        public enum ResourceResult
        {
            Success,
            NeedToDownload,
            NetworkError,
            UnknownError,
            DataError,
        }
        public static event Action<int, int> DownloadProgressChanged;
        public static ResourceResult ReadResource(string path)
        {
            ResourceResult result;

            result = CheckResource();

            if (result == ResourceResult.NeedToDownload)
                result = DownloadResource();

            if (result == ResourceResult.Success)
                result = ReadResourceFromDat();

            return result;
        }
        private static ResourceResult CheckResource()
        {
            var fileInfo = new FileInfo(ResourcePath);
            if (!fileInfo.Exists) return ResourceResult.NeedToDownload;

            try
            {
                var req = WebRequest.Create(ResourcePath) as HttpWebRequest;
                req.Method = "HEAD";
                req.UserAgent = "HighWire16Stacks";
                req.Timeout = req.ContinueTimeout = req.ReadWriteTimeout = 5 * 1000;

                using (var res = req.GetResponse() as HttpWebResponse)
                    if (fileInfo.Length != res.ContentLength)
                        return ResourceResult.Success;
                    else
                        return ResourceResult.NeedToDownload;
            }
            catch (WebException ex)
            {
                return ResourceResult.NetworkError;
            }
            catch
            {
                return ResourceResult.UnknownError;
            }
        }
        private static ResourceResult DownloadResource()
        {
            try
            {
                var req = WebRequest.Create(ResourcePath) as HttpWebRequest;
                req.UserAgent = "HighWire16Stacks";
                req.Timeout = req.ContinueTimeout = req.ReadWriteTimeout = 5 * 1000;

                using (var res = req.GetResponse() as HttpWebResponse)
                using (var stm = res.GetResponseStream())
                using (var file = new FileStream(ResourcePath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    int cur = 0;
                    int max = (int)res.ContentLength;

                    var buff = new byte[4096];
                    int read;
                    while ((read = stm.Read(buff, 0, 4096)) > 0)
                    {
                        file.Write(buff, 0, read);

                        cur += read;
                        DownloadProgressChanged?.Invoke(cur, max);
                    }
                }

                return ResourceResult.Success;
            }
            catch (WebException)
            {
                return ResourceResult.NetworkError;
            }
            catch
            {
                return ResourceResult.UnknownError;
            }
        }
        public static ResourceResult ReadResourceFromDat()
        {
            try
            {
                using (var memory = new MemoryStream(5 * 1024 * 1024))
                using (var file = new FileStream(ResourcePath, FileMode.Open, FileAccess.Read))
                using (var tar = new TarInputStream(file))
                {
                    TarEntry entry;
                    while ((entry = tar.GetNextEntry()) != null)
                    {
                        if (entry.IsDirectory) continue;

                        if (entry.Name.EndsWith("icons.png") || entry.Name.EndsWith("waifu2x.png"))
                        {
                            memory.SetLength(0);
                            tar.CopyEntryContents(memory);
                            memory.Position = 0;
                            if (!ReadIcon(memory, entry.Name.EndsWith("waifu2x.png")))
                                return ResourceResult.DataError;
                        }
                        else if (entry.Name.EndsWith("offset.json"))
                        {
                            memory.SetLength(0);
                            tar.CopyEntryContents(memory);
                            memory.Position = 0;

                            if (!Worker.SetOffset(Encoding.UTF8.GetString(memory.ToArray())))
                                return ResourceResult.DataError;
                        }
                        else if (entry.Name.EndsWith("icons-pos.csv"))
                        {
                            memory.SetLength(0);
                            tar.CopyEntryContents(memory);
                            memory.Position = 0;

                            if (!ReadIconPos(memory))
                                return ResourceResult.DataError;
                        }
                        else if (entry.Name.EndsWith("status.exh_ko.csv"))
                        {
                            memory.SetLength(0);
                            tar.CopyEntryContents(memory);
                            memory.Position = 0;

                            if (!ReadStatus(memory))
                                return ResourceResult.DataError;
                        }
                    }
                }

                return ResourceResult.Success;
            }
            catch
            {
                return ResourceResult.UnknownError;
            }
        }

        private static bool ReadIcon(Stream stream, bool waifu2x)
        {
            try
            {
                using (var bitmap = Image.FromStream(stream) as Bitmap)
                {
                    if (waifu2x)
                        IconBitmap = CreateBitmapSource(bitmap);
                    else
                        IconBitmap2x = CreateBitmapSource(bitmap);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
        private static bool ReadIconPos(Stream stream)
        {
            try
            {
                using (var reader = new StreamReader(stream))
                {
                    using (var csv = new CsvReader(reader))
                    {
                        csv.Configuration.HasHeaderRecord = true;
                        csv.Configuration.RegisterClassMap(typeof(PosRecord.Map));
                        
                        foreach (var r in csv.GetRecords<PosRecord>())
                        {
                            IconPosition.Add(r.StatusId, new Int32Rect(r.X, r.Y, 24, 32));
                            IconPosition2x.Add(r.StatusId, new Int32Rect(r.X * 2, r.Y * 2, 24 * 2, 32 * 2));
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
        private static bool ReadStatus(Stream stream)
        {
            FStatus status = new FStatus();
            StatusList.Add(status);
            StatusListDic.Add(0, status);

            try
            {
                using (var reader = new StreamReader(stream))
                {
                    using (var csv = new CsvReader(reader))
                    {
                        csv.Configuration.HasHeaderRecord = true;
                        csv.Configuration.RegisterClassMap(typeof(PosRecord.Map));
                        
                        foreach (var r in csv.GetRecords<FStatus>())
                        {
                            if (!string.IsNullOrEmpty(r.Name) || !string.IsNullOrEmpty(r.Desc))
                                continue;

                            StatusList   .Add(      r);
                            StatusListDic.Add(r.Id, r);
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public struct PosRecord
        {
            public class Map : CsvClassMap<PosRecord>
            {
                public Map()
                {
                    Map(m => m.StatusId).Index('A' - 'A');
                    Map(m => m.X       ).Index('B' - 'A');
                    Map(m => m.Y       ).Index('C' - 'A');
                }
            }

            public int StatusId { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
        }
    }
}
