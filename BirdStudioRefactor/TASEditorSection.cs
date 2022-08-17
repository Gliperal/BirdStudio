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

        // TODO maybe "toText" would be better?
        public string toTasLine()
        {
            if (buttons.Length == 0)
                return String.Format("{0,4}", frames);
            const string order = InputsData.BUTTONS;
            List<char> orderedButtons = new List<char>();
            foreach (char c in order)
                if (buttons.Contains(c))
                    orderedButtons.Add(c);
            string buttonsStr = string.Join(",", orderedButtons);
            if (buttonsStr == "")
                return String.Format("{0,4}", frames);
            else
                return String.Format("{0,4},{1}", frames, buttonsStr);
        }

        public static bool isInputLine(string line)
        {
            return from(line) != null;
        }
    }

    class InputsData
    {
        public const string BUTTONS = "RLUDJXGCQMN";

        private List<InputLine> inputLines;
        private int frameCount;
        private List<int> startingFrames; // 1 larger in size than inputLines

        public InputsData(string text)
        {
            inputLines = new List<InputLine>();
            string[] lines = text.Split('\n');
            foreach (string line in lines)
                inputLines.Add(InputLine.from(line));
            countFrames();
        }

        public InputsData(List<Press> presses)
        {
            inputLines = new List<InputLine>();
            presses.Sort(Press.compareFrames);
            HashSet<char> state = new HashSet<char>();
            int frame = 0;
            foreach (Press press in presses)
            {
                if (press.frame > frame)
                {
                    inputLines.Add(new InputLine(press.frame - frame, string.Join("", state)));
                    frame = press.frame;
                }
                if (press.on)
                    state.Add(press.button);
                else
                    state.Remove(press.button);
            }
            inputLines.Add(new InputLine(1, string.Join("", state)));
        }

        private void countFrames()
        {
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

        public List<Press> toPresses()
        {
            List<Press> presses = new List<Press>();
            HashSet<char> state = new HashSet<char>();
            int frame = 0;
            foreach (InputLine inputLine in inputLines)
            {
                if (inputLine == null)
                    continue;
                foreach (char button in BUTTONS)
                {
                    bool isOn = inputLine.buttons.Contains(button);
                    bool wasOn = state.Contains(button);
                    if (isOn != wasOn)
                    {
                        presses.Add(new Press
                        {
                            frame = frame,
                            button = button,
                            on = isOn
                        });
                        if (isOn)
                            state.Add(button);
                        else
                            state.Remove(button);
                    }
                }
                frame += inputLine.frames;
            }
            return presses;
        }

        public string toText()
        {
            string text = "";
            foreach (InputLine inputLine in inputLines)
                text += inputLine.toTasLine() + '\n';
            return text;
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

        public int totalFrames()
        {
            return frameCount;
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
            component.SetResourceReference(Control.ForegroundProperty, "Editor.Foreground");
            component.SetResourceReference(Control.BackgroundProperty, "Editor.Background");
            component.TextArea.TextView.BackgroundRenderers.Add(bgRenderer);
            component.TextArea.PreviewKeyDown += Editor_KeyDown;
            component.TextArea.TextEntering += Editor_TextEntering;
            component.TextChanged += Editor_TextChanged;
            component.GotKeyboardFocus += Editor_GainedFocus;
            component.LostKeyboardFocus += Editor_LostFocus;
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

        // TODO ugly, ugly function >.<
        private void _userEdit(int pos, int deleteLength, string insert)
        {
            // TODO re-obtain stage if needed
            int startOfLine = text.LastIndexOf('\n', pos - 1) + 1;
            int endOfFirstLine = text.IndexOf('\n', pos);
            if (endOfFirstLine == -1)
                endOfFirstLine = text.Length;
            int endOfLastLine = text.IndexOf('\n', pos + deleteLength);
            if (endOfLastLine == -1)
                endOfLastLine = text.Length;
            bool firstLineIsInputLine = InputLine.isInputLine(text.Substring(startOfLine, endOfFirstLine - startOfLine));
            if (firstLineIsInputLine && deleteLength == 0)
            {
                if (insert == "#")
                    pos = startOfLine;
                else if (insert == "\n")
                {
                    if (text.Substring(startOfLine, pos - startOfLine).Trim() == "")
                        pos = startOfLine;
                    else
                        pos = endOfFirstLine;
                }
                else
                {
                    // Unless we're typing numbers in the middle of the number, default cursor to position 4
                    int endOfNumbers = StringUtil.firstIndexThatIsNot(text, " \t0123456789", startOfLine);
                    if (!Char.IsDigit(insert[0]) || pos > endOfNumbers)
                        pos = endOfNumbers;
                }
            }
            string line = text.Substring(startOfLine, pos - startOfLine) + insert + text.Substring(pos + deleteLength, endOfLastLine - pos - deleteLength);
            InputLine inputLine = InputLine.from(line);
            if (inputLine != null && !insert.Contains('\n'))
            {
                HashSet<char> buttons = new HashSet<char>();
                foreach (char c in inputLine.buttons)
                {
                    if (buttons.Contains(c))
                        buttons.Remove(c);
                    else
                        buttons.Add(c);
                }
                string reformattedLine = new InputLine(inputLine.frames, string.Join("", buttons)).toTasLine();

                // Calculate new caret position, based on where caret appeared relative to the frame number
                int caret = pos + insert.Length - startOfLine;
                int newCaret = StringUtil.firstIndexThatIsNot(reformattedLine, " 0");
                int i = StringUtil.firstIndexThatIsNot(line, " 0");
                for (; i < caret && Char.IsDigit(line[i]); i++)
                    newCaret++;
                EditHistoryItem edit = new EditHistoryItem
                {
                    type = EditType.ModifyText,
                    pos = startOfLine,
                    textRemoved = text.Substring(startOfLine, endOfLastLine - startOfLine),
                    textInserted = reformattedLine,
                    cursorPosInitial = component.CaretOffset,
                    cursorPosFinal = startOfLine + newCaret
                };
                parent.requestEdit(this, edit);
            }
            else
            {
                EditHistoryItem edit = new EditHistoryItem
                {
                    type = EditType.ModifyText,
                    pos = pos,
                    textRemoved = text.Substring(pos, deleteLength),
                    textInserted = insert,
                    cursorPosInitial = component.CaretOffset,
                    cursorPosFinal = pos + insert.Length
                };
                parent.requestEdit(this, edit);
            }
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
                    _userEdit(deletePos, deleteLength, "");
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
            _userEdit(pos, deleteLength, e.Text);
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

        private void Editor_GainedFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            bgRenderer.changeFocus(true);
            component.TextArea.TextView.Redraw();
        }

        private void Editor_LostFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            bgRenderer.changeFocus(false);
            component.TextArea.TextView.Redraw();
            component.SelectionLength = 0;
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
            if (frame == -1)
                bgRenderer.ShowActiveFrame(-1, -1);
            else
            {
                getInputsData();
                int[] frameLocation = inputsData.locateFrame(frame);
                bgRenderer.ShowActiveFrame(frameLocation[0], frameLocation[1]);
            }
            App.Current.Dispatcher.Invoke((Action)delegate // need to update on main thread
            {
                component.TextArea.TextView.Redraw();
            });
        }
    }
}
