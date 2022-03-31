using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;

namespace BirdStudioRefactor
{
    class TASEditorSection
    {
        private TASEditor parent;
        private TextEditor component;

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
            component.TextArea.PreviewKeyDown += Editor_KeyDown;
            component.TextArea.TextEntering += Editor_TextEntering;
            component.TextChanged += Editor_TextChanged;
            // TODO still need to block copy/paste and drag&drop text (monitor textchanged and revert changes?) and undo/redo (UndoStack.ClearAll())
            component.AppendText(initialText);
        }

        public void performEdit(EditHistoryItem edit)
        {
            string text = component.Text;
            // TODO Better way to perform insertions/deletions on the avalon editor?
            // TextLocation deleteStart = component.Document.GetLocation(pos);
            // TextLocation deleteEnd = component.Document.GetLocation(pos + deleteLength);
            text = text.Substring(0, edit.pos) + edit.textInserted + text.Substring(edit.pos + edit.textRemoved.Length);
            component.Text = text;
            component.CaretOffset = edit.cursorPosFinal;
        }

        public void revertEdit(EditHistoryItem edit)
        {
            string text = component.Text;
            text = text.Substring(0, edit.pos) + edit.textRemoved + text.Substring(edit.pos + edit.textInserted.Length);
            component.Text = text;
            component.CaretOffset = edit.cursorPosInitial;
        }

        // TODO when user clicks within this section:
        // parent.takeFocus(this); <--- should unfocus other sections, and unselect any highlighted blocks of text within them

        private void userEdit(int pos, int deleteLength, string insert)
        {
            // TODO Reformat to fit tas style and obtain new pos/deleteLength/insert
            EditHistoryItem edit = new EditHistoryItem {
                pos = pos,
                textRemoved = component.Document.Text.Substring(pos, deleteLength),
                textInserted = insert,
                cursorPosInitial = component.CaretOffset,
                cursorPosFinal = pos + insert.Length
            };
            performEdit(edit);
            parent.editPerformed(this, edit);
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
            // TODO Catch copy/paste and undo/redo changes and revert them
        }

        public TextEditor getComponent()
        {
            return component;
        }
    }
}
