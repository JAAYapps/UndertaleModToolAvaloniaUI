using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModToolAvalonia.Models.EditorModels
{
    /// <summary>
    /// Specifies the type of rotation and flip to apply to an image.
    /// </summary>
    public enum RotateFlipType
    {
        RotateNoneFlipNone = 0,
        Rotate90FlipNone = 1,
        Rotate180FlipNone = 2,
        Rotate270FlipNone = 3,
        RotateNoneFlipX = 4,
        Rotate90FlipX = 5,
        Rotate180FlipX = 6,
        Rotate270FlipX = 7,
        // Aliases for convenience
        RotateNoneFlipY = 6,
        Rotate90FlipY = 7,
        Rotate180FlipY = 4,
        Rotate270FlipY = 5,
        RotateNoneFlipXY = 2,
        Rotate90FlipXY = 3,
        Rotate180FlipXY = 0,
        Rotate270FlipXY = 1
    }
}
