using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Util;

namespace UndertaleModToolAvalonia.Services.TextureCacheService
{
    public interface ITextureCacheService
    {
        public Bitmap GetBitmapForImage(GMImage image);
    }
}
