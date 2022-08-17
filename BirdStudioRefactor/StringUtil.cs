namespace BirdStudioRefactor
{
    class StringUtil
    {
        public static int firstIndexThatIsNot(string str, string anyOf, int start = 0)
        {
            int i = start;
            for (; i < str.Length; i++)
                if (!anyOf.Contains(str[i]))
                    return i;
            return i;
        }
    }
}
