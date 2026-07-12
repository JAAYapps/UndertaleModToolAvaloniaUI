using Avalonia.Media.Imaging;

namespace UndertaleModToolAvalonia.Models.EditorModels
{
    public class TileRectangle
    {
        public Bitmap ImageSrc { get; set; }
        public uint X { get; set; }
        public uint Y { get; set; }
        public uint Width { get; set; }
        public uint Height { get; set; }
        public double ScaleX { get; set; }
        public double ScaleY { get; set; }
        public double Angle { get; set; }

        public TileRectangle(Bitmap imageSrc, uint x, uint y, uint width, uint height, double scaleX, double scaleY, double angle)
        {
            ImageSrc = imageSrc;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            ScaleX = scaleX;
            ScaleY = scaleY;
            Angle = angle;
        }
    }
}
