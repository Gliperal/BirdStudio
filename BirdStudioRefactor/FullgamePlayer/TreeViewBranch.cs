using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;

namespace BirdStudioRefactor
{
    public class TreeViewBranch : TreeViewItem
    {
        public const string TAS_FILES_LOCATION = "C:/Users/Gliperal/Gliperal/Games/The King's Bird/executable versions/TASbot/tas-files/"; // TODO

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

        public static TreeViewBranch from(string filename)
        {
            try
            {
                string tas = File.ReadAllText(TAS_FILES_LOCATION + filename);
                tas = tas.Replace("\r\n", "\n");
                if (!tas.Trim().StartsWith('<'))
                    tas = Util.convertOldFormatToNew(tas);
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(tas);
                if (xml.DocumentElement.Name != "tas")
                    throw new Exception();
                Branch branch = Branch.fromXml(xml.DocumentElement, null);
                TreeViewBranch x = new TreeViewBranch(null, filename, filename, branch);
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

        public static TreeViewBranch from(List<string> lines)
        {
            TreeViewBranch x = from(lines[0].Trim());
            lines.RemoveAt(0);
            x._parse(lines, 0);
            return x;
        }

        // starting from the current line, deal with all the upcoming lines that are deeper than indentLevel
        private void _parse(List<string> lines, int indentLevel)
        {
            /*
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
                    string line = lines[0];
                    lines.RemoveAt(0);
                    line = line.Trim();
                        // line = "   3-1. main branch"
                        // groupIndex-branchIndex. branchName;
                        // ^\s*(\d+)-(\d+)\.(.*)$
                        // https://learn.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.match.groups?view=net-7.0
                        // https://stackoverflow.com/questions/6375873/regular-expression-groups-in-c-sharp
                        TreeViewBranch child = groups[groupIndex].branches[branchIndex];
                        if (child.branch.getName().Trim() == branchName.Trim())
                        {
                            child.Checked = true;
                            child._parse(lines, subIndentLevel);
                        }
                        else
                        {
                            _addChild(groupIndex, new TreeViewBranch
                            {
                                Header = branchName.Trim(),
                                Foreground = Brushes.Red,
                            });
                        }
                }
                if (indent < subIndentLevel)
                    return;
            }
            */
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
            /*
            foreach (TreeViewBranchGroup group in groups)
            {
                int activeBranch = group.branchGroup.activeBranch;
                for (int i = 0; i < group.branches.Count; i++)
                    group.branches[i].update(forced && i == activeBranch);
            }
            */
        }

        private bool nameIsUnique(string name)
        {
            int count = 0;
            foreach (TreeViewBranchGroup group in groups)
                foreach (TreeViewBranch x in group.branches)
                    if (x.name == name)
                        count++;
            return count == 1;
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
                if (!nameIsUnique(name))
                    name = String.Format("{}-{}. {}", groupIndex, branchIndex, name);
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
            // TODO setActive(false) should cascade down: all the children become unactive as well
            // setActive(true) should also cascade down: the currently active child of every branchgroup becomes active
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
