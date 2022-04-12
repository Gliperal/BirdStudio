using System.Windows.Controls;
using System.Collections.Generic;

namespace BirdStudioRefactor
{
    class TASEditor : FileManager
    {
        private const string DEFAULT_FILE_TEXT = ">stage Twin Tree Village\n>rerecords 0\n\n  29";

        private StackPanel panel;
        private List<TASEditorSection> sections; // TODO convert from list into tree (may need to change index to some kind of id too)
        private List<EditHistoryItem> editHistory = new List<EditHistoryItem>();
        private int editHistoryLocation = 0;
        private bool tasEditedSinceLastWatch = true;

        public TASEditor(MainWindow window, StackPanel panel) : base(window)
        {
            this.panel = panel;
            neww();
        }

        public void editPerformed(TASEditorSection section, EditHistoryItem edit)
        {
            for (int i = 0; i < sections.Count; i++)
                if (sections[i] == section)
                {
                    edit.sectionIndex = i;
                    break;
                }
            if (editHistoryLocation < editHistory.Count)
                editHistory.RemoveRange(editHistoryLocation, editHistory.Count - editHistoryLocation);
            editHistory.Add(edit);
            editHistoryLocation++;
        }

        public bool canUndo()
        {
            return editHistoryLocation > 0;
        }

        public void undo()
        {
            if (!canUndo())
                return;
            EditHistoryItem edit = editHistory[editHistoryLocation - 1];
            sections[edit.sectionIndex].revertEdit(edit);
            editHistoryLocation--;
            // TODO change focus to sections[edit.sectionIndex]
        }

        public bool canRedo()
        {
            return editHistoryLocation < editHistory.Count;
        }

        public void redo()
        {
            EditHistoryItem edit = editHistory[editHistoryLocation];
            sections[edit.sectionIndex].performEdit(edit);
            editHistoryLocation++;
            // TODO change focus to sections[edit.sectionIndex]
        }

        private void _clearUndoStack()
        {
            editHistory = new List<EditHistoryItem>();
            editHistoryLocation = 0;
        }

        protected override void _importFromFile(string tas)
        {
            if (tas == null)
                tas = DEFAULT_FILE_TEXT;
            sections = new List<TASEditorSection>();
            panel.Children.Clear();
            for (int i = 0; i < 5; i++)
            {
                TASEditorSection section = new TASEditorSection(tas, this);
                panel.Children.Add(section.getComponent());
                sections.Add(section);
            }
            tasEditedSinceLastWatch = true;
            _clearUndoStack();
        }

        protected override string _exportToFile()
        {
            return "TODO"; // TODO
        }
    }
}

// undo/redo: https://stackoverflow.com/questions/1900450/wpf-how-to-prevent-a-control-from-stealing-a-key-gesture
// Can maybe use avalonEdit code folding to get the correct line numbers?
// Use UndoStack.ClearAll() after every change to prevent undo/redo
// When changing components: component.Focus();
// https://stackoverflow.com/questions/29175018/how-to-style-avalonedit-scrollbars
