namespace BirdStudioRefactor
{
    class EditHistoryItem
    {
        public int sectionIndex;
        public int pos;
        public string textRemoved;
        public string textInserted;
        public int cursorPosInitial;
        public int cursorPosFinal;
    }
}
