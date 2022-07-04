using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace BirdStudioRefactor
{
    enum EditableTargetType
    {
        Any,
        InputBlock,
        Branch,
        BranchGroup,
    }

    class BranchNode : IEditable
    {
        public TASEditorSection inputs;

        public TextBlock headerComponent;
        public List<Branch> branches;
        public int activeBranch;

        public BranchNode(TASEditorSection inputs)
        {
            this.inputs = inputs;
        }

        public BranchNode(List<Branch> branches)
        {
            this.branches = branches;
            this.activeBranch = 0;
            headerComponent = new TextBlock();
            onChange();
        }

        private void onChange()
        {
            headerComponent.Text = branches[activeBranch].getName();
        }

        public void performEdit(EditHistoryItem edit)
        {
            switch(edit.type)
            {
                case EditType.AddBranch:
                    // TODO
                    // Make sure to update active branch
                    return;
                case EditType.ChangeActiveBranch:
                    activeBranch = edit.activeBranchFinal;
                    onChange();
                    return;
                case EditType.RemoveBranch:
                    // TODO
                    // Make sure to update active branch
                    return;
                case EditType.ModifyText:
                    inputs.performEdit(edit);
                    break;
                default:
                    throw new Exception("Edit type not supported.");
            }
        }

        public void revertEdit(EditHistoryItem edit)
        {
            switch (edit.type)
            {
                case EditType.AddBranch:
                    // TODO
                    // Make sure to update active branch
                    return;
                case EditType.ChangeActiveBranch:
                    activeBranch = edit.activeBranchInitial;
                    onChange();
                    return;
                case EditType.RemoveBranch:
                    // TODO
                    // Make sure to update active branch
                    return;
                case EditType.ModifyText:
                    inputs.revertEdit(edit);
                    break;
                default:
                    throw new Exception("Edit type not supported.");
            }
        }

        public EditHistoryItem addBranchEdit()
        {
            return new EditHistoryItem
            {
                // TODO
            };
        }

        public EditHistoryItem cycleBranchEdit()
        {
            return new EditHistoryItem {
                type = EditType.ChangeActiveBranch,
                activeBranchInitial = activeBranch,
                activeBranchFinal = (activeBranch + 1) % branches.Count,
            };
        }

        public EditHistoryItem removeBranchEdit()
        {
            return new EditHistoryItem
            {
                // TODO
            };
        }
    }

    class Branch : IEditable
    {
        private string name;
        // TODO Can maybe remove inputs from BranchNode, making it just
        // BranchGroup, and making this List<IEditable>
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
                    branch.nodes.Add(new BranchNode(new TASEditorSection(inputs, parent)));
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
                        return new BranchNode(branches);
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

        public string getName()
        {
            return name;
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
                    // TODO better UI style
                    components.Add(new Separator());
                    components.Add(node.headerComponent);
                    components.AddRange(node.branches[node.activeBranch].getComponents());
                    components.Add(new Separator());
                }
            }
            return components;
        }

        private List<int> _getEditable(IInputElement element, ref IEditable target)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].inputs != null)
                {
                    if (nodes[i].inputs.getComponent().TextArea == element)
                    {
                        target = nodes[i].inputs;
                        return new List<int> { i };
                    }
                }
                else
                    for (int j = 0; j < nodes[i].branches.Count; j++)
                    {
                        List<int> id = nodes[i].branches[j]._getEditable(element, ref target);
                        if (id != null)
                        {
                            id.Insert(0, i);
                            id.Insert(1, j);
                            return id;
                        }
                    }
            }
            return null;
        }

        public List<int> findEditTargetID(IEditable target)
        {
            if (this == target)
                return new List<int>();
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] == target || nodes[i].inputs == target)
                    return new List<int> { i };
                else if (nodes[i].inputs == null)
                {
                    for (int j = 0; j < nodes[i].branches.Count; j++)
                    {
                        List<int> id = nodes[i].branches[j].findEditTargetID(target);
                        if (id != null)
                        {
                            id.Insert(0, i);
                            id.Insert(1, j);
                            return id;
                        }
                    }
                }
            }
            return null;
        }

        public List<int> findEditTargetID(IInputElement focusedElement, EditableTargetType type)
        {
            IEditable target = null;
            List<int> id = _getEditable(focusedElement, ref target);
            if (id == null)
                return null;
            switch(type)
            {
                case EditableTargetType.Any:
                    return id;
                case EditableTargetType.Branch:
                    if (id.Count % 2 == 1) // this might always be true
                        id.RemoveAt(id.Count - 1);
                    return id;
                case EditableTargetType.InputBlock:
                    // TODO If branch group selected, then active branch inputs
                    return id;
                case EditableTargetType.BranchGroup:
                    if (target is BranchNode)
                        return id;
                    if (id.Count < 2)
                        return null;
                    id.RemoveRange(id.Count - 2, 2);
                    return id;
                default:
                    return null;
            }
        }

        public IEditable getEditable(List<int> id)
        {
            Branch branch = this;
            int i;
            for (i = 0; i + 1 < id.Count; i += 2)
            {
                int nodeIndex = id[i];
                int branchIndex = id[i+1];
                branch = branch.nodes[nodeIndex].branches[branchIndex];
            }
            if (i == id.Count)
                return branch;
            else
                return branch.nodes[id[i]];
        }

        public void performEdit(EditHistoryItem edit)
        {
            switch (edit.type)
            {
                case EditType.AddBranch:
                case EditType.RemoveBranch:
                case EditType.ChangeActiveBranch:
                    throw new NotImplementedException();
                    // TODO should send execution to parent element which is an actual BranchNode
                default:
                    throw new Exception("No other type of edit should reach this point.");
            }
        }

        public void revertEdit(EditHistoryItem edit)
        {
            // TODO
        }
    }
}
