using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;

namespace BirdStudioRefactor
{
    class TASEditor
    {
        private StackPanel panel;
        private Point cursor;
        private List<TASEditorSection> sections; // TODO convert from list into tree (may need to change index to some kind of id too)
        private List<EditHistoryItem> editHistory = new List<EditHistoryItem>();
        private int editHistoryLocation = 0;

        public TASEditor(StackPanel panel)
        {
            this.panel = panel;
            sections = new List<TASEditorSection>();
            for (int i = 0; i < 5; i++)
            {
                TASEditorSection section = new TASEditorSection(">stage Twin Tree Village\n>rerecords 0\n\n  29", this);
                panel.Children.Add(section.getComponent());
                sections.Add(section);
                cursor = new Point(0, 0);
            }
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

        public void undo()
        {
            if (editHistoryLocation == 0)
                return;
            EditHistoryItem edit = editHistory[editHistoryLocation - 1];
            sections[edit.sectionIndex].revertEdit(edit);
            editHistoryLocation--;
        }

        public void redo()
        {
            if (editHistoryLocation == editHistory.Count)
                return;
            EditHistoryItem edit = editHistory[editHistoryLocation];
            sections[edit.sectionIndex].performEdit(edit);
            editHistoryLocation++;
        }
    }
}

// Can maybe use avalonEdit code folding to get the correct line numbers?
// Use UndoStack.ClearAll() after every change to prevent undo/redo
// When changing components: component.Focus();
// https://stackoverflow.com/questions/29175018/how-to-style-avalonedit-scrollbars
