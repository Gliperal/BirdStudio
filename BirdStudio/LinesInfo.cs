namespace BirdStudio
{
    class LinesInfo
    {
        public int start;
        public int end;
        public int length;
        public int endOfFirstLine;
        public string lines;
        public string firstLine;
        public string preText;
        public string postText;

        public LinesInfo(string text, int blockStart, int blockLength)
        {
            start = 0;
            if (blockStart > 0)
                start = text.LastIndexOf('\n', blockStart - 1) + 1;
            endOfFirstLine = text.IndexOf('\n', blockStart);
            if (endOfFirstLine == -1)
                endOfFirstLine = text.Length;
            end = text.IndexOf('\n', blockStart + blockLength);
            if (end == -1)
                end = text.Length;
            length = end - start;
            lines = text.Substring(start, length);
            firstLine = text.Substring(start, endOfFirstLine - start);
            preText = text.Substring(start, blockStart - start);
            postText = text.Substring(blockStart + blockLength, end - blockStart - blockLength);
        }
    }
}
