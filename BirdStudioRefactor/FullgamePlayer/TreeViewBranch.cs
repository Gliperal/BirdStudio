using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Xml;

namespace BirdStudioRefactor
{
    public class TreeViewBranch : TreeViewItem
    {
        public string stage;

        // TODO private
        public TreeViewBranchGroup parent;
        public string name;
        public List<TreeViewBranchGroup> groups = new List<TreeViewBranchGroup>();
        public Branch branch;
        public bool active;

        private TreeViewBranch() { }

        // TODO remove header ?
        // maybe I can remove both header and name even?
        public TreeViewBranch(TreeViewBranchGroup parent, string name, string header, Branch branch)
        {
            this.parent = parent;
            this.name = name;
            this.Header = header;
            this.branch = branch;
            foreach (IBranchSection section in branch.nodes)
                if (section is BranchGroup)
                {
                    BranchGroup branchGroup = (BranchGroup)section;
                    TreeViewBranchGroup group = new TreeViewBranchGroup(branchGroup, this);
                    groups.Add(group);
                    foreach (TreeViewBranch child in group.branches)
                        AddChild(child);
                }
            //_updateColors();
        }

        public static TreeViewBranch from(string filename, string rootDirectory)
        {
            try
            {
                string tas = File.ReadAllText(rootDirectory + filename);
                tas = tas.Replace("\r\n", "\n");
                if (!tas.Trim().StartsWith('<'))
                    tas = Util.convertOldFormatToNew(tas);
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(tas);
                if (xml.DocumentElement.Name != "tas")
                    throw new Exception();
                TASEditorHeader header = new TASEditorHeader(xml.DocumentElement.Attributes);
                Branch branch = Branch.fromXml(xml.DocumentElement, null);
                TreeViewBranch x = new TreeViewBranch(null, filename, filename, branch);
                x.stage = header.stage();
                x.setActive(true);
                return x;
            }
            catch
            {
                return new TreeViewBranch
                {
                    Header = filename,
                    Foreground = Brushes.Red,
                };
            }
        }

        public static TreeViewBranch from(List<string> lines, string rootDirectory)
        {
            TreeViewBranch x = from(lines[0].Trim(), rootDirectory);
            lines.RemoveAt(0);
            x._parse(lines, 0);
            return x;
        }

        private TreeViewBranch _findChildBranch(string id)
        {
            TreeViewBranch child;
            while (true)
            {
                Regex r = new Regex(@"^\s*(\d+)-(\d+)\.(.*)$");
                Match m = r.Match(id);
                if (!m.Success)
                    break;
                int groupIndex;
                int branchIndex;
                if (!Int32.TryParse(m.Groups[1].Value, out groupIndex))
                    break;
                if (!Int32.TryParse(m.Groups[2].Value, out branchIndex))
                    break;
                string branchName = m.Groups[3].Value;
                child = groups[groupIndex - 1].branches[branchIndex - 1];
                if (child.branch.getName().Trim() == branchName.Trim())
                    return child;
                throw new FormatException("Expected branch name doesn't match branch at that location: \"" + id + "\"");
            }
            child = _getUniqueBranchWithName(id);
            if (child == null)
                throw new FormatException("Branch name \"" + id + "\" not found or not unique.");
            return child;
        }

        // starting from the current line, deal with all the upcoming lines that are deeper than indentLevel
        private void _parse(List<string> lines, int indentLevel)
        {
            if (lines.Count == 0)
                return;
            int subIndentLevel = Util.countLeadingWhitespace(lines[0]);
            if (subIndentLevel <= indentLevel)
                return;
            while (lines.Count > 0)
            {
                int indent = Util.countLeadingWhitespace(lines[0]);
                if (indent == subIndentLevel)
                {
                    // TODO catch errors with incorrect formatting
                    string branchID = lines[0].Trim();
                    lines.RemoveAt(0);
                    try
                    {
                        TreeViewBranch child = _findChildBranch(branchID);
                        child.parent.force(child, true);
                        child._parse(lines, subIndentLevel);
                    }
                    catch (FormatException e)
                    {
                        MessageBox.Show("Ignoring error while parsing file: " + e.Message);
                    }
                }
                else if (indent < subIndentLevel)
                    return;
                else if (indent > indentLevel)
                    // Previous subbranch must have failed, so ignore its children
                    continue;
            }
        }

        // TODO private
        public void _updateColors()
        {
            if (parent == null)
                return;
            bool isMainBranch = parent.isDefaultBranch(this);
            bool forced = parent.isForcedBranch(this);
            if (forced && !Header.ToString().StartsWith("[F] "))
                Header = "[F] " + Header;
            else
                while (Header.ToString().StartsWith("[F] "))
                    Header = Header.ToString().Substring(4);
            this.Foreground = Brushes.DarkGray; // which is for some reason lighter than Gray..?
            this.FontWeight = System.Windows.FontWeights.Normal;
            if (active)
            {
                this.Foreground = isMainBranch ? Brushes.Purple : Brushes.Black;
                this.FontWeight = System.Windows.FontWeights.Bold;
            }
            else if (isMainBranch)
            {
                this.Foreground = Brushes.Plum;
            }
        }

        private TreeViewBranch _getUniqueBranchWithName(string name)
        {
            TreeViewBranch result = null;
            foreach (TreeViewBranchGroup group in groups)
                foreach (TreeViewBranch x in group.branches)
                    if (x.name == name)
                    {
                        if (result == null)
                            result = x;
                        else
                            return null;
                    }
            return result;
        }

        private string _toText(string indent)
        {
            indent = " " + indent;
            string text = "";
            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                TreeViewBranchGroup group = groups[groupIndex];
                int branchIndex = group.forcedBranch;
                if (branchIndex == -1)
                    continue;
                TreeViewBranch forcedBranch = group.branches[branchIndex];
                string name = forcedBranch.branch.getName();
                if (_getUniqueBranchWithName(name) == null)
                    name = String.Format("{0}-{1}. {2}", groupIndex + 1, branchIndex + 1, name);
                text += indent + name + "\n";
                text += group.branches[group.forcedBranch]._toText(indent);
            }
            return text;
        }

        public string toText()
        {
            return name + "\n" + _toText("");
        }

        public void setActive(bool active)
        {
            if (this.active == active)
                return;
            this.active = active;
            _updateColors();
            foreach (TreeViewBranchGroup group in groups)
                group.activeBranch().setActive(active);
        }

        public void toggleForced()
        {
            if (parent == null)
                return;
            bool on = !parent.isForcedBranch(this);
            parent.force(this, on);
        }
    }
}
