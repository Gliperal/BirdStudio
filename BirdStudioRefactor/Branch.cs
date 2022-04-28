using System;
using System.Collections.Generic;
using System.Windows;

namespace BirdStudioRefactor
{
    class BranchNode
    {
        public TASEditorSection inputs;
        public List<Branch> branches;
    }

    class Branch
    {
        private string name;
        List<BranchNode> nodes = new List<BranchNode>();

        private Branch() {}

        private static string removeSingleNewline(string text)
        {
            if (text.EndsWith('\n'))
                text = text.Substring(0, text.Length - 1);
            return text;
        }

        private static BranchNode _makeBranchNode(string firstBranch, ref string text, TASEditor parent)
        {
            List<Branch> branches = new List<Branch>();
            Branch branch = new Branch { name = firstBranch };
            for (int lineStart = 0;;)
            {
                string line = text.Substring(lineStart).Split('\n', 2)[0];
                string command = line.Split(null, 2)[0];
                string branchName = line.Substring(command.Length).Trim();
                if (command == ">startbranch" || command == ">branch" || command == ">endbranch")
                {
                    string inputs = text.Substring(0, lineStart);
                    inputs = removeSingleNewline(inputs);
                    branch.nodes.Add(new BranchNode { inputs = new TASEditorSection(inputs, parent) });
                    int nextLineStart = lineStart + line.Length;
                    if (nextLineStart < text.Length)
                        nextLineStart++; // skip past newline unless end of file
                    text = text.Substring(nextLineStart);
                    lineStart = 0;
                    if (command == ">startbranch")
                    {
                        branch.nodes.Add(_makeBranchNode(branchName, ref text, parent));
                    }
                    else if (command == ">branch")
                    {
                        branches.Add(branch);
                        branch = new Branch { name = branchName };
                    }
                    else if(command == ">endbranch")
                    {
                        branches.Add(branch);
                        return new BranchNode { branches = branches };
                    }
                }
                else
                {
                    lineStart = text.IndexOf("\n>", lineStart, StringComparison.Ordinal) + 1;
                    if (lineStart == 0)
                        throw new FormatException("Unexpected end of file (potentially unmatched >startbranch)");
                }
            }
        }

        public static Branch fromFile(string text, TASEditor parent)
        {
            text = text.Replace("\r\n", "\n");
            text = text + "\n>endbranch";
            BranchNode node = _makeBranchNode("", ref text, parent);
            if (text.Length > 0)
                throw new FormatException("Unmatched >endbranch");
            return node.branches[0];
        }

        public string toFile()
        {
            string contents = "";
            foreach (BranchNode node in nodes)
            {
                if (node.inputs != null)
                    contents += "\n" + node.inputs.text;
                else
                {
                    for (int i = 0; i < node.branches.Count; i++)
                    {
                        if (i == 0)
                            contents += "\n>startbranch " + node.branches[0].name;
                        else
                            contents += "\n>branch " + node.branches[0].name;
                        contents += "\n" + node.branches[i].toFile();
                    }
                    contents += "\n>endbranch";
                }
            }
            return contents.Substring(1);
        }

        public List<UIElement> getComponents()
        {
            List<UIElement> components = new List<UIElement>();
            foreach (BranchNode node in nodes)
            {
                if (node.inputs != null)
                    components.Add(node.inputs.getComponent());
                else
                {
                    // TODO branch start
                    components.AddRange(node.branches[0].getComponents());
                    // TODO branch end
                }
            }
            return components;
        }
    }
}
