using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Xml;

namespace BirdStudioRefactor
{
    public enum EditableTargetType
    {
        Any,
        InputBlock,
        Branch,
        BranchGroup,
    }

    public interface IBranchSection : IEditable
    {
        public string getText();
        public IBranchSection clone();
    }

    public class BranchGroup : IBranchSection
    {
        private TASEditor editor;
        public BranchGroupHeader headerComponent;
        public List<Branch> branches;
        public int activeBranch;

        public BranchGroup(TASEditor editor, List<Branch> branches, int activeBranch = 0)
        {
            this.editor = editor;
            this.branches = branches;
            this.activeBranch = activeBranch;
            headerComponent = new BranchGroupHeader(this);
            updateHeader();
        }

        public IBranchSection clone()
        {
            List<Branch> newBranches = new List<Branch>();
            foreach (Branch branch in branches)
                newBranches.Add(branch.clone());
            return new BranchGroup(editor, newBranches, activeBranch);
        }

        public void updateHeader()
        {
            headerComponent.setBranch(activeBranch + 1, branches[activeBranch].getName());
        }

        public void renameBranch()
        {
            headerComponent.beginRename();
        }

        public void requestBranchNameChange(string newName)
        {
            EditHistoryItem edit = new RenameBranchEdit
            {
                branchNameInitial = branches[activeBranch].getName(),
                branchNameFinal = newName,
            };
            editor.requestEdit(branches[activeBranch], edit);
        }

        public string getText()
        {
            return branches[activeBranch].getText();
        }

        public void performEdit(EditHistoryItem edit)
        {
            if (edit is AddBranchEdit)
            {
                AddBranchEdit addEdit = (AddBranchEdit)edit;
                branches.Add(addEdit.branchCopy.clone());
                activeBranch = branches.Count - 1;
                // TODO any time the active branch changes, the focussed element should also change
            }
            else if (edit is ChangeActiveBranchEdit)
            {
                ChangeActiveBranchEdit changeEdit = (ChangeActiveBranchEdit)edit;
                activeBranch = changeEdit.activeBranchFinal;
            }
            else if (edit is RemoveBranchEdit)
            {
                RemoveBranchEdit removeEdit = (RemoveBranchEdit)edit;
                branches.RemoveAt(removeEdit.branchIndex);
                activeBranch = removeEdit.activeBranchFinal;
            }
            else
                throw new EditTypeNotSupportedException();
        }

        public void revertEdit(EditHistoryItem edit)
        {
            if (edit is AddBranchEdit)
            {
                AddBranchEdit addEdit = (AddBranchEdit)edit;
                branches.RemoveAt(branches.Count - 1);
                activeBranch = addEdit.activeBranchInitial;
            }
            else if (edit is ChangeActiveBranchEdit)
            {
                ChangeActiveBranchEdit changeEdit = (ChangeActiveBranchEdit)edit;
                activeBranch = changeEdit.activeBranchInitial;
            }
            else if (edit is RemoveBranchEdit)
            {
                RemoveBranchEdit removeEdit = (RemoveBranchEdit)edit;
                branches.Insert(removeEdit.branchIndex, removeEdit.branchCopy.clone());
                activeBranch = removeEdit.branchIndex;
            }
            else
                throw new EditTypeNotSupportedException();
        }

        public EditHistoryItem addBranchEdit(TASEditor parent)
        {
            // TODO name?
            return new AddBranchEdit
            {
                activeBranchInitial = activeBranch,
                branchCopy = Branch.fromText("unnamed branch", "", parent),
            };
        }

        public bool canChangeBranch(int offset)
        {
            return
                (offset < 0 && activeBranch > 0) ||
                (offset > 0 && activeBranch < branches.Count - 1);
        }

        public EditHistoryItem changeBranchEdit(int offset)
        {
            int newActiveBranch = activeBranch + offset;
            if (newActiveBranch < 0)
                newActiveBranch = 0;
            if (newActiveBranch >= branches.Count)
                newActiveBranch = branches.Count - 1;
            if (newActiveBranch == activeBranch)
                return null;
            return new ChangeActiveBranchEdit
            {
                activeBranchInitial = activeBranch,
                activeBranchFinal = newActiveBranch,
            };
        }

