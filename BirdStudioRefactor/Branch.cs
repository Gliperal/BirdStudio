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

    interface IBranchSection : IEditable
    {
        public string getText();
        public IBranchSection clone();
    }

    class BranchGroup : IBranchSection
    {
        public TextBlock headerComponent;
        public List<Branch> branches;
        public int activeBranch;

        public BranchGroup(List<Branch> branches, int activeBranch = 0)
        {
            this.branches = branches;
            this.activeBranch = activeBranch;
            headerComponent = new TextBlock();
            updateHeader();
        }

        public IBranchSection clone()
        {
            List<Branch> newBranches = new List<Branch>();
            foreach (Branch branch in branches)
                newBranches.Add(branch.clone());
            return new BranchGroup(newBranches, activeBranch);
        }

        public void updateHeader()
        {
            headerComponent.Text = branches[activeBranch].getName();
        }

        public string getText()
        {
            return branches[activeBranch].getText();
        }

        public void performEdit(EditHistoryItem edit)
        {
            switch(edit.type)
            {
                case EditType.AddBranch:
                    branches.Add(edit.branchCopy);
                    activeBranch = branches.Count - 1;
                    // TODO any time the active branch changes, the focussed element should also change
                    return;
                case EditType.ChangeActiveBranch:
                    activeBranch = edit.activeBranchFinal;
                    return;
                case EditType.RemoveBranch:
                    branches.RemoveAt(edit.branchIndex);
                    activeBranch = edit.activeBranchFinal;
                    return;
                default:
                    throw new EditTypeNotSupportedException();
            }
        }

        public void revertEdit(EditHistoryItem edit)
        {
            switch (edit.type)
            {
                case EditType.AddBranch:
                    branches.RemoveAt(branches.Count - 1);
                    activeBranch = edit.activeBranchInitial;
                    return;
                case EditType.ChangeActiveBranch:
                    activeBranch = edit.activeBranchInitial;
                    return;
                case EditType.RemoveBranch:
                    branches.Insert(edit.branchIndex, edit.branchCopy.clone());
                    activeBranch = edit.branchIndex;
                    return;
                default:
                    throw new EditTypeNotSupportedException();
            }
        }

        public EditHistoryItem addBranchEdit(TASEditor parent)
        {
            // TODO name?
            return new EditHistoryItem
            {
                type = EditType.AddBranch,
                activeBranchInitial = activeBranch,
                branchCopy = Branch.fromFile("new branch", parent),
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
            if (branches.Count <= 1)
                return null;
            return new EditHistoryItem
            {
                type = EditType.RemoveBranch,
                branchIndex = activeBranch,
                activeBranchFinal = (activeBranch > 0) ? activeBranch - 1 : 0,
                branchCopy = branches[activeBranch].clone(),
            };
        }
    }

    class Branch : IEditable
    {
        private string name;
        List<IBranchSection> nodes = new List<IBranchSection>();

        private Branch() {}

        public Branch(Branch src)
        {
            name = src.name;
            nodes = new List<IBranchSection>();
            foreach (IBranchSection node in src.nodes)
                nodes.Add(node.clone());
        }

        public Branch clone()
        {
            return new Branch(this);
        }

        private static string removeSingleNewline(string text)
        {
            if (text.EndsWith('\n'))
                text = text.Substring(0, text.Length - 1);
            return text;
        }

        private static BranchGroup _makeBranchNode(string firstBranch, ref string text, TASEditor parent)
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
                    branch.nodes.Add(new TASEditorSection(inputs, parent));
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
                        return new BranchGroup(branches);
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
            BranchGroup node = _makeBranchNode("", ref text, parent);
            if (text.Length > 0)
                throw new FormatException("Unmatched >endbranch");
            return node.branches[0];
        }

        public string toFile()
        {
            string contents = "";
            foreach (IEditable node in nodes)
            {
                if (node is TASEditorSection)
                    contents += "\n" + ((TASEditorSection)node).getText();
                else
                {
                    List<Branch> branches = ((BranchGroup)node).branches;
                    for (int i = 0; i < branches.Count; i++)
                    {
                        if (i == 0)
                            contents += "\n>startbranch " + branches[0].name;
                        else
                            contents += "\n>branch " + branches[0].name;
                        contents += "\n" + branches[i].toFile();
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
            foreach (IEditable node in nodes)
            {
                if (node is TASEditorSection)
                    components.Add(((TASEditorSection)node).getComponent());
                else
                {
                    BranchGroup branchGroup = (BranchGroup)node;
                    branchGroup.updateHeader();
                    // TODO better UI style
                    components.Add(new Separator());
                    components.Add(branchGroup.headerComponent);
                    components.AddRange(branchGroup.branches[branchGroup.activeBranch].getComponents());
                    components.Add(new Separator());
                }
            }
            return components;
        }

        public string getText()
        {
            string text = "";
            for (int i = 0; i < nodes.Count; i++)
            {
                if (i > 0)
                    text += "\n";
                text += nodes[i].getText();
            }
            return text;
        }

        private List<int> _getEditable(IInputElement element, ref IEditable target)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] is TASEditorSection)
                {
                    if (((TASEditorSection)nodes[i]).getComponent().TextArea == element)
                    {
                        target = nodes[i];
                        return new List<int> { i };
                    }
                }
                else
                {
                    List<Branch> branches = ((BranchGroup)nodes[i]).branches;
                    for (int j = 0; j < branches.Count; j++)
                    {
                        List<int> id = branches[j]._getEditable(element, ref target);
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

        public List<int> findEditTargetID(IEditable target)
        {
            if (this == target)
                return new List<int>();
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] == target)
                    return new List<int> { i };
                else if (nodes[i] is BranchGroup)
                {
                    List<Branch> branches = ((BranchGroup)nodes[i]).branches;
                    for (int j = 0; j < branches.Count; j++)
                    {
                        List<int> id = branches[j].findEditTargetID(target);
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
                    if (target is BranchGroup)
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
                BranchGroup branchGroup = (BranchGroup)branch.nodes[nodeIndex];
                branch = branchGroup.branches[branchIndex];
            }
            if (i == id.Count)
                return branch;
            else
                return branch.nodes[id[i]];
        }

        public EditHistoryItem renameBranchEdit(string newName)
        {
            return new EditHistoryItem
            {
                type = EditType.RenameBranch,
                branchNameInitial = name,
                branchNameFinal = newName,
            };
        }

        internal EditHistoryItem newBranchGroupEdit(int inputBlockIndex, TASEditor parent)
        {
            TASEditorSection inputs = (TASEditorSection)nodes[inputBlockIndex];
            string[] text = inputs.splitOutBranch();
            List<Branch> branches = new List<Branch>();
            branches.Add(fromFile(text[1], parent));
            return new EditHistoryItem
            {
                type = EditType.NewBranchGroup,
                nodeIndex = inputBlockIndex,
                preText = text[0],
                branchGroupCopy = new BranchGroup(branches),
                postText = text[2],
                parent = parent,
            };
        }

        internal EditHistoryItem deleteBranchGroupEdit(int branchGroupIndex, TASEditor parent)
        {
            return new EditHistoryItem
            {
                type = EditType.DeleteBranchGroup,
                nodeIndex = branchGroupIndex - 1,
                preText = ((TASEditorSection)nodes[branchGroupIndex - 1]).getText(),
                branchGroupCopy = (BranchGroup)nodes[branchGroupIndex].clone(),
                postText = ((TASEditorSection)nodes[branchGroupIndex + 1]).getText(),
                parent = parent,
            };
        }

        public void performEdit(EditHistoryItem edit)
        {
            switch (edit.type)
            {
                case EditType.RenameBranch:
                    name = edit.branchNameFinal;
                    break;
                case EditType.NewBranchGroup:
                    // TODO any time things are deleted, the focussed element should change
                    nodes.RemoveAt(edit.nodeIndex);
                    nodes.InsertRange(edit.nodeIndex, new IBranchSection[] {
                        new TASEditorSection(edit.preText, edit.parent),
                        edit.branchGroupCopy,
                        new TASEditorSection(edit.postText, edit.parent),
                    });
                    break;
                case EditType.DeleteBranchGroup:
                    nodes.RemoveRange(edit.nodeIndex, 3);
                    string text = edit.preText + "\n" + edit.postText;
                    nodes.Insert(edit.nodeIndex, new TASEditorSection(text, edit.parent));
                    break;
                default:
                    throw new EditTypeNotSupportedException();
            }
        }

        public void revertEdit(EditHistoryItem edit)
        {
            switch (edit.type)
            {
                case EditType.RenameBranch:
                    name = edit.branchNameInitial;
                    break;
                case EditType.NewBranchGroup:
                    nodes.RemoveRange(edit.nodeIndex, 3);
                    string text = edit.preText + edit.branchGroupCopy.getText() + edit.postText;
                    nodes.Insert(edit.nodeIndex, new TASEditorSection(text, edit.parent));
                    break;
                case EditType.DeleteBranchGroup:
                    nodes.RemoveAt(edit.nodeIndex);
                    nodes.InsertRange(edit.nodeIndex, new IBranchSection[] {
                        new TASEditorSection(edit.preText, edit.parent),
                        edit.branchGroupCopy,
                        new TASEditorSection(edit.postText, edit.parent),
                    });
                    break;
                default:
                    throw new EditTypeNotSupportedException();
            }
        }

        public void showPlaybackFrame(int frame)
        {
            // TDOO
            ((TASEditorSection)nodes[0]).showPlaybackFrame(frame);
        }
    }
}
