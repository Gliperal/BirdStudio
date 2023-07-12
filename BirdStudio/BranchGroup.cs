using System.Collections.Generic;

namespace BirdStudio
{
    public class BranchGroup : IBranchSection
    {
        private Editor editor;
        public Branch parent;
        public BranchGroupHeader headerComponent;
        public List<Branch> branches;
        public int activeBranch;

        public BranchGroup(Editor editor, List<Branch> branches, int activeBranch = 0)
        {
            this.editor = editor;
            this.branches = branches;
            foreach (Branch branch in branches)
                branch.parent = this;
            this.activeBranch = activeBranch;
            headerComponent = new BranchGroupHeader(this);
            updateHeader();
        }

        public IBranchSection clone()
        {
            List<Branch> newBranches = new List<Branch>();
            foreach (Branch branch in branches)
                newBranches.Add(branch.clone());
            return new BranchGroup(editor, newBranches, activeBranch);
        }

        private void _insertBranch(Branch branch, int index=-1)
        {
            branch.parent = this;
            if (index == -1)
                branches.Add(branch);
            else
                branches.Insert(index, branch);
        }

        public Branch getActiveBranch()
        {
            return branches[activeBranch];
        }

        public void updateHeader()
        {
            headerComponent.setBranch($"({activeBranch + 1}/{branches.Count})", branches[activeBranch].getName());
        }

        public void renameBranch()
        {
            headerComponent.beginRename();
        }

        public void requestBranchNameChange(string newName)
        {
            EditHistoryItem edit = new RenameBranchEdit
            {
                branchNameInitial = branches[activeBranch].getName(),
                branchNameFinal = newName,
            };
            editor.requestEdit(branches[activeBranch], edit);
        }

        public string getText()
        {
            return branches[activeBranch].getText();
        }

        public void performEdit(EditHistoryItem edit)
        {
            if (edit is AddBranchEdit)
            {
                AddBranchEdit addEdit = (AddBranchEdit)edit;
                _insertBranch(addEdit.branchCopy.clone());
                activeBranch = branches.Count - 1;
            }
            else if (edit is ChangeActiveBranchEdit)
            {
                ChangeActiveBranchEdit changeEdit = (ChangeActiveBranchEdit)edit;
                activeBranch = changeEdit.activeBranchFinal;
            }
            else if (edit is RemoveBranchEdit)
            {
                RemoveBranchEdit removeEdit = (RemoveBranchEdit)edit;
                branches.RemoveAt(removeEdit.branchIndex);
                activeBranch = removeEdit.activeBranchFinal;
            }
            else
                throw new EditTypeNotSupportedException();
        }

        public void revertEdit(EditHistoryItem edit)
        {
            if (edit is AddBranchEdit)
            {
                AddBranchEdit addEdit = (AddBranchEdit)edit;
                branches.RemoveAt(branches.Count - 1);
                activeBranch = addEdit.activeBranchInitial;
            }
            else if (edit is ChangeActiveBranchEdit)
            {
                ChangeActiveBranchEdit changeEdit = (ChangeActiveBranchEdit)edit;
                activeBranch = changeEdit.activeBranchInitial;
            }
            else if (edit is RemoveBranchEdit)
            {
                RemoveBranchEdit removeEdit = (RemoveBranchEdit)edit;
                _insertBranch(removeEdit.branchCopy.clone(), removeEdit.branchIndex);
                activeBranch = removeEdit.branchIndex;
            }
            else
                throw new EditTypeNotSupportedException();
        }

        public EditHistoryItem addBranchEdit()
        {
            return new AddBranchEdit
            {
                activeBranchInitial = activeBranch,
                branchCopy = Branch.fromText("unnamed branch", "", editor),
            };
        }

        public bool canChangeBranch(int offset)
        {
            return
                (offset < 0 && activeBranch > 0) ||
                (offset > 0 && activeBranch < branches.Count - 1);
        }

        public EditHistoryItem changeBranchEdit(int offset)
        {
            int newActiveBranch = activeBranch + offset;
            if (newActiveBranch < 0)
                newActiveBranch = 0;
            if (newActiveBranch >= branches.Count)
                newActiveBranch = branches.Count - 1;
            if (newActiveBranch == activeBranch)
                return null;
            return new ChangeActiveBranchEdit
            {
                activeBranchInitial = activeBranch,
                activeBranchFinal = newActiveBranch,
            };
        }

        public EditHistoryItem removeBranchEdit()
        {
            if (branches.Count <= 1)
                return null;
            return new RemoveBranchEdit
            {
                branchIndex = activeBranch,
                activeBranchFinal = (activeBranch > 0) ? activeBranch - 1 : 0,
                branchCopy = branches[activeBranch].clone(),
            };
        }
    }
}
