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
            if (edit.type != EditType.ModifyText)
                reloadComponents();
        }

        private void reloadComponents()
        {
            panel.Children.Clear();
            foreach (UIElement component in masterBranch.getComponents())
                panel.Children.Add(component);
            // TODO Maintain scroll position
            // TODO re-highlight active line?
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
            if (edit.type != EditType.ModifyText)
                reloadComponents();
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
            if (edit.type != EditType.ModifyText)
                reloadComponents();
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
            IInputElement focusedElement = FocusManager.GetFocusedElement(panel);
            List<int> id = masterBranch.findEditTargetID(focusedElement, EditableTargetType.InputBlock);
            if (id == null)
                return;
            int inputBlockIndex = id[id.Count - 1];
            id.RemoveAt(id.Count - 1);
            Branch target = (Branch)masterBranch.getEditable(id);
            EditHistoryItem edit = target.newBranchGroupEdit(inputBlockIndex, this);
            requestEdit(target, edit);
        }

        public void addBranch()
        {
            IInputElement focusedElement = FocusManager.GetFocusedElement(panel);
            List<int> id = masterBranch.findEditTargetID(focusedElement, EditableTargetType.BranchGroup);
            if (id == null)
                return;
            BranchGroup target = (BranchGroup)masterBranch.getEditable(id);
            EditHistoryItem edit = target.addBranchEdit(this);
            requestEdit(target, edit);
        }

        public void cycleBranch()
        {
            IInputElement focusedElement = FocusManager.GetFocusedElement(panel);
            // This approach kinda sucks since we have to search for the target id twice, but w/e
            List<int> id = masterBranch.findEditTargetID(focusedElement, EditableTargetType.BranchGroup);
            if (id == null)
                return;
            BranchGroup target = (BranchGroup) masterBranch.getEditable(id);
            EditHistoryItem edit = target.cycleBranchEdit();
            requestEdit(target, edit);
        }

        public void removeBranch()
        {
            IInputElement focusedElement = FocusManager.GetFocusedElement(panel);
            List<int> id = masterBranch.findEditTargetID(focusedElement, EditableTargetType.BranchGroup);
            if (id == null)
                return;
            BranchGroup target = (BranchGroup)masterBranch.getEditable(id);
            // If only one branch left, the entire group should be deleted.
            if (target.branches.Count <= 1)
            {
                deleteBranchGroup();
                return;
            }
            EditHistoryItem edit = target.removeBranchEdit();
            requestEdit(target, edit);
        }

        public void deleteBranchGroup()
        {
            IInputElement focusedElement = FocusManager.GetFocusedElement(panel);
            List<int> id = masterBranch.findEditTargetID(focusedElement, EditableTargetType.BranchGroup);
            if (id == null)
                return;
            int branchGroupIndex = id[id.Count - 1];
            id.RemoveAt(id.Count - 1);
            Branch target = (Branch)masterBranch.getEditable(id);
            // TODO Confirmation dialogue ("Are you sure you want to delete _ branches (_ subbranches) (_ lines)?")
            EditHistoryItem edit = target.deleteBranchGroupEdit(branchGroupIndex, this);
            requestEdit(target, edit);
        }

        public void renameBranch()
        {
            IInputElement focusedElement = FocusManager.GetFocusedElement(panel);
            List<int> id = masterBranch.findEditTargetID(focusedElement, EditableTargetType.Branch);
            if (id == null)
                return;
            Branch target = (Branch)masterBranch.getEditable(id);
            RenameDialogue dialogue = new RenameDialogue();
            bool? res = dialogue.ShowDialog();
            if (res != true)
                return;
            string newName = dialogue.ResponseText;
            EditHistoryItem edit = target.renameBranchEdit(newName);
            requestEdit(target, edit);
        }

        protected override void _importFromFile(string tas)
        {
            if (tas == null)
                tas = DEFAULT_FILE_TEXT;
            masterBranch = Branch.fromFile(tas, this);
            // TODO Catch FormatExceptions
            reloadComponents();
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
