using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using CsvHelper;

namespace IconModifier
{
    class Program
    {
        static void Main(string[] args)
        {
            //var files = Directory.GetFiles(@".\icons", "*.tex");

            var files = new SortedSet<int>();
            using (var reader = new StreamReader("status.exh_ko.csv"))
            {
                using (var csv = new CsvReader(reader))
                {
                    csv.Read();
                    while (csv.Read())
                    {
                        if (csv.TryGetField((int)('C' - 'A'), out string desc) && !string.IsNullOrEmpty(desc) &&
                            csv.TryGetField((int)('D' - 'A'), out int icon) &&
                            csv.TryGetField((int)('E' - 'A'), out int buffStack))
                        {
                            files.Add(icon);

                            for (int i = 0; i < buffStack; ++i)
                                files.Add(icon + i);
                        }
                    }
                }
            }

            int w = (int)Math.Ceiling(Math.Sqrt(files.Count)) + 1;
            int h = (int)Math.Ceiling((double)files.Count / w);

            Dictionary<int, Point> dic = new Dictionary<int,Point>();

            int index = 0;
            int xx = 0;
            int yy = 0;
            int x, y;
            Color c;

            int p;
            int a, r, g, b;

            uint type;

            string path;

            using (var imgOrig = new Bitmap(w * (2 + 24 + 2), h * (2 + 32 + 2), PixelFormat.Format32bppArgb))
            {
                foreach (var file in files)
                {
                    xx = index % w;
                    yy = index / w;

                    path = Path.Combine("icons", string.Format("{0:000000}.tex", file));

                    Console.WriteLine(Path.GetFileName(path));

                    try
                    {
                        using (var tex = File.OpenRead(path))
                        {
                            var reader = new BinaryReader(tex);

                            tex.Seek(4, SeekOrigin.Begin);

                            type = reader.ReadUInt32();

                            tex.Seek(0x50, SeekOrigin.Begin);
                            
                            if (type == 0x00001440)
                            {
                                // BGRA 4444
                                for (y = 0; y < 32; ++y)
                                {
                                    for (x = 0; x < 24; ++x)
                                    {
                                        p = reader.ReadUInt16() & 0xffff;
                                        b = ((p & 0x000F) >>  0) * 17;
                                        g = ((p & 0x00F0) >>  4) * 17;
                                        r = ((p & 0x0F00) >>  8) * 17;
                                        a = ((p & 0xF000) >> 12) * 17;

                                        c = Color.FromArgb(a, r, g, b);

                                        imgOrig.SetPixel(xx * (2 + 24 + 2) + x + 2, yy * (2 + 32 + 2) + y + 2, c);
                                    }
                                }
                            }
                                
                            else if (type == 0x00003420)
                            {   
                                // DX1
                                int blockCountX = (24 + 3) / 4;
                                int blockCountY = (32 + 3) / 4;

                                for (y = 0; y < blockCountY; y++)
                                {
                                    for (x = 0; x < blockCountX; x++)
                                    {
                                        DecompressDxt1Block(reader, x, y, blockCountX, 24, 32, imgOrig, xx * (2 + 24 + 2) + 2, yy * (2 + 32 + 2) + 2);
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                    }

                    dic.Add(file, new Point(xx * (2 + 24 + 2) + 2, yy * (2 + 32 + 2) + 2));

                    index++;
                }

                var codec = ImageCodecInfo.GetImageDecoders().First(e => e.FormatID == ImageFormat.Png.Guid);
                var param = new EncoderParameters(2);
                param.Param[0] = new EncoderParameter(Encoder.ColorDepth, 8L);
                param.Param[1] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionLZW);
                imgOrig.Save("icons.png", codec, param);
            }

            using (var writer = new StreamWriter("icons-pos.csv", false, System.Text.Encoding.UTF8))
            {
                foreach (var d in dic)
                {
                    writer.WriteLine("{0},{1},{2}", d.Key, d.Value.X, d.Value.Y);
                }
            }
        }

        /* FNA - XNA4 Reimplementation for Desktop Platforms
         * Copyright 2009-2017 Ethan Lee and the MonoGame Team
         *
         * Released under the Microsoft Public License.
         * See LICENSE for details.
         */
        private static void DecompressDxt1Block(BinaryReader imageReader, int x, int y, int blockCountX, int width, int height, Bitmap img, int xx, int yy)
        {
            ushort c0 = imageReader.ReadUInt16();
            ushort c1 = imageReader.ReadUInt16();

            byte r0, g0, b0;
            byte r1, g1, b1;
            ConvertRgb565ToRgb888(c0, out r0, out g0, out b0);
            ConvertRgb565ToRgb888(c1, out r1, out g1, out b1);

            uint lookupTable = imageReader.ReadUInt32();

            for (int blockY = 0; blockY < 4; blockY++)
            {
                for (int blockX = 0; blockX < 4; blockX++)
                {
                    byte r = 0, g = 0, b = 0, a = 255;
                    uint index = (lookupTable >> 2 * (4 * blockY + blockX)) & 0x03;

                    if (c0 > c1)
                    {
                        switch (index)
                        {
                            case 0:
                                r = r0;
                                g = g0;
                                b = b0;
                                break;
                            case 1:
                                r = r1;
                                g = g1;
                                b = b1;
                                break;
                            case 2:
                                r = (byte)((2 * r0 + r1) / 3);
                                g = (byte)((2 * g0 + g1) / 3);
                                b = (byte)((2 * b0 + b1) / 3);
                                break;
                            case 3:
                                r = (byte)((r0 + 2 * r1) / 3);
                                g = (byte)((g0 + 2 * g1) / 3);
                                b = (byte)((b0 + 2 * b1) / 3);
                                break;
                        }
                    }
                    else
                    {
                        switch (index)
                        {
                            case 0:
                                r = r0;
                                g = g0;
                                b = b0;
                                break;
                            case 1:
                                r = r1;
                                g = g1;
                                b = b1;
                                break;
                            case 2:
                                r = (byte)((r0 + r1) / 2);
                                g = (byte)((g0 + g1) / 2);
                                b = (byte)((b0 + b1) / 2);
                                break;
                            case 3:
                                r = 0;
                                g = 0;
                                b = 0;
                                a = 0;
                                break;
                        }
                    }

                    int px = (x << 2) + blockX;
                    int py = (y << 2) + blockY;
                    if ((px < width) && (py < height))
                        img.SetPixel(xx + px, yy + py, Color.FromArgb(a, r, g, b));
                }
            }
        }

        private static void ConvertRgb565ToRgb888(ushort color, out byte r, out byte g, out byte b)
        {
            int temp;

            temp = (color >> 11) * 255 + 16;
            r = (byte)((temp / 32 + temp) / 32);
            temp = ((color & 0x07E0) >> 5) * 255 + 32;
            g = (byte)((temp / 64 + temp) / 64);
            temp = (color & 0x001F) * 255 + 16;
            b = (byte)((temp / 32 + temp) / 32);
        }
    }
}
