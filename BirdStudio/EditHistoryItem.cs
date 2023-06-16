using System.Collections.Generic;

namespace BirdStudio
{
    public abstract class EditHistoryItem
    {
        public List<int> targetID;
        public List<int> focusInitial;
        public List<int> focusFinal;
    }

    public class ModifyTextEdit : EditHistoryItem
    {
        public int pos;
        public string textRemoved;
        public string textInserted;
        public int cursorPosInitial;
        public int cursorPosFinal;
    }

    public class RestructureBranchEdit : EditHistoryItem
    {
        public int nodeIndex;
        public IBranchSection[] removedSections;
        public IBranchSection[] insertedSections;
    }

    public class AddBranchEdit : EditHistoryItem
    {
        public int activeBranchInitial;
        public Branch branchCopy;
    }

    public class ChangeActiveBranchEdit : EditHistoryItem
    {
        public int activeBranchInitial;
        public int activeBranchFinal;
    }

    public class RenameBranchEdit : EditHistoryItem
    {
        public string branchNameInitial;
        public string branchNameFinal;
    }

    public class RemoveBranchEdit : EditHistoryItem
    {
        public int branchIndex;
        public int activeBranchFinal;
        public Branch branchCopy;
    }
}