        public EditHistoryItem removeBranchEdit()
        {
            if (branches.Count <= 1)
                return null;
            return new RemoveBranchEdit
            {
                branchIndex = activeBranch,
                activeBranchFinal = (activeBranch > 0) ? activeBranch - 1 : 0,
                branchCopy = branches[activeBranch].clone(),
            };
        }
    }

    public class Branch : IEditable
    {
        private TASEditor editor;
        private string name;
        List<IBranchSection> nodes = new List<IBranchSection>();

        private Branch() {}

        public Branch(Branch src)
        {
            name = src.name;
            nodes = new List<IBranchSection>();
            foreach (IBranchSection node in src.nodes)
                nodes.Add(node.clone());
            editor = src.editor;
        }

        public Branch clone()
        {
            return new Branch(this);
        }

        public static Branch fromText(string name, string text, TASEditor parent)
        {
            List<IBranchSection> nodes = new List<IBranchSection>();
            nodes.Add(new TASEditorSection(text, parent));
            return new Branch
            {
                name = name,
                nodes = nodes,
                editor = parent,
            };
        }

        public static Branch fromXml(XmlNode xml, TASEditor parent)
        {
            XmlNode nameNode = xml.Attributes.GetNamedItem("name");
            string branchName = (nameNode != null) ? nameNode.InnerText : "unnamed branch";
            Branch branch = new Branch { name = branchName, editor = parent };
            foreach (XmlNode node in xml.ChildNodes)
            {
                if (node.Name == "inputs")
                {
                    string inputs = node.InnerText;
                    inputs = Util.removeSandwichingNewlines(inputs);
                    branch.nodes.Add(new TASEditorSection(inputs, parent));
                }
                else if (node.Name == "branch")
                {
                    List<Branch> branches = new List<Branch>();
                    foreach (XmlNode x in node.ChildNodes)
                    {
                        if (x.Name != "b")
                            throw new FormatException();
                        branches.Add(fromXml(x, parent));
                    }
                    if (branches.Count == 0)
                        throw new FormatException();
                    branch.nodes.Add(new BranchGroup(parent, branches));
                }
                else
                    throw new FormatException();
            }
            return branch;
        }

