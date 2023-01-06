using System.Collections.Generic;

namespace BirdStudioRefactor
{
    public abstract class EditHistoryItem
    {
        public List<int> targetID;
    }

    class ModifyTextEdit : EditHistoryItem
    {
        public int pos;
        public string textRemoved;
        public string textInserted;
        public int cursorPosInitial;
        public int cursorPosFinal;
    }

    class NewBranchGroupEdit : EditHistoryItem
    {
        public int nodeIndex;
        public string initialText;
        public string preText;
        public BranchGroup branchGroupCopy;
        public string postText;
    }

    class AddBranchEdit : EditHistoryItem
    {
        public int activeBranchInitial;
        public Branch branchCopy;
    }

    class ChangeActiveBranchEdit : EditHistoryItem
    {
        public int activeBranchInitial;
        public int activeBranchFinal;
    }

    class RenameBranchEdit : EditHistoryItem
    {
        public string branchNameInitial;
        public string branchNameFinal;
    }

    class RemoveBranchEdit : EditHistoryItem
    {
        public int branchIndex;
        public int activeBranchFinal;
        public Branch branchCopy;
    }

    class DeleteBranchGroupEdit : EditHistoryItem
    {
        public int nodeIndex;
        public string preText;
        public BranchGroup branchGroupCopy;
        public string postText;
        public string replacementText;
    }
}
