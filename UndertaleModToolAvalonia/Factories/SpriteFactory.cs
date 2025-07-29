using Avalonia;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;
using UndertaleModLib.Util;
using UndertaleModToolAvalonia.Services.TextureCacheService;

namespace UndertaleModToolAvalonia.Factories
{
    /// <summary>
    /// Creates sprite Bitmaps from texture pages.
    /// </summary>
    public class SpriteFactory
    {
        private readonly TextureCacheService _textureCache;

        public SpriteFactory(TextureCacheService textureCache)
        {
            _textureCache = textureCache;
        }

        /// <summary>
        /// Creates a sprite by clipping a region from a texture page.
        /// </summary>
        /// <returns>An Avalonia Bitmap of the specified sprite.</returns>
        public Bitmap CreateSpriteBitmap(PixelRect rect, in UndertaleTexturePageItem texture, int diffW = 0, int diffH = 0)
        {
            GMImage image = texture.TexturePage.TextureData.Image;
            // 1. Get the full texture sheet from the cache service
            Bitmap sourceBitmap = _textureCache.GetBitmapForImage(image);

            PixelSize targetSize = rect.Size;
            PixelRect sourceClipRect = rect;

            // --- Same clamping logic as before, using Avalonia types ---
            sourceClipRect = sourceClipRect.WithWidth(sourceClipRect.Width - Math.Max(0, diffW));
            sourceClipRect = sourceClipRect.WithHeight(sourceClipRect.Height - Math.Max(0, diffH));

            int offsetX = 0, offsetY = 0;
            if (sourceClipRect.X < texture.SourceX)
            {
                offsetX = texture.SourceX - sourceClipRect.X;
                sourceClipRect = sourceClipRect.WithWidth(sourceClipRect.Width - offsetX)
                                               .WithX(texture.SourceX);
            }
            if (sourceClipRect.Y < texture.SourceY)
            {
                offsetY = texture.SourceY - sourceClipRect.Y;
                sourceClipRect = sourceClipRect.WithHeight(sourceClipRect.Height - offsetY)
                                               .WithY(texture.SourceY);
            }

            // 2. Create the final bitmap to draw on
            var spriteBitmap = new RenderTargetBitmap(targetSize, new Vector(96, 96));

            // Abort if the rectangle is invalid or out of bounds
            if (sourceClipRect.X >= (texture.SourceX + texture.SourceWidth) || sourceClipRect.Y >= (texture.SourceY + texture.SourceHeight) ||
                sourceClipRect.Width <= 0 || sourceClipRect.Height <= 0 ||
                (sourceClipRect.Right > sourceBitmap.PixelSize.Width) || (sourceClipRect.Bottom > sourceBitmap.PixelSize.Height))
            {
                return spriteBitmap; // Return the empty (transparent) bitmap
            }

            // 3. Use a DrawingContext to render the sprite
            using (var ctx = spriteBitmap.CreateDrawingContext())
            {
                // Create a lightweight, cropped view of the source
                var croppedSource = new CroppedBitmap(sourceBitmap, sourceClipRect);

                // Draw the cropped portion onto the target bitmap at the calculated offset
                ctx.DrawImage(croppedSource, new Rect(offsetX, offsetY, sourceClipRect.Width, sourceClipRect.Height));
            }

            return spriteBitmap;
        }
    }
}
