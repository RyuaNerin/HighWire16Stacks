using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
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
        public static readonly string ResourceUrl  = "https://raw.githubusercontent.com/RyuaNerin/HighWire16Stacks/master/Resources/Resources.tar";
        public static readonly string ResourceHash = "https://raw.githubusercontent.com/RyuaNerin/HighWire16Stacks/master/Resources/Resources.tar.md5";

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
        public static event Action<long, long> DownloadProgressChanged;
        public static ResourceResult ReadResource()
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
                using (var wc = new WebClientEx())
                {
                    // 파일 사이즈 비교
                    wc.Method = "HEAD";
                    wc.DownloadData(ResourceUrl);
                    if (fileInfo.Length != wc.ContentLength)
                        return ResourceResult.NeedToDownload;

                    if (fileInfo.Exists)
                    {
                        // 파일 해싱 비교
                        string hashCurrent, hashRemote;

                        using (var md5 = MD5.Create())
                        using (var file = File.OpenRead(ResourcePath))
                            hashCurrent = BitConverter.ToString(md5.ComputeHash(file)).Replace("-", "").ToLower();

                        hashRemote = wc.DownloadString(ResourceHash).ToLower();

                        if (hashCurrent == hashRemote)
                            return ResourceResult.Success;
                    }
                }
            }
            catch (WebException ex)
            {
                Sentry.Error(ex);
                return ResourceResult.NetworkError;
            }
            catch (Exception ex)
            {
                Sentry.Error(ex);
                return ResourceResult.UnknownError;
            }

            return ResourceResult.NeedToDownload;
        }
        private static ResourceResult DownloadResource()
        {
            try
            {
                using (var wc = new WebClientEx())
                {
                    wc.DownloadProgressChanged += (ls, le) => DownloadProgressChanged?.Invoke(le.BytesReceived, le.TotalBytesToReceive);

                    wc.DownloadFileAsync(new Uri(ResourceUrl), ResourcePath);
                    while (wc.IsBusy)
                        Thread.Sleep(100);
                }

                return ResourceResult.Success;
            }
            catch (WebException ex)
            {
                Sentry.Error(ex);
                return ResourceResult.NetworkError;
            }
            catch (Exception ex)
            {
                Sentry.Error(ex);
                return ResourceResult.UnknownError;
            }
        }
        private static ResourceResult ReadResourceFromDat()
        {
            try
            {
                using (var memory = new MemoryStream())
                using (var file = new FileStream(ResourcePath, FileMode.Open, FileAccess.ReadWrite))
                using (var tar = new TarInputStream(file))
                {
                    TarEntry entry;
                    while ((entry = tar.GetNextEntry()) != null)
                    {
                        if (entry.IsDirectory) continue;

                        if (entry.Name.EndsWith("icons.png") ||
                            entry.Name.EndsWith("icons@2x.png"))
                        {
                            memory.SetLength(0);
                            tar.CopyEntryContents(memory);
                            memory.Position = 0;

                            if (!ReadIcon(memory, entry.Name.EndsWith("icons@2x.png")))
                                return ResourceResult.DataError;
                        }
                        else if (entry.Name.EndsWith("offset.json"))
                        {
                            memory.SetLength(0);
                            tar.CopyEntryContents(memory);
                            memory.Position = 0;

                            if (!Worker.SetOffset(memory))
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
            catch (Exception ex)
            {
                Sentry.Error(ex);
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
                        IconBitmap2x = CreateBitmapSource(bitmap);
                    else
                        IconBitmap   = CreateBitmapSource(bitmap);
                }

                return true;
            }
            catch (Exception ex)
            {
                Sentry.Error(ex);
                return false;
            }
        }
        private static bool ReadIconPos(Stream stream)
        {
            try
            {
                using (var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, true))
                {
                    using (var csv = new CsvReader(reader))
                    {
                        csv.Configuration.HasHeaderRecord = true;
                        csv.Configuration.RegisterClassMap(typeof(PosRecord.Map));
                        
                        foreach (var r in csv.GetRecords<PosRecord>())
                        {
                            IconPosition  .Add(r.StatusId, new Int32Rect(r.X,     r.Y,     24,     32));
                            IconPosition2x.Add(r.StatusId, new Int32Rect(r.X * 2, r.Y * 2, 24 * 2, 32 * 2));
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Sentry.Error(ex);
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
                using (var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, true))
                {
                    using (var csv = new CsvReader(reader))
                    {
                        csv.Configuration.HasHeaderRecord = true;
                        csv.Configuration.RegisterClassMap(typeof(FStatus.Map));
                        
                        foreach (var r in csv.GetRecords<FStatus>())
                        {
                            if (string.IsNullOrEmpty(r.Name) || string.IsNullOrEmpty(r.Desc))
                                continue;

                            StatusList.Add(r);
                            StatusListDic.Add(r.Id, r);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Sentry.Error(ex);
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
            public int X        { get; set; }
            public int Y        { get; set; }
        }

        private class WebClientEx : WebClient
        {
            private long m_contentLength;
            public long ContentLength => this.m_contentLength;

            public string Method { get; set; }

            protected override WebRequest GetWebRequest(Uri address)
            {
                var req = base.GetWebRequest(address) as HttpWebRequest;
                req.Timeout = req.ContinueTimeout = req.ReadWriteTimeout = 5 * 1000;
                req.UserAgent = "HighWire16Stacks";

                if (this.Method != null) req.Method = this.Method;

                return req;
            }

            protected override WebResponse GetWebResponse(WebRequest request)
            {
                var res = base.GetWebResponse(request) as HttpWebResponse;
                this.m_contentLength = res.ContentLength;

                return res;
            }

            protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
            {
                var res = base.GetWebResponse(request, result) as HttpWebResponse;
                this.m_contentLength = res.ContentLength;

                return res;
            }
        }
    }
}
