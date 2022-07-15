using System.Collections.Generic;

namespace BirdStudioRefactor
{
    public enum EditType
    {
        Unknown,
        ModifyText,
        NewBranchGroup,
        AddBranch,
        ChangeActiveBranch,
        RenameBranch,
        RemoveBranch,
        DeleteBranchGroup,
    };

    class EditHistoryItem
    {
        public EditType type;
        public List<int> targetID;

        // text edits
        public int pos;
        public string textRemoved;
        public string textInserted;
        public int cursorPosInitial;
        public int cursorPosFinal;

        // branch edits
        public string branchName1;
        public string branchName2;
        public int activeBranchInitial;
        public int activeBranchFinal;
        public int branchIndex;
        public Branch branchCopy;
        public BranchGroup branchGroupCopy;
    }
}
