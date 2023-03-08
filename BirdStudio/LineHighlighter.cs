using System;
using System.Windows;
using System.Windows.Media;
using System.Globalization;

using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Document;

namespace BirdStudio
{
    public class LineHighlighter : IBackgroundRenderer
    {
        private TextEditor editor;
        private int line = -1;
        private int frame;
        private bool inFocus;

        public LineHighlighter(TextEditor editor)
        {
            this.editor = editor;
        }

        public KnownLayer Layer
        {
            get
            {
                // draw behind selection
                return KnownLayer.Selection;
            }
        }

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (textView == null)
                throw new ArgumentNullException("textView");
            if (drawingContext == null)
                throw new ArgumentNullException("drawingContext");
            if (editor.Document == null)
                return;

            textView.EnsureVisualLines();

            // TODO if multiple lines selected?
            if (inFocus)
            {
                var currentLine = editor.Document.GetLineByOffset(editor.CaretOffset);
                foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, currentLine))
                {
                    drawingContext.DrawRectangle(
                        ColorScheme.instance().activeLineBrush, null,
                        new Rect(rect.Location, new Size(textView.ActualWidth, rect.Height)));
                }
            }

            if (line > -1 && line < editor.Document.LineCount)
            {
                DocumentLine docLine = editor.Document.GetLineByNumber(line + 1);
                foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, docLine))
                {
                    drawingContext.DrawRectangle(
                        ColorScheme.instance().playbackLineBrush, null,
                        new Rect(rect.Location, new Size(textView.ActualWidth, rect.Height))
                    );
                    // This is probably the wrong way to do this
                    FormattedText text = new FormattedText(
                        frame.ToString(),
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Consolas"),
                        19,
                        ColorScheme.instance().playbackFrameBrush,
                        1 //TODO dafuq is pixels per DIP
                    );
                    Point origin = new Point(textView.ActualWidth - text.Width - 5, rect.Top);
                    drawingContext.DrawText(text, origin);
                }
            }
        }

        public void ShowActiveFrame(int line, int frame)
        {
            this.line = line;
            this.frame = frame;
        }

        public void changeFocus(bool state)
        {
            inFocus = state;
        }
    }
}
