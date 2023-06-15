using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml;

namespace BirdStudio
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

        public Branch getActiveBranch()
        {
            return branches[activeBranch];
        }

        public void updateHeader()
        {
            headerComponent.setBranch($"({activeBranch + 1}/{branches.Count})", branches[activeBranch].getName());
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

        public EditHistoryItem addBranchEdit()
        {
            // TODO name?
            return new AddBranchEdit
            {
                activeBranchInitial = activeBranch,
                branchCopy = Branch.fromText("unnamed branch", "", editor),
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
        public List<IBranchSection> nodes = new List<IBranchSection>();

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

        public static Branch fromText(string name, string text, TASEditor editor)
        {
            List<IBranchSection> nodes = new List<IBranchSection>();
            nodes.Add(new TASEditorSection(text, editor));
            return new Branch
            {
                name = name,
                nodes = nodes,
                editor = editor,
            };
        }

        public static Branch fromXml(XmlNode xml, TASEditor editor)
        {
            string branchName = Util.getXmlAttribute(xml, "name", "unnamed branch");
            Branch branch = new Branch { name = branchName, editor = editor };
            foreach (XmlNode node in xml.ChildNodes)
            {
                if (node.Name == "inputs")
                {
                    string inputs = node.InnerText;
                    inputs = Util.removeSandwichingNewlines(inputs);
                    branch.nodes.Add(new TASEditorSection(inputs, editor));
                }
                else if (node.Name == "branch")
                {
                    int activeBranch = Util.getXmlAttributeAsInt(node, "active", 0);
                    List<Branch> branches = new List<Branch>();
                    foreach (XmlNode x in node.ChildNodes)
                    {
                        if (x.Name != "b")
                            throw new FormatException();
                        branches.Add(fromXml(x, editor));
                    }
                    if (branches.Count == 0)
                        throw new FormatException();
                    branch.nodes.Add(new BranchGroup(editor, branches, activeBranch));
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
                    contents += $"<branch active=\"{((BranchGroup)node).activeBranch}\">";
                    foreach (Branch branch in ((BranchGroup)node).branches)
                        contents += $"<b name=\"{branch.name}\">{branch.toInnerXml()}</b>";
                    contents += "</branch>";
                }
            }
            return contents;
        }

        public string getName()
        {
            return name;
        }

        private Separator branchSeparator()
        {
            Separator s = new Separator();
            s.SetResourceReference(Control.BackgroundProperty, "BranchSeparator.Background");
            return s;
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
                    components.Add(branchSeparator());
                    components.Add(branchGroup.headerComponent);
                    components.AddRange(branchGroup.branches[branchGroup.activeBranch].getComponents());
                    if (node != nodes[nodes.Count - 1])
                        components.Add(branchSeparator());
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
                    BranchGroup branchGroupNode = (BranchGroup)nodes[i];
                    if (
                        element == branchGroupNode.headerComponent ||
                        element == branchGroupNode.headerComponent.nameDisplay ||
                        element == branchGroupNode.headerComponent.nameEdit
                    ) {
                        target = nodes[i];
                        return new List<int> { i };
                    }
                    List <Branch> branches = branchGroupNode.branches;
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
                    if (target is TASEditorSection)
                        return id;
                    return null;
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

        internal EditHistoryItem newBranchGroupEdit(int inputBlockIndex, NewBranchInfo split = null, string newBranchName = "unnamed branch")
        {
            if (split == null)
                split = ((TASEditorSection)nodes[inputBlockIndex]).splitOutBranch();
            Branch mainBranch = fromText("main branch", split.splitText, editor);
            if (split.bottomless && inputBlockIndex < nodes.Count - 1)
                mainBranch.nodes.AddRange(nodes.GetRange(inputBlockIndex + 1, nodes.Count - inputBlockIndex - 1));
            List<Branch> branches = new List<Branch>();
            branches.Add(mainBranch);
            branches.Add(fromText(newBranchName, split.newBranchText, editor));
            BranchGroup branchGroup = new BranchGroup(editor, branches);
            branchGroup.activeBranch = 1;
            int removeCount = (split.bottomless) ? nodes.Count - inputBlockIndex : 1;
            List<IBranchSection> removedSections = Util.getRangeClone(nodes, inputBlockIndex, removeCount);
            List<IBranchSection> insertedSections = new List<IBranchSection>();
            if (split.preText != null)
                insertedSections.Add(new TASEditorSection(split.preText, editor));
            insertedSections.Add(branchGroup);
            if (split.postText != null)
                insertedSections.Add(new TASEditorSection(split.postText, editor));
            return new RestructureBranchEdit
            {
                nodeIndex = inputBlockIndex,
                removedSections = removedSections.ToArray(),
                insertedSections = insertedSections.ToArray(),
            };
        }

        internal EditHistoryItem deleteBranchGroupEdit(int branchGroupIndex)
        {
            string replacementText = "";
            int deleteStart = branchGroupIndex;
            int deleteCount = 1;
            if (branchGroupIndex > 0 && nodes[branchGroupIndex - 1] is TASEditorSection)
            {
                deleteStart--;
                replacementText = ((TASEditorSection)nodes[branchGroupIndex - 1]).getText() + "\n" + replacementText;
                deleteCount++;
            }
            if (branchGroupIndex < nodes.Count - 1 && nodes[branchGroupIndex + 1] is TASEditorSection)
            {
                replacementText += "\n" + ((TASEditorSection)nodes[branchGroupIndex + 1]).getText();
                deleteCount++;
            }
            return new RestructureBranchEdit
            {
                nodeIndex = deleteStart,
                removedSections = Util.getRangeClone(nodes, deleteStart, deleteCount).ToArray(),
                insertedSections = new IBranchSection[]
                {
                    new TASEditorSection(replacementText, editor)
                },
            };
        }

        internal EditHistoryItem acceptBranchGroupEdit(int branchGroupIndex)
        {
            List<IBranchSection> replacementNodes = ((BranchGroup)nodes[branchGroupIndex]).getActiveBranch().nodes;
            IBranchSection[] insertedSections = Util.getRangeClone(replacementNodes, 0, replacementNodes.Count).ToArray();
            int deleteStart = branchGroupIndex;
            int deleteCount = 1;
            if (
                branchGroupIndex > 0 && nodes[branchGroupIndex - 1] is TASEditorSection &&
                insertedSections.First() is TASEditorSection
            )
            {
                deleteStart--;
                deleteCount++;
                string text = ((TASEditorSection)nodes[branchGroupIndex - 1]).getText();
                text += "\n" + insertedSections.First().getText();
                insertedSections[0] = new TASEditorSection(text, editor);
            }
            if (
                branchGroupIndex < nodes.Count - 1 && nodes[branchGroupIndex + 1] is TASEditorSection &&
                insertedSections.Last() is TASEditorSection
            )
            {
                deleteCount++;
                string text = insertedSections.Last().getText();
                text += "\n" + ((TASEditorSection)nodes[branchGroupIndex + 1]).getText();
                insertedSections[insertedSections.Length - 1] = new TASEditorSection(text, editor);
            }
            return new RestructureBranchEdit
            {
                nodeIndex = deleteStart,
                removedSections = Util.getRangeClone(nodes, deleteStart, deleteCount).ToArray(),
                insertedSections = insertedSections,
            };
        }

        internal bool updateInputs(List<TASInputLine> newInputs, bool force)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                bool lastChance = force && (i == nodes.Count - 1);
                if (nodes[i] is TASEditorSection)
                {
                    TASEditorSection inputNode = (TASEditorSection)nodes[i];
                    NewBranchInfo newBranchInfo = null;
                    bool done = inputNode.updateInputs(newInputs, lastChance, ref newBranchInfo);
                    if (done && newBranchInfo != null)
                    {
                        editor.requestEdit(this, newBranchGroupEdit(i, newBranchInfo, "recorded inputs"));
                        return true;
                    }
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
            else if (edit is RestructureBranchEdit)
            {
                RestructureBranchEdit restructureEdit = (RestructureBranchEdit)edit;
                // TODO any time things are deleted, the focussed element should change
                nodes.RemoveRange(restructureEdit.nodeIndex, restructureEdit.removedSections.Length);
                for (int i = 0; i < restructureEdit.insertedSections.Length; i++)
                {
                    IBranchSection section = restructureEdit.insertedSections[i];
                    nodes.Insert(restructureEdit.nodeIndex + i, section.clone());
                }
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
            else if (edit is RestructureBranchEdit)
            {
                RestructureBranchEdit restructureEdit = (RestructureBranchEdit)edit;
                nodes.RemoveRange(restructureEdit.nodeIndex, restructureEdit.insertedSections.Length);
                for (int i = 0; i < restructureEdit.removedSections.Length; i++)
                {
                    IBranchSection section = restructureEdit.removedSections[i];
                    nodes.Insert(restructureEdit.nodeIndex + i, section.clone());
                }
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

        /// <summary>
        /// If branch contains block, then count is incremented by the number
        /// of frames before that block, and true is returned. If branch does
        /// not contain block, then count is incremented by the total number
        /// of frames in the block, and false is returned.
        /// </summary>
        private bool getStartFrameOfBlock(TASEditorSection block, ref int count)
        {
            foreach (IBranchSection node in nodes)
            {
                if (node == block)
                    return true;
                else if (node is TASEditorSection)
                    count += ((TASEditorSection)node).getInputsData().totalFrames();
                else if (node is BranchGroup)
                {
                    Branch activeBranch = ((BranchGroup)node).getActiveBranch();
                    bool found = activeBranch.getStartFrameOfBlock(block, ref count);
                    if (found)
                        return true;
                }
            }
            return false;
        }

        public int getStartFrameOfBlock(TASEditorSection block)
        {
            int startFrame = 0;
            bool found = getStartFrameOfBlock(block, ref startFrame);
            if (found)
                return startFrame;
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
                // Separator thinks that it has height 0 for some reason...
                if (component is Separator)
                    y += 5;
            }
            return null;
        }
    }
}
