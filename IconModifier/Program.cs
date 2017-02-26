using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using CsvHelper;
using System;

namespace IconModifier
{
    class Program
    {
        static void Main(string[] args)
        {
            var files = Directory.GetFiles(@".\icons", "*.png");

            int w = (int)Math.Ceiling(Math.Sqrt(files.Length)) + 1;
            int h = (int)Math.Ceiling((double)files.Length / w);

            Dictionary<int, Point> dic = new Dictionary<int,Point>();

            int index = 0;
            int xx = 0;
            int yy = 0;
            int x, y;
            Color c;

            HashSet<int> a = new HashSet<int>();
            HashSet<int> r = new HashSet<int>();
            HashSet<int> g = new HashSet<int>();
            HashSet<int> b = new HashSet<int>();

            using (var imgOrig = new Bitmap(w * 24, h * 32, PixelFormat.Format32bppArgb))
            {
                using (var gr = Graphics.FromImage(imgOrig))
                    gr.Clear(Color.Transparent);

                foreach (var file in files)
                {
                    xx = index % w;
                    yy = index / w;

                    Console.WriteLine(Path.GetFileName(file));

                    using (var img = Bitmap.FromFile(file) as Bitmap)
                    {
                        // BGR > RGB
                        for (x = 0; x < img.Width; ++x)
                        {
                            for (y = 0; y < img.Height; ++y)
                            {
                                c = img.GetPixel(x, y);
                                a.Add(c.A);
                                r.Add(c.R);
                                g.Add(c.G);
                                b.Add(c.B);
                                imgOrig.SetPixel(xx * 24 + x, yy * 32 + y, Color.FromArgb(c.A, c.B, c.G, c.R));
                            }
                        }
                    }

                    dic.Add(int.Parse(Path.GetFileName(file).Substring(0, 6)), new Point(xx * 24, yy * 32));

                    index++;
                }
                
                var codec = ImageCodecInfo.GetImageDecoders().First(e => e.FormatID == ImageFormat.Png.Guid);
                var param = new EncoderParameters(2);
                param.Param[0] = new EncoderParameter(Encoder.ColorDepth, 8L);
                param.Param[1] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionLZW);
                imgOrig.Save("icons.png", codec, param);
            }

            using (var writer = new StreamWriter("icons-p.txt", false, System.Text.Encoding.UTF8))
            {
                foreach (var d in dic)
                {
                    writer.WriteLine("{0},{1},{2}", d.Key, d.Value.X, d.Value.Y);
                }
            }
        }
    }
}
