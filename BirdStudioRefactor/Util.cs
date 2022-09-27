using System;
using System.IO;
using System.Windows.Forms;

namespace BirdStudioRefactor
{
    class TopBottom
    {
        public double top;
        public double bottom;
    }

    class FrameAndBlock
    {
        public int frame;
        public TASEditorSection block;
    }

    static class Util
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

        public static int substringCount(string str, string substr, int length = -1)
        {
            if (length < 0 || length > str.Length)
                length = str.Length;
            int end = length - substr.Length;
            int count = 0;
            for (int i = 0; i <= end; i++)
                if (str.Substring(i).StartsWith(substr))
                    count++;
            return count;
        }

        public static void handleCrash(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                bool writeSuccess = false;
                try
                {
                    File.AppendAllText("error.log", e.ToString() + "\n\n");
                    writeSuccess = true;
                }
                catch (Exception e2) { }
                string message = "Unexpected error: " + e.Message +
                    (writeSuccess
                        ? ". Details written to error.log."
                        : ". In attempting to log the error, a second error occured."
                    );
                MessageBox.Show(message);
                Environment.Exit(1);
            }
        }
    }
}
