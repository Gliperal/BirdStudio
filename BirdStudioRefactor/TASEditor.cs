using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace BirdStudioRefactor
{
    class TASEditor : FileManager
    {
        private const string DEFAULT_FILE_TEXT = ">stage Twin Tree Village\n>rerecords 0\n\n  29";

        private StackPanel panel;
        private Branch masterBranch;
        private List<EditHistoryItem> editHistory = new List<EditHistoryItem>();
        private int editHistoryLocation = 0;
        private bool tasEditedSinceLastWatch = true;

        public TASEditor(MainWindow window, StackPanel panel) : base(window)
        {
            this.panel = panel;
            neww();
        }

        public void editPerformed(IEditable target, EditHistoryItem edit)
        {
            List<int> id = masterBranch.findEditTargetID(target);
            edit.targetID = id;
            if (editHistoryLocation < editHistory.Count)
                editHistory.RemoveRange(editHistoryLocation, editHistory.Count - editHistoryLocation);
            editHistory.Add(edit);
            editHistoryLocation++;
            fileChanged();
        }

        public void requestEdit(IEditable target, EditHistoryItem edit)
        {
            target.performEdit(edit);
            editPerformed(target, edit);
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
            IEditable target = masterBranch.getEditable(edit.targetID);
            target.revertEdit(edit);
            editHistoryLocation--;
            // TODO change focus to sections[edit.sectionIndex]
            fileChanged();
        }

        public bool canRedo()
        {
            return editHistoryLocation < editHistory.Count;
        }

        public void redo()
        {
            EditHistoryItem edit = editHistory[editHistoryLocation];
            IEditable target = masterBranch.getEditable(edit.targetID);
            target.performEdit(edit);
            editHistoryLocation++;
            // TODO change focus to sections[edit.sectionIndex]
            fileChanged();
        }

        private void _clearUndoStack()
        {
            editHistory = new List<EditHistoryItem>();
            editHistoryLocation = 0;
        }

        public void newBranch()
        {
            IInputElement focus = FocusManager.GetFocusedElement(panel);
            // TODO
        }

        public void addBranch()
        {
            IInputElement focus = FocusManager.GetFocusedElement(panel);
            // TODO
        }

        public void cycleBranch()
        {
            IInputElement focusedElement = FocusManager.GetFocusedElement(panel);
            // This approach kinda sucks since we have to search for the target id twice, but w/e
            List<int> id = masterBranch.findEditTargetID(focusedElement, EditableTargetType.BranchGroup);
            if (id == null)
                return;
            BranchNode target = (BranchNode) masterBranch.getEditable(id);
            EditHistoryItem edit = target.cycleBranchEdit();
            requestEdit(target, edit);
        }

        public void removeBranch()
        {
            IInputElement focus = FocusManager.GetFocusedElement(panel);
            // TODO
        }

        protected override void _importFromFile(string tas)
        {
            if (tas == null)
                tas = DEFAULT_FILE_TEXT;
            masterBranch = Branch.fromFile(tas, this);
            // TODO Catch FormatExceptions
            panel.Children.Clear();
            foreach (UIElement component in masterBranch.getComponents())
                panel.Children.Add(component);
            tasEditedSinceLastWatch = true;
            _clearUndoStack();
        }

        protected override string _exportToFile()
        {
            return masterBranch.toFile();
        }
    }
}

// undo/redo: https://stackoverflow.com/questions/1900450/wpf-how-to-prevent-a-control-from-stealing-a-key-gesture
// Can maybe use avalonEdit code folding to get the correct line numbers?
// Use UndoStack.ClearAll() after every change to prevent undo/redo
// When changing components: component.Focus();
// https://stackoverflow.com/questions/29175018/how-to-style-avalonedit-scrollbars
