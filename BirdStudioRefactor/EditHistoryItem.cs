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

    class RestructureBranchEdit : EditHistoryItem
    {
        public int nodeIndex;
        public IBranchSection[] removedSections;
        public IBranchSection[] insertedSections;
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
}
