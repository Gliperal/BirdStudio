namespace BirdStudioRefactor
{
    class StringUtil
    {
        public static string removeSingleNewline(string text)
        {
            if (text.EndsWith('\n'))
                text = text.Substring(0, text.Length - 1);
            return text;
        }

        public static string removeSandwichingNewlines(string text)
        {
            if (text.Length == 1)
                return text;
            if (text.StartsWith('\n') && text.EndsWith('\n'))
                text = text.Substring(1, text.Length - 2);
            return text;
        }

        public static int firstIndexThatIsNot(string str, string anyOf, int start = 0)
        {
            int i = start;
            for (; i < str.Length; i++)
                if (!anyOf.Contains(str[i]))
                    return i;
            return i;
        }

        public static int nthIndexOf(string str, char c, int n)
        {
            int i = -1;
            for (int _ = 0; _ < n; _++)
            {
                if (i > str.Length - 1)
                    return -1;
                i = str.IndexOf(c, i + 1);
                if (i == -1)
                    return -1;
            }
            return i;
        }
    }
}
