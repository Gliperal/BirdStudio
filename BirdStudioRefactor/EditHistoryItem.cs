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

        // branch group edits
        public int branchIndex;
        public int activeBranchInitial;
        public int activeBranchFinal;
        public Branch branchCopy;

        // branch edits
        public int nodeIndex;
        public string branchNameInitial;
        public string branchNameFinal;
        public string preText;
        public BranchGroup branchGroupCopy;
        public string postText;

        // TODO Find a better way to do this
        public TASEditor parent;
    }
}
