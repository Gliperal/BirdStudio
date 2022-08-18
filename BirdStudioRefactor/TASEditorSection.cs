using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;

namespace BirdStudioRefactor
{
    // TODO Maybe rename this to something like InputsBlock or InputsSection
    class TASEditorSection : IBranchSection
    {
        private TASEditor parent;
        private TextEditor component;
        private LineHighlighter bgRenderer;

        private string text;
        private TASInputs inputsData;

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

        public TASInputs getInputsData()
        {
            if (inputsData != null)
                return inputsData;
            inputsData = new TASInputs(text);
            return inputsData;
        }

        public void performEdit(EditHistoryItem e)
        {
            if (!(e is ModifyTextEdit))
                throw new EditTypeNotSupportedException();
            ModifyTextEdit edit = (ModifyTextEdit)e;
            text = component.Text;
            // TODO Better way to perform insertions/deletions on the avalon editor?
            // TextLocation deleteStart = component.Document.GetLocation(pos);
            // TextLocation deleteEnd = component.Document.GetLocation(pos + deleteLength);
            text = text.Substring(0, edit.pos) + edit.textInserted + text.Substring(edit.pos + edit.textRemoved.Length);
            component.Text = text;
            component.CaretOffset = edit.cursorPosFinal;
            inputsData = null;
        }

        public void revertEdit(EditHistoryItem e)
        {
            if (!(e is ModifyTextEdit))
                throw new EditTypeNotSupportedException();
            ModifyTextEdit edit = (ModifyTextEdit)e;
            text = component.Text;
            text = text.Substring(0, edit.pos) + edit.textRemoved + text.Substring(edit.pos + edit.textInserted.Length);
            component.Text = text;
            component.CaretOffset = edit.cursorPosInitial;
            inputsData = null;
        }

        // TODO ugly, ugly function >.<
        private void _userEdit(int pos, int deleteLength, string insert)
        {
            int startOfLine = 0;
            if (pos > 0)
                startOfLine = text.LastIndexOf('\n', pos - 1) + 1;
            int endOfFirstLine = text.IndexOf('\n', pos);
            if (endOfFirstLine == -1)
                endOfFirstLine = text.Length;
            int endOfLastLine = text.IndexOf('\n', pos + deleteLength);
            if (endOfLastLine == -1)
                endOfLastLine = text.Length;
            bool firstLineIsInputLine = TASInputLine.isInputLine(text.Substring(startOfLine, endOfFirstLine - startOfLine));
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
            TASInputLine inputLine = TASInputLine.from(line);
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
                string reformattedLine = new TASInputLine(inputLine.frames, string.Join("", buttons)).toText();

                // Calculate new caret position, based on where caret appeared relative to the frame number
                int caret = pos + insert.Length - startOfLine;
                int newCaret = StringUtil.firstIndexThatIsNot(reformattedLine, " 0");
                int i = StringUtil.firstIndexThatIsNot(line, " 0");
                for (; i < caret && Char.IsDigit(line[i]); i++)
                    newCaret++;
                EditHistoryItem edit = new ModifyTextEdit
                {
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
                EditHistoryItem edit = new ModifyTextEdit
                {
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
                parent.editPerformed(this, new ModifyTextEdit
                {
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
            {
                int splitPoint = 0;
                if (component.CaretOffset != 0)
                    splitPoint = text.LastIndexOf('\n', component.CaretOffset - 1) + 1;
                return new string[]
                {
                    text.Substring(0, splitPoint),
                    text.Substring(splitPoint),
                    ""
                };
            }
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

        public bool updateInputs(List<TASInputLine> newInputs, bool force, Branch target, int nodeIndex)
        {
            List<TASInputLine> inputLines = getInputsData().getInputLines();
            for (int i = 0; i < inputLines.Count && newInputs.Count > 0; i++)
            {
                if (inputLines[i] == null)
                    continue;
                if (inputLines[i].Equals(newInputs[0]))
                    newInputs.RemoveAt(0);
                else
                {
                    int split = StringUtil.nthIndexOf(text, '\n', i);
                    string preText = text.Substring(0, split);
                    string oldBranchText = text.Substring(split + 1);
                    string newBranchText = "";
                    foreach (TASInputLine inputLine in newInputs)
                        newBranchText += inputLine.toText() + '\n';
                    List<Branch> branches = new List<Branch>();
                    branches.Add(Branch.fromText("recorded inputs", newBranchText, parent));
                    branches.Add(Branch.fromText("main branch", oldBranchText, parent));
                    parent.requestEdit(target, new NewBranchGroupEdit
                    {
                        nodeIndex = nodeIndex,
                        initialText = text,
                        preText = preText,
                        branchGroupCopy = new BranchGroup(branches),
                        postText = "",
                        parent = parent,
                    });
                    return true;
                }
            }
            if (newInputs.Count == 0)
                return true;
            if (force)
            {
                string addedText = "";
                foreach (TASInputLine inputLine in newInputs)
                    addedText += inputLine.toText() + '\n';
                parent.requestEdit(this, new ModifyTextEdit
                {
                    pos = text.Length,
                    textRemoved = "",
                    textInserted = "\n" + addedText,
                    cursorPosInitial = text.Length,
                    cursorPosFinal = text.Length
                });
                return true;
            }
            return false;
        }
    }
}
