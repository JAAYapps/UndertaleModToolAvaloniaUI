using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            UndertaleData data = UndertaleIO.Read(new FileStream(@"data.win", FileMode.Open, FileAccess.Read));
            int lastpage = data.TexturePageItems.Count - 1;
            //foreach(var code in data.Code)
            //{
            //    Debug.WriteLine(code.Name.Content);
            //    code.Replace(Assembler.Assemble(code.Disassemble(data.Variables, data.CodeLocals.For(code)), data.Functions, data.Variables, data.Strings));
            //}
            using MemoryStream stream = new(data.TexturePageItems[lastpage - 15].TexturePage.TextureData.TextureBlob);
            Bitmap bmp = new Bitmap(stream);
            BitmapData bdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
            Console.WriteLine(bdata.Scan0);

            var builder = BuildAvaloniaApp();

            Task t = new Task(() => builder.StartWithClassicDesktopLifetime(args));
            t.Start();

            using MemoryStream stream2 = new(data.TexturePageItems[lastpage - 15].TexturePage.TextureData.TextureBlob);
            Thread.Sleep(10000);

            Avalonia.Media.Imaging.Bitmap bitmap = new(stream2);
            var aBitmap = BitmapToWritableBitmap(bitmap);
            Console.WriteLine(aBitmap.Lock().Address);

            bmp.Save(@"newimage.png");
            aBitmap.Save(@"newimage2.png");
            // UndertaleIO.Write(new FileStream(@"newdata.win", FileMode.Create), data);
        }

        public static Avalonia.Media.Imaging.Bitmap ByteDataToBlackAndWhiteBitmap(byte[] data, int width, int height)
        {
            using (var memoryStream = new MemoryStream(data))
            {
                memoryStream.Seek(0, SeekOrigin.Begin);
                // return 
                var wb = new Avalonia.Media.Imaging.WriteableBitmap(new Avalonia.PixelSize((int)width, (int)height), new Avalonia.Vector(96, 96), Avalonia.Platform.PixelFormat.Rgb565, Avalonia.Platform.AlphaFormat.Opaque);
                var bdata = wb.Lock();
                var sbm = SkiaSharp.SKBitmap.Decode(data);
                
                return wb;//(, , PixelFormats.BlackWhite, null, data, (int)((width + 7) / 8));
            }
        }

        public static Avalonia.Media.Imaging.WriteableBitmap BitmapToWritableBitmap(Avalonia.Media.Imaging.Bitmap bitmap)
        {
            using (var memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);
                return Avalonia.Media.Imaging.WriteableBitmap.Decode(memoryStream);
            }
        }

        private static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<Application>()
                .UsePlatformDetect()
                .With(new X11PlatformOptions
                {
                    EnableMultiTouch = true,
                    UseDBusMenu = true,
                    EnableIme = true,
                    UseGpu = true,
                    UseEGL = true
                })
                .With(new Win32PlatformOptions { AllowEglInitialization = true })
                .With(new AvaloniaNativePlatformOptions { UseGpu = true })
                .UseSkia()
                .LogToTrace();
    }
}
