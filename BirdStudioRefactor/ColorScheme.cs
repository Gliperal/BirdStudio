using System.Windows.Media;
using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Highlighting;

namespace BirdStudioRefactor
{
    class ColorScheme
    {
        public static HighlightingBrush comment;
        public static HighlightingBrush frame;
        public static HighlightingBrush input;
        public static Brush activeLineBrush = Brushes.Gainsboro;
        public static Brush playbackLineBrush = new SolidColorBrush(Color.FromRgb(0xFD, 0xD8, 0x98));
        public static Brush playbackFrameBrush = Brushes.Black;
        public static Dictionary<string, SolidColorBrush> resources;

        public static void LightMode()
        {
            /*
            comment = new SimpleHighlightingBrush(Color.FromRgb(0, 0x80, 0));
            frame = new SimpleHighlightingBrush(Color.FromRgb(0xFF, 0, 0));
            input = new SimpleHighlightingBrush(Color.FromRgb(0, 0, 0xFF));
            */
            activeLineBrush = Brushes.Gainsboro;
            playbackLineBrush = new SolidColorBrush(Color.FromRgb(0xFD, 0xD8, 0x98));
            playbackFrameBrush = Brushes.Black;

            resources = new Dictionary<string, SolidColorBrush>();
            resources["Editor.Background"] = Brushes.White;
            resources["Editor.Foreground"] = Brushes.Gray;
            resources["Menu.Static.Background"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xF0, 0xF0, 0xF0));
            resources["Menu.Static.Border"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x99, 0x99, 0x99));
            resources["Menu.Static.Foreground"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x21, 0x21, 0x21));
            resources["Menu.Static.Separator"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xD7, 0xD7, 0xD7));
            resources["Menu.Disabled.Foreground"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x70, 0x70, 0x70));
            resources["MenuItem.Selected.Background"] = new SolidColorBrush(Color.FromArgb(0x3D, 0x26, 0xA0, 0xDA));
            resources["MenuItem.Selected.Border"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x26, 0xA0, 0xDA));
            resources["MenuItem.Highlight.Background"] = resources["MenuItem.Selected.Background"];
            resources["MenuItem.Highlight.Border"] = resources["MenuItem.Selected.Border"];
            resources["MenuItem.Highlight.Disabled.Background"] = new SolidColorBrush(Color.FromArgb(0x0A, 0x00, 0x00, 0x00));
            resources["MenuItem.Highlight.Disabled.Border"] = new SolidColorBrush(Color.FromArgb(0x21, 0x00, 0x00, 0x00));
            resources["TextBlock.Background"] = Brushes.White;
            resources["TextBlock.Foreground"] = Brushes.Black;
            resources["ScrollViewer.Background"] = resources["Editor.Background"];
            resources["ScrollBar.Static.Background"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xF0, 0xF0, 0xF0));
            resources["ScrollBar.Static.Border"] = resources["ScrollBar.Static.Background"];
            resources["ScrollBar.Static.Glyph"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x60, 0x60, 0x60));
            resources["ScrollBar.Static.Thumb"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xCD, 0xCD, 0xCD));
            resources["ScrollBar.MouseOver.Background"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xDA, 0xDA, 0xDA));
            resources["ScrollBar.MouseOver.Border"] = resources["ScrollBar.MouseOver.Background"];
            resources["ScrollBar.MouseOver.Glyph"] = Brushes.Black;
            resources["ScrollBar.MouseOver.Thumb"] = Brushes.DarkGray;
            resources["ScrollBar.Pressed.Background"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x60, 0x60, 0x60));
            resources["ScrollBar.Pressed.Border"] = resources["ScrollBar.Pressed.Background"];
            resources["ScrollBar.Pressed.Thumb"] = resources["ScrollBar.Pressed.Background"];
            resources["ScrollBar.Pressed.Glyph"] = Brushes.White;
            resources["ScrollBar.Disabled.Background"] = resources["ScrollBar.Static.Background"];
            resources["ScrollBar.Disabled.Border"] = resources["ScrollBar.Static.Background"];
            resources["ScrollBar.Disabled.Glyph"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xBF, 0xBF, 0xBF));
        }

        public static void DarkMode()
        {
            /*
            comment = new SimpleHighlightingBrush(Color.FromRgb(0x18, 0xA0, 0x30));
            frame = new SimpleHighlightingBrush(Color.FromRgb(0xE0, 0x60, 0x40));
            input = new SimpleHighlightingBrush(Color.FromRgb(0x0A, 0xA0, 0xE0));
            */
            activeLineBrush = new SolidColorBrush(Color.FromRgb(0x38, 0x38, 0x38));
            playbackLineBrush = new SolidColorBrush(Color.FromRgb(0x55, 0x48, 0));
            playbackFrameBrush = Brushes.Orange;

            resources = new Dictionary<string, SolidColorBrush>();
            resources["Editor.Background"] = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));
            resources["Editor.Foreground"] = new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0));
            resources["Menu.Static.Background"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x1A, 0x1A, 0x1A));
            resources["Menu.Static.Border"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x60, 0x60, 0x60));
            resources["Menu.Static.Foreground"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xD0, 0xD0, 0xD0));
            resources["Menu.Static.Separator"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x40, 0x40, 0x40));
            resources["Menu.Disabled.Foreground"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x70, 0x70, 0x70));
            resources["MenuItem.Selected.Background"] = new SolidColorBrush(Color.FromArgb(0x3D, 0x80, 0x80, 0x80));
            resources["MenuItem.Selected.Border"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x80, 0x80, 0x80));
            resources["MenuItem.Highlight.Background"] = resources["MenuItem.Selected.Background"];
            resources["MenuItem.Highlight.Border"] = resources["MenuItem.Selected.Border"];
            resources["MenuItem.Highlight.Disabled.Background"] = new SolidColorBrush(Color.FromArgb(0x0A, 0x00, 0x00, 0x00));
            resources["MenuItem.Highlight.Disabled.Border"] = new SolidColorBrush(Color.FromArgb(0x21, 0x00, 0x00, 0x00));
            // are these ever used?
            resources["MenuItem.Highlight.Disabled.Background"] = new SolidColorBrush(Colors.Magenta);
            resources["MenuItem.Highlight.Disabled.Border"] = new SolidColorBrush(Colors.Magenta);

            resources["TextBlock.Background"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x1A, 0x1A, 0x1A));
            resources["TextBlock.Foreground"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xD0, 0xD0, 0xD0));
            resources["ScrollViewer.Background"] = resources["Editor.Background"];
            resources["ScrollBar.Static.Background"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x1A, 0x1A, 0x1A));
            resources["ScrollBar.Static.Border"] = resources["ScrollBar.Static.Background"];
            resources["ScrollBar.Static.Glyph"] = Brushes.Gray;
            resources["ScrollBar.Static.Thumb"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x44, 0x44, 0x44));
            resources["ScrollBar.MouseOver.Background"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x88, 0x88, 0x88));
            resources["ScrollBar.MouseOver.Border"] = resources["ScrollBar.MouseOver.Background"];
            resources["ScrollBar.MouseOver.Glyph"] = Brushes.Black;
            resources["ScrollBar.MouseOver.Thumb"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x60, 0x60, 0x60));
            resources["ScrollBar.Pressed.Background"] = Brushes.Gray;
            resources["ScrollBar.Pressed.Border"] = resources["ScrollBar.Pressed.Background"];
            resources["ScrollBar.Pressed.Thumb"] = resources["ScrollBar.Pressed.Background"];
            resources["ScrollBar.Pressed.Glyph"] = Brushes.White;
            resources["ScrollBar.Disabled.Background"] = resources["ScrollBar.Static.Background"];
            resources["ScrollBar.Disabled.Border"] = resources["ScrollBar.Static.Background"];
            resources["ScrollBar.Disabled.Glyph"] = Brushes.Gray;
        }
    }
}
