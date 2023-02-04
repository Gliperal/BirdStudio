using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit;

namespace BirdStudioRefactor
{
    // TODO Maybe rename this to something like InputsBlock or InputsSection
    public class TASEditorSection : TextEditor, IBranchSection
    {
        private TASEditor editor;
        private LineHighlighter bgRenderer;

        private string text;
        bool ignoreCaretChanges;
        private TASInputs inputsData;

        public TASEditorSection(string initialText, TASEditor editor)
        {
            this.editor = editor;
            Padding = new Thickness(10, 0, 0, 0);
            FontFamily = new FontFamily("Consolas");
            FontSize = 19;
            ShowLineNumbers = true;
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            SyntaxHighlighting = ColorScheme.instance().syntaxHighlighting;
            bgRenderer = new LineHighlighter(this);
            SetResourceReference(Control.ForegroundProperty, "Editor.Foreground");
            SetResourceReference(Control.BackgroundProperty, "Editor.Background");
            TextArea.TextView.BackgroundRenderers.Add(bgRenderer);
            TextArea.PreviewKeyDown += Editor_KeyDown;
            TextArea.TextEntering += Editor_TextEntering;
            TextChanged += Editor_TextChanged;
            Document.UpdateFinished += Editor_UpdateFinished;
            GotKeyboardFocus += Editor_GainedFocus;
            LostKeyboardFocus += Editor_LostFocus;
            RequestBringIntoView += Editor_RequestBringIntoView;
            TextArea.Caret.PositionChanged += Editor_CaretPositionChanged;
            text = initialText;
            Text = initialText;
            // Disable native undo/redo
            Document.UndoStack.SizeLimit = 0;
        }

        public IBranchSection clone()
        {
            return new TASEditorSection(text, editor);
        }

        public string getText()
        {
            return text;
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
            ignoreCaretChanges = true;
            if (!(e is ModifyTextEdit))
                throw new EditTypeNotSupportedException();
            ModifyTextEdit edit = (ModifyTextEdit)e;
            // text = component.Text;
            // TODO Better way to perform insertions/deletions on the avalon editor?
            // TextLocation deleteStart = component.Document.GetLocation(pos);
            // TextLocation deleteEnd = component.Document.GetLocation(pos + deleteLength);
            text = text.Substring(0, edit.pos) + edit.textInserted + text.Substring(edit.pos + edit.textRemoved.Length);
            Text = text;
            CaretOffset = edit.cursorPosFinal;
            inputsData = null;
            ignoreCaretChanges = false;
        }

        public void revertEdit(EditHistoryItem e)
        {
            ignoreCaretChanges = true;
            if (!(e is ModifyTextEdit))
                throw new EditTypeNotSupportedException();
            ModifyTextEdit edit = (ModifyTextEdit)e;
            // text = component.Text;
            text = text.Substring(0, edit.pos) + edit.textRemoved + text.Substring(edit.pos + edit.textInserted.Length);
            Text = text;
            CaretOffset = edit.cursorPosInitial;
            inputsData = null;
            ignoreCaretChanges = false;
        }

        // TODO ugly, ugly function >.<
        private void _userEdit(int pos, int deleteLength, string insert)
        {
            LinesInfo linesInfo = new LinesInfo(text, pos, deleteLength);
            bool firstLineIsInputLine = TASInputLine.isInputLine(linesInfo.firstLine);
            if (firstLineIsInputLine && deleteLength == 0)
            {
                if (insert == "#")
                    pos = linesInfo.start;
                else if (insert == "\n")
                {
                    if (linesInfo.preText.Trim() == "")
                        pos = linesInfo.start;
                    else
                        pos = linesInfo.endOfFirstLine;
                }
                else
                {
                    // Unless we're typing numbers in the middle of the number, default cursor to position 4
                    int endOfNumbers = Util.firstIndexThatIsNot(text, " \t0123456789", linesInfo.start);
                    if (!Char.IsDigit(insert[0]) || pos > endOfNumbers)
                        pos = endOfNumbers;
                    linesInfo = new LinesInfo(text, pos, deleteLength);
                }
            }
            string line = linesInfo.preText + insert + linesInfo.postText;
            TASInputLine inputLine = TASInputLine.from(line);
            if (inputLine != null && !insert.Contains('\n'))
            {
                string reformattedLine = inputLine.reformat(insert).toText();

                // Calculate new caret position, based on where caret appeared relative to the frame number
                int caret = pos + insert.Length - linesInfo.start;
                int newCaret = Util.firstIndexThatIsNot(reformattedLine, " 0");
                int i = Util.firstIndexThatIsNot(line, " 0");
                for (; i < caret && Char.IsDigit(line[i]); i++)
                    newCaret++;
                EditHistoryItem edit = new ModifyTextEdit
                {
                    pos = linesInfo.start,
                    textRemoved = text.Substring(linesInfo.start, linesInfo.length),
                    textInserted = reformattedLine,
                    cursorPosInitial = CaretOffset,
                    cursorPosFinal = linesInfo.start + newCaret
                };
                editor.requestEdit(this, edit);
            }
            else
            {
                EditHistoryItem edit = new ModifyTextEdit
                {
                    pos = pos,
                    textRemoved = text.Substring(pos, deleteLength),
                    textInserted = insert,
                    cursorPosInitial = CaretOffset,
                    cursorPosFinal = pos + insert.Length
                };
                editor.requestEdit(this, edit);
            }
        }

