using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml;

namespace BirdStudio
{
    public class Branch : IEditable
    {
        private Editor editor;
        public BranchGroup parent;
        private string name;
        public List<IBranchSection> nodes = new List<IBranchSection>();

        private Branch() {}

        public Branch(Branch src)
        {
            name = src.name;
            nodes = new List<IBranchSection>();
            foreach (IBranchSection node in src.nodes)
                _insertNode(node.clone());
            editor = src.editor;
        }

        public Branch clone()
        {
            return new Branch(this);
        }

        private void _insertNode(IBranchSection node, int index=-1)
        {
            if (node is InputsBlock)
                ((InputsBlock)node).parent = this;
            if (node is BranchGroup)
                ((BranchGroup)node).parent = this;
            if (index == -1)
                nodes.Add(node);
            else
                nodes.Insert(index, node);
        }

        public static Branch fromText(string name, string text, Editor editor)
        {
            Branch branch = new Branch
            {
                name = name,
                nodes = new List<IBranchSection>(),
                editor = editor,
            };
            branch._insertNode(new InputsBlock(text, editor));
            return branch;
        }

        public static Branch fromXml(XmlNode xml, Editor editor)
        {
            string branchName = Util.getXmlAttribute(xml, "name", "unnamed branch");
            Branch branch = new Branch { name = branchName, editor = editor };
            foreach (XmlNode node in xml.ChildNodes)
            {
                if (node.Name == "inputs")
                {
                    string inputs = node.InnerText;
                    inputs = Util.removeSandwichingNewlines(inputs);
                    branch._insertNode(new InputsBlock(inputs, editor));
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
                    branch._insertNode(new BranchGroup(editor, branches, activeBranch));
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
                if (node is InputsBlock)
                    contents += "<inputs>\n" + ((InputsBlock)node).getText() + "\n</inputs>";
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
                if (node is InputsBlock)
                    components.Add((InputsBlock)node);
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
                if (nodes[i] is InputsBlock)
                {
                    if (((InputsBlock)nodes[i]).TextArea == element)
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
                    if (target is InputsBlock)
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

        public RestructureBranchEdit newBranchGroupEdit(int inputBlockIndex, NewBranchInfo split = null, string newBranchName = "unnamed branch")
        {
            if (split == null)
                split = ((InputsBlock)nodes[inputBlockIndex]).splitOutBranch();
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
                insertedSections.Add(new InputsBlock(split.preText, editor));
            insertedSections.Add(branchGroup);
            if (split.postText != null)
                insertedSections.Add(new InputsBlock(split.postText, editor));
            return new RestructureBranchEdit
            {
                nodeIndex = inputBlockIndex,
                removedSections = removedSections.ToArray(),
                insertedSections = insertedSections.ToArray(),
            };
        }

        public RestructureBranchEdit deleteBranchGroupEdit(int branchGroupIndex)
        {
            string replacementText = "";
            int deleteStart = branchGroupIndex;
            int deleteCount = 1;
            if (branchGroupIndex > 0 && nodes[branchGroupIndex - 1] is InputsBlock)
            {
                deleteStart--;
                replacementText = ((InputsBlock)nodes[branchGroupIndex - 1]).getText() + "\n" + replacementText;
                deleteCount++;
            }
            if (branchGroupIndex < nodes.Count - 1 && nodes[branchGroupIndex + 1] is InputsBlock)
            {
                replacementText += "\n" + ((InputsBlock)nodes[branchGroupIndex + 1]).getText();
                deleteCount++;
            }
            return new RestructureBranchEdit
            {
                nodeIndex = deleteStart,
                removedSections = Util.getRangeClone(nodes, deleteStart, deleteCount).ToArray(),
                insertedSections = new IBranchSection[]
                {
                    new InputsBlock(replacementText, editor)
                },
            };
        }

        public RestructureBranchEdit acceptBranchGroupEdit(int branchGroupIndex)
        {
            List<IBranchSection> replacementNodes = ((BranchGroup)nodes[branchGroupIndex]).getActiveBranch().nodes;
            IBranchSection[] insertedSections = Util.getRangeClone(replacementNodes, 0, replacementNodes.Count).ToArray();
            int deleteStart = branchGroupIndex;
            int deleteCount = 1;
            if (
                branchGroupIndex > 0 && nodes[branchGroupIndex - 1] is InputsBlock &&
                insertedSections.First() is InputsBlock
            )
            {
                deleteStart--;
                deleteCount++;
                string text = ((InputsBlock)nodes[branchGroupIndex - 1]).getText();
                text += "\n" + insertedSections.First().getText();
                insertedSections[0] = new InputsBlock(text, editor);
            }
            if (
                branchGroupIndex < nodes.Count - 1 && nodes[branchGroupIndex + 1] is InputsBlock &&
                insertedSections.Last() is InputsBlock
            )
            {
                deleteCount++;
                string text = insertedSections.Last().getText();
                text += "\n" + ((InputsBlock)nodes[branchGroupIndex + 1]).getText();
                insertedSections[insertedSections.Length - 1] = new InputsBlock(text, editor);
            }
            return new RestructureBranchEdit
            {
                nodeIndex = deleteStart,
                removedSections = Util.getRangeClone(nodes, deleteStart, deleteCount).ToArray(),
                insertedSections = insertedSections,
            };
        }

        internal bool updateInputs(List<InputsLine> newInputs, bool force)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                bool lastChance = force && (i == nodes.Count - 1);
                if (nodes[i] is InputsBlock)
                {
                    InputsBlock inputNode = (InputsBlock)nodes[i];
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
                    _insertNode(section.clone(), restructureEdit.nodeIndex + i);
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
                    _insertNode(section.clone(), restructureEdit.nodeIndex + i);
                }
            }
            else
                throw new EditTypeNotSupportedException();
        }

        public int listBlocksByStartFrame(List<FrameAndBlock> blocks, int startFrame = 0)
        {
            foreach (IBranchSection node in nodes)
            {
                if (node is InputsBlock)
                {
                    InputsBlock inputNode = (InputsBlock)node;
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
        private bool getStartFrameOfBlock(InputsBlock block, ref int count)
        {
            foreach (IBranchSection node in nodes)
            {
                if (node == block)
                    return true;
                else if (node is InputsBlock)
                    count += ((InputsBlock)node).getInputsData().totalFrames();
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

        public int getStartFrameOfBlock(InputsBlock block)
        {
            int startFrame = 0;
            bool found = getStartFrameOfBlock(block, ref startFrame);
            if (found)
                return startFrame;
            return -1;
        }

        public TopBottom activeLineYPos(InputsBlock activeBlock)
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