        public string toInnerXml()
        {
            string contents = "";
            foreach (IEditable node in nodes)
            {
                if (node is TASEditorSection)
                    contents += "<inputs>\n" + ((TASEditorSection)node).getText() + "\n</inputs>";
                else
                {
                    contents += "<branch>";
                    foreach (Branch branch in ((BranchGroup)node).branches)
                        contents += "<b name=\"" + branch.name + "\">" + branch.toInnerXml() + "</b>";
                    contents += "</branch>";
                }
            }
            return contents;
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
                    components.Add((TASEditorSection)node);
                else
                {
                    BranchGroup branchGroup = (BranchGroup)node;
                    branchGroup.updateHeader();
                    // TODO better UI style
                    components.Add(new Separator());
                    components.Add(branchGroup.headerComponent);
                    components.AddRange(branchGroup.branches[branchGroup.activeBranch].getComponents());
                    if (node != nodes[nodes.Count - 1])
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
                    if (((TASEditorSection)nodes[i]).TextArea == element)
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

        internal EditHistoryItem newBranchGroupEdit(int inputBlockIndex)
        {
            TASEditorSection inputs = (TASEditorSection)nodes[inputBlockIndex];
            string[] text = inputs.splitOutBranch();
            List<Branch> branches = new List<Branch>();
            branches.Add(fromText("unnamed branch", "", editor));
            branches.Add(fromText("main branch", text[1], editor));
            return new NewBranchGroupEdit
            {
                nodeIndex = inputBlockIndex,
                initialText = inputs.getText(),
                preText = text[0],
                branchGroupCopy = new BranchGroup(editor, branches),
                postText = text[2],
                parent = editor,
            };
        }

        internal EditHistoryItem deleteBranchGroupEdit(int branchGroupIndex)
        {
            DeleteBranchGroupEdit edit = new DeleteBranchGroupEdit
            {
                nodeIndex = branchGroupIndex,
                branchGroupCopy = (BranchGroup)nodes[branchGroupIndex].clone(),
                replacementText = "",
                parent = editor,
            };
            if (branchGroupIndex > 0 && nodes[branchGroupIndex - 1] is TASEditorSection)
            {
                edit.nodeIndex = branchGroupIndex - 1;
                edit.preText = ((TASEditorSection)nodes[branchGroupIndex - 1]).getText();
                edit.replacementText = edit.preText;
            }
            if (branchGroupIndex < nodes.Count - 1 && nodes[branchGroupIndex + 1] is TASEditorSection)
            {
                edit.postText = ((TASEditorSection)nodes[branchGroupIndex + 1]).getText();
                if (edit.preText == null)
                    edit.replacementText = edit.postText;
                else
                    edit.replacementText += "\n" + edit.postText;
            }
            return edit;
        }

        internal EditHistoryItem acceptBranchGroupEdit(int branchGroupIndex)
        {
            DeleteBranchGroupEdit edit = new DeleteBranchGroupEdit
            {
                nodeIndex = branchGroupIndex,
                branchGroupCopy = (BranchGroup)nodes[branchGroupIndex].clone(),
                replacementText = nodes[branchGroupIndex].getText(),
                parent = editor,
            };
            if (branchGroupIndex > 0 && nodes[branchGroupIndex - 1] is TASEditorSection)
            {
                edit.nodeIndex = branchGroupIndex - 1;
                edit.preText = ((TASEditorSection)nodes[branchGroupIndex - 1]).getText();
                edit.replacementText = edit.preText + "\n" + edit.replacementText;
            }
            if (branchGroupIndex < nodes.Count - 1 && nodes[branchGroupIndex + 1] is TASEditorSection)
            {
                edit.postText = ((TASEditorSection)nodes[branchGroupIndex + 1]).getText();
                edit.replacementText += "\n" + edit.postText;
            }
            return edit;
        }

        internal bool updateInputs(List<TASInputLine> newInputs, bool force)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                bool lastChance = force && (i == nodes.Count - 1);
                if (nodes[i] is TASEditorSection)
                {
                    TASEditorSection inputNode = (TASEditorSection)nodes[i];
                    bool done = inputNode.updateInputs(newInputs, lastChance, this, i);
                    if (done)
                        return true;
                }
                else
                {
                    BranchGroup branchNode = (BranchGroup)nodes[i];
                    Branch activeBranch = branchNode.branches[branchNode.activeBranch];
                    bool done = activeBranch.updateInputs(newInputs, lastChance);
                    if (done)
                        return true;
                }
            }
            return false;
        }

        public void performEdit(EditHistoryItem edit)
        {
            if (edit is RenameBranchEdit)
            {
                RenameBranchEdit renameEdit = (RenameBranchEdit)edit;
                name = renameEdit.branchNameFinal;
            }
            else if (edit is NewBranchGroupEdit)
            {
                NewBranchGroupEdit newEdit = (NewBranchGroupEdit)edit;
                // TODO any time things are deleted, the focussed element should change
                nodes.RemoveAt(newEdit.nodeIndex);
                if (newEdit.postText != null)
                    nodes.Insert(newEdit.nodeIndex, new TASEditorSection(newEdit.postText, newEdit.parent));
                nodes.Insert(newEdit.nodeIndex, newEdit.branchGroupCopy.clone());
                if (newEdit.preText != null)
                    nodes.Insert(newEdit.nodeIndex, new TASEditorSection(newEdit.preText, newEdit.parent));
            }
            else if (edit is DeleteBranchGroupEdit)
            {
                DeleteBranchGroupEdit deleteEdit = (DeleteBranchGroupEdit)edit;
                int deleteCount = (deleteEdit.preText == null ? 0 : 1) + 1 + (deleteEdit.postText == null ? 0 : 1);
                nodes.RemoveRange(deleteEdit.nodeIndex, deleteCount);
                nodes.Insert(deleteEdit.nodeIndex, new TASEditorSection(deleteEdit.replacementText, deleteEdit.parent));
            }
            else
                throw new EditTypeNotSupportedException();
        }

