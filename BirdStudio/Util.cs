﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace BirdStudio
{
    public class TopBottom
    {
        public double top;
        public double bottom;
    }

    public class FrameAndBlock
    {
        public int frame;
        public InputsBlock block;
    }

    public class NewBranchInfo
    {
        public string preText;
        public string splitText;
        public string postText;
        public bool bottomless;
        public string newBranchText = "";
    }

    public static class Util
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

        public static void log(string message)
        {
            File.AppendAllText("log.txt", message + "\n");
        }

        public static void logAndReportException(Exception e)
        {
            bool writeSuccess = false;
            try
            {
                File.AppendAllText("error.log", e.ToString() + "\n\n");
                writeSuccess = true;
            }
            catch (Exception) { }
            string message = "Unexpected error: " + e.Message +
                (writeSuccess
                    ? ". Details written to error.log."
                    : ". In attempting to log the error, a second error occured."
                );
            MessageBox.Show(message);
        }

        public static void handleCrash(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                logAndReportException(e);
                Environment.Exit(1);
            }
        }

        public static string convertOldFormatToNew(string tas)
        {
            string stage = "Twin Tree Village";
            string rerecords = "0";
            List<string> lines = tas.Split('\n').ToList();
            for (int i = 0; i < lines.Count;)
            {
                string line = lines[i].Trim();
                if (line.StartsWith(">stage "))
                {
                    stage = line.Substring(7);
                    lines.RemoveAt(i);
                    continue;
                }
                else if (line.StartsWith(">rerecords "))
                {
                    rerecords = line.Substring(11);
                    lines.RemoveAt(i);
                    continue;
                }
                i++;
            }
            return "<tas stage=\"" + stage + "\" rerecords=\"" + rerecords + "\"><inputs>\n" + string.Join('\n', lines) + "\n</inputs></tas>";
        }

        public static string getXmlAttribute(XmlNode node, string name, string defaultValue)
        {
            XmlNode attributeNode = node.Attributes.GetNamedItem(name);
            if (attributeNode == null)
                return defaultValue;
            else
                return attributeNode.InnerText;
        }

        public static int getXmlAttributeAsInt(XmlNode node, string name, int defaultValue)
        {
            XmlNode attributeNode = node.Attributes.GetNamedItem(name);
            if (attributeNode != null)
            {
                int res;
                if (Int32.TryParse(attributeNode.InnerText, out res))
                    return res;
            }
            return defaultValue;
        }

        public static List<IBranchSection> getRangeClone(List<IBranchSection> list, int start, int count)
        {
            List<IBranchSection> res = new List<IBranchSection>();
            for (int i = start; i < start + count; i++)
                res.Add(list[i].clone());
            return res;
        }

        public static int countLeadingWhitespace(string str)
        {
            int i;
            for (i = 0; i < str.Length; i++)
                if (!Char.IsWhiteSpace(str[i]))
                    return i;
            return i;
        }

        public static string filePathToFileName(string path)
        {
            return path.Split('\\').Last().Split('/').Last();
        }

        public static string filePathToNameOnly(string path)
        {
            string name = filePathToFileName(path);
            int i = name.LastIndexOf('.');
            if (i == -1)
                return name;
            else
                return name.Substring(0, i);
        }

        public static string getGameDirectory()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName("TheKingsBird");
                string path = processes.First().MainModule.FileName;
                int i = path.LastIndexOf('\\');
                return path.Substring(0, i + 1);
            }
            catch
            {
                return null;
            }
        }
    }
}
