using System.Windows.Media;

namespace BirdStudioRefactor
{
    class ColorScheme
    {
        public static Brush activeLineBrush = Brushes.Gainsboro;
        public static Brush playbackLineBrush = new SolidColorBrush(Color.FromRgb(0xFD, 0xD8, 0x98));
        public static Brush playbackFrameBrush = Brushes.Black;
    }
}
