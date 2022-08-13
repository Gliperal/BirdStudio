using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;

namespace BirdStudioRefactor
{
    public class InputLine
    {
        public int frames;
        public string buttons;

        public InputLine(int frames, string buttons)
        {
            this.frames = frames;
            this.buttons = buttons;
        }

        public static InputLine from(string line)
        {
            line = line.Trim();
            if (line == "" || line.StartsWith('#') || line.StartsWith('>'))
                return null;
            int split = line.LastIndexOfAny("0123456789".ToCharArray()) + 1;
            // If no frame number is found, split will be at 0.
            string frames = line.Substring(0, split);
            string buttons = line.Substring(split).ToUpper();
            buttons = string.Join("", buttons.Split(','));
            foreach (char c in buttons)
                if (!Char.IsLetter(c))
                    return null;
            if (split == 0)
                return new InputLine(0, buttons);
            try
            {
                return new InputLine(Int32.Parse(frames), buttons);
            }
            catch
            {
                return null;
            }
        }

        public static bool isInputLine(string line)
        {
            return from(line) != null;
        }
    }

    class InputsData
    {
        public List<InputLine> inputLines;
        public int frameCount;
        public List<int> startingFrames; // 1 larger in size than inputLines

        public InputsData(string text)
        {
            inputLines = new List<InputLine>();
            string[] lines = text.Split('\n');
            foreach (string line in lines)
                inputLines.Add(InputLine.from(line));

            startingFrames = new List<int>();
            int frame = 0;
            for (int i = 0; i < inputLines.Count; i++)
            {
                startingFrames.Add(frame);
                if (inputLines[i] != null)
                    frame += inputLines[i].frames;
            }
            startingFrames.Add(frame);
            frameCount = frame;
        }

        public int startingFrameForLine(int lineNumber)
        {
            if (lineNumber < 0 || lineNumber >= inputLines.Count)
                return -1;
            return startingFrames[lineNumber];
        }

        public int endingFrameForLine(int lineNumber)
        {
            if (lineNumber < 0 || lineNumber >= inputLines.Count)
                return -1;
            return startingFrames[lineNumber + 1];
        }

        public int[] locateFrame(int frame)
        {
            for (int i = 0; i < inputLines.Count; i++)
            {
                int lineStartFrame = startingFrames[i];
                int lineEndFrame = startingFrames[i + 1];
                if (frame <= lineEndFrame)
                    return new int[] { i, frame - lineStartFrame };
            }
            return new int[] { inputLines.Count - 1, frame - startingFrames[inputLines.Count] };
        }
    }

    // TODO Maybe rename this to something like InputsBlock or InputsSection
    class TASEditorSection : IBranchSection
    {
        private TASEditor parent;
        private TextEditor component;
        private LineHighlighter bgRenderer;

        private string text;
        private InputsData inputsData;

        public TASEditorSection(string initialText, TASEditor parent)
        {
            this.parent = parent;
            component = new TextEditor
            {
                //Padding = "10,0,0,0",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 19,
                ShowLineNumbers = true,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            bgRenderer = new LineHighlighter(component);
            component.TextArea.TextView.BackgroundRenderers.Add(bgRenderer);
            component.TextArea.PreviewKeyDown += Editor_KeyDown;
            component.TextArea.TextEntering += Editor_TextEntering;
            component.TextChanged += Editor_TextChanged;
            text = initialText;
            component.Text = initialText;
            // Disable native undo/redo
            component.Document.UndoStack.SizeLimit = 0;
        }

        public IBranchSection clone()
        {
            return new TASEditorSection(text, parent);
        }

        public string getText()
        {
            return text;
        }

        public TextEditor getComponent()
        {
            return component;
        }

        public InputsData getInputsData()
        {
            if (inputsData != null)
                return inputsData;
            inputsData = new InputsData(text);
            return inputsData;
        }

        public void performEdit(EditHistoryItem edit)
        {
            text = component.Text;
            // TODO Better way to perform insertions/deletions on the avalon editor?
            // TextLocation deleteStart = component.Document.GetLocation(pos);
            // TextLocation deleteEnd = component.Document.GetLocation(pos + deleteLength);
            text = text.Substring(0, edit.pos) + edit.textInserted + text.Substring(edit.pos + edit.textRemoved.Length);
            component.Text = text;
            component.CaretOffset = edit.cursorPosFinal;
            inputsData = null;
        }

        public void revertEdit(EditHistoryItem edit)
        {
            text = component.Text;
            text = text.Substring(0, edit.pos) + edit.textRemoved + text.Substring(edit.pos + edit.textInserted.Length);
            component.Text = text;
            component.CaretOffset = edit.cursorPosInitial;
            inputsData = null;
        }

        // TODO when user clicks within this section:
        // parent.takeFocus(this); <--- should unfocus other sections, and unselect any highlighted blocks of text within them

        private void userEdit(int pos, int deleteLength, string insert)
        {
            // TODO Reformat to fit tas style and obtain new pos/deleteLength/insert
            EditHistoryItem edit = new EditHistoryItem {
                type = EditType.ModifyText,
                pos = pos,
                textRemoved = text.Substring(pos, deleteLength),
                textInserted = insert,
                cursorPosInitial = component.CaretOffset,
                cursorPosFinal = pos + insert.Length
            };
            parent.requestEdit(this, edit);
        }

        private void Editor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back || e.Key == Key.Delete)
            {
                int deletePos = component.SelectionStart;
                int deleteLength = component.SelectionLength;
                if (deleteLength == 0)
                {
                    deleteLength = 1;
                    if (e.Key == Key.Back)
                        deletePos--;
                }
                // Ingore backspace at beginning of text or delete at end of text
                if (
                    deletePos >= 0 &&
                    deletePos + deleteLength <= component.Document.TextLength
                )
                    userEdit(deletePos, deleteLength, "");
                e.Handled = true;
            }
        }

        private void Editor_TextEntering(object sender, TextCompositionEventArgs e)
        {
            int pos = component.CaretOffset;
            int deleteLength = 0;
            if (component.SelectionLength > 0)
            {
                pos = component.SelectionStart;
                deleteLength = component.SelectionLength;
            }
            userEdit(pos, deleteLength, e.Text);
            e.Handled = true;
        }

        private void Editor_TextChanged(object sender, System.EventArgs e)
        {
            // Catch copy/paste and drag&drop changes and include them in the edit history
            if (component.Text != text)
            {
                parent.editPerformed(this, new EditHistoryItem
                {
                    type = EditType.ModifyText,
                    pos = 0,
                    textRemoved = text,
                    textInserted = component.Text,
                    cursorPosInitial = 0, // TODO
                    cursorPosFinal = component.CaretOffset
                });
                text = component.Text;
            }
        }

        public string[] splitOutBranch()
        {
            if (component.SelectionLength == 0)
                return new string[]
                {
                    text.Substring(0, component.CaretOffset),
                    text.Substring(component.CaretOffset),
                    ""
                };
            else
                return new string[]
                {
                    text.Substring(0, component.SelectionStart),
                    text.Substring(component.SelectionStart, component.SelectionLength),
                    text.Substring(component.SelectionStart + component.SelectionLength),
                };
        }

        public void showPlaybackFrame(int frame)
        {
            getInputsData();
            int[] frameLocation = inputsData.locateFrame(frame);
            bgRenderer.ShowActiveFrame(frameLocation[0], frameLocation[1]);
            App.Current.Dispatcher.Invoke((Action)delegate // need to update on main thread
            {
                component.TextArea.TextView.Redraw();
            });
        }
    }
}
