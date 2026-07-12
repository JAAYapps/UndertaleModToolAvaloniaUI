using Avalonia.Media.Imaging;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModToolAvalonia.Models.EditorModels;

namespace UndertaleModToolAvalonia.Utilities
{
    public static class BitmapExtensions
    {
        public static Bitmap RotateFlip(this Bitmap source, RotateFlipType rotateFlipType)
        {
            using var ms = new MemoryStream();
            source.Save(ms);
            ms.Position = 0;
            using var original = SKBitmap.Decode(ms);

            int newWidth = original.Width;
            int newHeight = original.Height;
            float scaleX = 1;
            float scaleY = 1;
            float angle = 0;

            switch (rotateFlipType)
            {
                case RotateFlipType.Rotate90FlipNone:
                    angle = 90;
                    (newWidth, newHeight) = (newHeight, newWidth); // Swap dimensions
                    break;
                case RotateFlipType.Rotate180FlipNone:
                    angle = 180;
                    break;
                case RotateFlipType.Rotate270FlipNone:
                    angle = 270;
                    (newWidth, newHeight) = (newHeight, newWidth); // Swap dimensions
                    break;
                case RotateFlipType.RotateNoneFlipX:
                    scaleX = -1;
                    break;
                case RotateFlipType.Rotate90FlipX:
                    angle = 90;
                    scaleX = -1;
                    (newWidth, newHeight) = (newHeight, newWidth);
                    break;
                case RotateFlipType.Rotate180FlipX:
                    angle = 180;
                    scaleX = -1;
                    break;
                case RotateFlipType.Rotate270FlipX:
                    angle = 270;
                    scaleX = -1;
                    (newWidth, newHeight) = (newHeight, newWidth);
                    break;
            }

            if (rotateFlipType == RotateFlipType.RotateNoneFlipY ||
                rotateFlipType == RotateFlipType.Rotate90FlipY ||
                rotateFlipType == RotateFlipType.Rotate180FlipY ||
                rotateFlipType == RotateFlipType.Rotate270FlipY)
            {
                // A Y-flip is an 180-degree rotation plus an X-flip
                angle += 180;
                scaleX = -1;
            }

            // 3. Create a new SKBitmap and canvas to draw the transformed image
            var info = new SKImageInfo(newWidth, newHeight, original.ColorType, original.AlphaType);
            using var newBitmap = new SKBitmap(info);
            using var canvas = new SKCanvas(newBitmap);

            // 4. Apply the transformations
            // Move the pivot point to the center of the new canvas
            canvas.Translate(newWidth / 2f, newHeight / 2f);
            // Apply flip
            canvas.Scale(scaleX, scaleY);
            // Apply rotation
            canvas.RotateDegrees(angle);
            // Move the pivot point back, considering the original dimensions
            canvas.Translate(-original.Width / 2f, -original.Height / 2f);

            // 5. Draw the original bitmap onto the transformed canvas
            canvas.DrawBitmap(original, 0, 0);
            canvas.Flush();

            // 6. Convert the new SKBitmap back to an Avalonia Bitmap
            using var image = SKImage.FromBitmap(newBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = data.AsStream();

            return new Bitmap(stream);
        }
    }
}
