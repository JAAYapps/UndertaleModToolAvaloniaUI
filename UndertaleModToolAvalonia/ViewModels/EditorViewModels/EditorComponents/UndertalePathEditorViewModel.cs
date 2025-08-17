using CommunityToolkit.Mvvm.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents
{
    public partial class UndertalePathEditorViewModel(string title, UndertalePath undertalePath) : EditorContentViewModel(title)
    {
        [ObservableProperty]
        private UndertalePath undertalePath = undertalePath;

        [ObservableProperty]
        private ITransform? pathTransform;
        
        [ObservableProperty]
        private double pathWidth;

        [ObservableProperty]
        private double pathHeight;
        
        public void RefreshPathPreview()
        {
            OnPropertyChanged(nameof(PathDataString));
        }

        public void AddPoint()
        {
            UndertalePath.Points.Add(new UndertalePath.PathPoint());
            OnPropertyChanged(nameof(PathDataString));
        }

        public string PathDataString
        {
            get
            {
                if (UndertalePath?.Points == null || UndertalePath.Points.Count == 0)
                {
                    return "";
                }

                var points = UndertalePath.Points;
                var isClosed = UndertalePath.IsClosed;

                var startPoint = $"M {points[0].X.ToString(CultureInfo.InvariantCulture)},{points[0].Y.ToString(CultureInfo.InvariantCulture)}";
                var linePoints = string.Join(" ", points.Skip(1).Select(p => $"L {p.X.ToString(CultureInfo.InvariantCulture)},{p.Y.ToString(CultureInfo.InvariantCulture)}"));
                string closing = isClosed ? " Z" : "";

                UpdatePathProperties($"{startPoint} {linePoints}{closing}");
                return $"{startPoint} {linePoints}{closing}";
            }
        }
        
        private void UpdatePathProperties(string newPath)
        {
            Rect bounds = PathGeometry.Parse(newPath).Bounds;
            PathTransform = new TranslateTransform(-bounds.X, -bounds.Y);
            PathWidth = bounds.Width;
            PathHeight = bounds.Height;
        }
    }
}