        private void Editor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back || e.Key == Key.Delete)
            {
                int deletePos = SelectionStart;
                int deleteLength = SelectionLength;
                if (deleteLength == 0)
                {
                    deleteLength = 1;
                    if (e.Key == Key.Back)
                        deletePos--;
                }
                // Ingore backspace at beginning of text or delete at end of text
                if (
                    deletePos >= 0 &&
                    deletePos + deleteLength <= Document.TextLength
                )
                    _userEdit(deletePos, deleteLength, "");
                e.Handled = true;
            }
        }

        private void Editor_TextEntering(object sender, TextCompositionEventArgs e)
        {
            int pos = CaretOffset;
            int deleteLength = 0;
            if (SelectionLength > 0)
            {
                pos = SelectionStart;
                deleteLength = SelectionLength;
            }
            _userEdit(pos, deleteLength, e.Text);
            e.Handled = true;
        }

        private void Editor_TextChanged(object sender, EventArgs e)
        {
            // Catch copy/paste and drag&drop changes and include them in the
            // edit history. Copy/paste loves to convert to windows line
            // endings, so also nip that in the bud.
            if (Text != text)
            {
                inputsData = null;
                string oldText = text;
                int cursorPosFinal = CaretOffset - Util.substringCount(Text, "\r\n", CaretOffset + 1);
                text = Text.Replace("\r\n", "\n");
                editor.editPerformed(this, new ModifyTextEdit
                {
                    pos = 0,
                    textRemoved = oldText,
                    textInserted = text,
                    cursorPosInitial = 0, // TODO
                    cursorPosFinal = cursorPosFinal,
                });
            }
        }

        private void Editor_UpdateFinished(object sender, EventArgs e)
        {
            if (Text != text)
                Text = text;
        }

        private void Editor_GainedFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            bgRenderer.changeFocus(true);
            TextArea.TextView.Redraw();
        }

        private void Editor_LostFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            bgRenderer.changeFocus(false);
            TextArea.TextView.Redraw();
            SelectionLength = 0;
        }

        public void Editor_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            // Stop WPF from snapping to a new section when it gains focus
            e.Handled = true;
        }

        public void Editor_CaretPositionChanged(object sender, EventArgs e)
        {
            if (ignoreCaretChanges)
                return;
            // TODO Causes weird things to happen when clicking on a different branch while the currently selected line is out of focus
            editor.bringActiveLineToFocus();
        }

        public NewBranchInfo splitOutBranch()
        {
            if (SelectionLength == 0)
            {
                LinesInfo info = new LinesInfo(text, SelectionStart, SelectionLength);
                return new NewBranchInfo
                {
                    preText = (info.start > 0) ? text.Substring(0, info.start - 1) : null,
                    splitText = text.Substring(info.start),
                    bottomless = true,
                };
            }
            else
                return new NewBranchInfo
                {
                    preText = text.Substring(0, SelectionStart),
                    splitText = text.Substring(SelectionStart, SelectionLength),
                    postText = text.Substring(SelectionStart + SelectionLength),
                };
        }

        private DispatcherOperation lastRedraw;

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
            if (lastRedraw != null)
                lastRedraw.Abort();
            lastRedraw = App.Current.Dispatcher.BeginInvoke((Action)delegate // need to update on main thread
            {
                TextArea.TextView.Redraw();
            });
        }

        public bool updateInputs(List<TASInputLine> newInputs, bool force, ref NewBranchInfo newBranchInfo)
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
                    int split = Util.nthIndexOf(text, '\n', i);
                    string newBranchText = "";
                    foreach (TASInputLine inputLine in newInputs)
                        newBranchText += inputLine.toText() + '\n';
                    newBranchInfo = new NewBranchInfo
                    {
                        preText = (split > 0) ? text.Substring(0, split) : null,
                        splitText = text.Substring(split + 1),
                        bottomless = true,
                        newBranchText = newBranchText,
                    };
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
                editor.requestEdit(this, new ModifyTextEdit
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

        public void comment()
        {
            LinesInfo linesInfo = new LinesInfo(text, SelectionStart, SelectionLength);
            string[] lines = linesInfo.lines.Split('\n');
            bool uncomment = true;
            foreach (string line in lines)
                if (!line.Trim().StartsWith('#'))
                    uncomment = false;
            if (uncomment)
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    int commentStart = lines[i].IndexOf('#');
                    lines[i] = lines[i].Substring(commentStart + 1);
                }
            }
            else
            {
                for (int i = 0; i < lines.Length; i++)
                    lines[i] = "#" + lines[i];
            }
            ModifyTextEdit edit = new ModifyTextEdit
            {
                pos = linesInfo.start,
                textRemoved = text.Substring(linesInfo.start, linesInfo.end - linesInfo.start),
                textInserted = string.Join('\n', lines),
                cursorPosInitial = CaretOffset,
                cursorPosFinal = linesInfo.start
            };
            editor.requestEdit(this, edit);
        }

        public void insertLine(string timestampComment)
        {
            LinesInfo info = new LinesInfo(text, CaretOffset, 0);
            ModifyTextEdit edit = new ModifyTextEdit
            {
                pos = info.start,
                textRemoved = info.lines,
                textInserted = timestampComment,
                cursorPosInitial = CaretOffset,
                cursorPosFinal = info.start + timestampComment.Length
            };
            if (info.lines.Trim() != "")
            {
                edit.pos = info.end;
                edit.textRemoved = "";
                edit.textInserted = "\n" + timestampComment;
                edit.cursorPosFinal++;
            }
            editor.requestEdit(this, edit);
        }
    }
}