        public void revertEdit(EditHistoryItem edit)
        {
            if (edit is RenameBranchEdit)
            {
                RenameBranchEdit renameEdit = (RenameBranchEdit)edit;
                name = renameEdit.branchNameInitial;
            }
            else if (edit is NewBranchGroupEdit)
            {
                NewBranchGroupEdit newEdit = (NewBranchGroupEdit)edit;
                int deleteCount = (newEdit.preText != null ? 1 : 0) + 1 + (newEdit.postText != null ? 1 : 0);
                nodes.RemoveRange(newEdit.nodeIndex, deleteCount);
                nodes.Insert(newEdit.nodeIndex, new TASEditorSection(newEdit.initialText, newEdit.parent));
            }
            else if (edit is DeleteBranchGroupEdit)
            {
                DeleteBranchGroupEdit deleteEdit = (DeleteBranchGroupEdit)edit;
                nodes.RemoveAt(deleteEdit.nodeIndex);
                if (deleteEdit.postText != null)
                    nodes.Insert(deleteEdit.nodeIndex, new TASEditorSection(deleteEdit.postText, deleteEdit.parent));
                nodes.Insert(deleteEdit.nodeIndex, deleteEdit.branchGroupCopy.clone());
                if (deleteEdit.preText != null)
                    nodes.Insert(deleteEdit.nodeIndex, new TASEditorSection(deleteEdit.preText, deleteEdit.parent));
            }
            else
                throw new EditTypeNotSupportedException();
        }

        public int listBlocksByStartFrame(List<FrameAndBlock> blocks, int startFrame = 0)
        {
            foreach (IBranchSection node in nodes)
            {
                if (node is TASEditorSection)
                {
                    TASEditorSection inputNode = (TASEditorSection)node;
                    blocks.Add(new FrameAndBlock
                    {
                        frame = startFrame,
                        block = inputNode,
                    });
                    startFrame += inputNode.getInputsData().totalFrames();
                }
                else
                {
                    BranchGroup branchNode = (BranchGroup)node;
                    Branch activeBranch = branchNode.branches[branchNode.activeBranch];
                    startFrame = activeBranch.listBlocksByStartFrame(blocks, startFrame);
                }
            }
            return startFrame;
        }

        public int getStartFrameOfBlock(TASEditorSection block)
        {
            int count = 0;
            foreach (IBranchSection node in nodes)
            {
                if (node == block)
                    return count;
                else if (node is TASEditorSection)
                    count += ((TASEditorSection)node).getInputsData().totalFrames();
                else if (node is BranchGroup)
                {
                    foreach (Branch branch in ((BranchGroup)node).branches)
                    {
                        int countWithinBranch = branch.getStartFrameOfBlock(block);
                        if (countWithinBranch != -1)
                            return count + countWithinBranch;
                    }
                }
            }
            return -1;
        }

        public TopBottom activeLineYPos(TASEditorSection activeBlock)
        {
            double y = 0;
            foreach (UIElement component in getComponents())
            {
                if (component == activeBlock)
                {
                    component.UpdateLayout();
                    int caretOffset = activeBlock.CaretOffset;
                    var caretLine = activeBlock.Document.GetLineByOffset(caretOffset);
                    int caretColumn = caretOffset - caretLine.Offset + 1; // +1 because avalonEdit likes to 1-index
                    ICSharpCode.AvalonEdit.TextViewPosition caretPos = new ICSharpCode.AvalonEdit.TextViewPosition(caretLine.LineNumber, caretColumn);
                    Point top = activeBlock.TextArea.TextView.GetVisualPosition(caretPos, ICSharpCode.AvalonEdit.Rendering.VisualYPosition.LineTop);
                    Point bottom = activeBlock.TextArea.TextView.GetVisualPosition(caretPos, ICSharpCode.AvalonEdit.Rendering.VisualYPosition.LineBottom);
                    return new TopBottom {
                        top = y + top.Y,
                        bottom = y + bottom.Y
                    };
                }
                y += component.RenderSize.Height;
            }
            return null;
        }
    }
}
