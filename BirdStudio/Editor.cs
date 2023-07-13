using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System;
using System.Xml;
using System.Linq;

namespace BirdStudio
{
    public class Editor : FileManager
    {
        private const string DEFAULT_FILE_TEXT = "<tas stage=\"Twin Tree Village\"><inputs>  29\n</inputs></tas>";

        private EditorHeader header;
        private StackPanel panel;
        private ScrollViewer scrollViewer;
        private Branch masterBranch;
        private List<EditHistoryItem> editHistory = new List<EditHistoryItem>();
        private int editHistoryLocation = 0;
        private bool tasEditedSinceLastWatch = true;
        private InputsBlock highlightedSection;
        private int playbackFrame = -1;
        private List<FrameAndBlock> blocksByStartFrame;

        public Editor(MainWindow window, StackPanel panel, ScrollViewer scrollViewer, string initialFile = null) : base(window)
        {
            this.panel = panel;
            this.scrollViewer = scrollViewer;
            if (initialFile == null)
                neww();
            else
                open(initialFile);
        }

        internal void _mergeEdits()
        {
            if (editHistory.Count < 2)
                return;
            int end = editHistory.Count - 1;
            if (!(editHistory[end] is ModifyTextEdit && editHistory[end - 1] is ModifyTextEdit))
                return;
            if (!editHistory[end].targetID.SequenceEqual(editHistory[end - 1].targetID))
                return;
            ModifyTextEdit before = (ModifyTextEdit)editHistory[end - 1];
            ModifyTextEdit after = (ModifyTextEdit)editHistory[end];
            if (after.pos == before.pos + before.textInserted.Length)
            {
                before.textRemoved += after.textRemoved;
                before.textInserted += after.textInserted;
                before.cursorPosFinal = after.cursorPosFinal;
                editHistory.RemoveAt(end);
                editHistoryLocation--;
            }
            else if (before.pos == after.pos + after.textRemoved.Length)
            {
                after.textRemoved += before.textRemoved;
                after.textInserted += before.textInserted;
                after.cursorPosInitial = before.cursorPosInitial;
                editHistory.RemoveAt(end - 1);
                editHistoryLocation--;
            }
        }

        internal void _moveFocus(List<int> focusID)
        {
            focusID = new List<int>(focusID);
            // If focus target is branch, back out to branch group
            // TODO What to do if focus target is the master branch?
            if (focusID.Count % 2 == 0 && focusID.Count > 0)
                focusID.RemoveAt(focusID.Count - 1);
            IEditable focusTarget = masterBranch.getEditable(focusID);
            if (focusTarget is BranchGroup)
                ((BranchGroup)focusTarget).headerComponent.Focus();
            else if (focusTarget is InputsBlock)
            {
                InputsBlock focusTargetS = (InputsBlock)focusTarget;
                if (focusTargetS.IsLoaded)
                    focusTargetS.TextArea.Focus();
                else
                    // Workaround because it isn't loaded yet (unlike the
                    // branch group for some reason..?)
                    ((InputsBlock)focusTarget).focusOnLoad = true;
            }
        }

        internal void _reloadComponents()
        {
            panel.Children.Clear();
            panel.Children.Add(header);
            foreach (UIElement component in masterBranch.getComponents())
                panel.Children.Add(component);
            blocksByStartFrame = null;
            bringActiveLineToFocus();
            showPlaybackFrame(playbackFrame);
        }

        public void editPerformed(IEditable target, EditHistoryItem edit)
        {
            edit.targetID = masterBranch.findEditTargetID(target);
            IInputElement focusedElement = FocusManager.GetFocusedElement(panel);
            edit.focusInitial = masterBranch.findEditTargetID(focusedElement, EditableTargetType.Any);
            if (editHistoryLocation < editHistory.Count)
                editHistory.RemoveRange(editHistoryLocation, editHistory.Count - editHistoryLocation);
            editHistory.Add(edit);
            editHistoryLocation++;
            _mergeEdits();
            fileChanged();
            blocksByStartFrame = null;
            if (!tasEditedSinceLastWatch && edit is ModifyTextEdit)
            {
                header.incrementRerecords();
                tasEditedSinceLastWatch = true;
            }
            bringActiveLineToFocus();
            showPlaybackFrame(playbackFrame);
        }

        public void requestEdit(IEditable target, EditHistoryItem edit)
        {
            target.performEdit(edit);
            editPerformed(target, edit);
            if (!(edit is ModifyTextEdit))
                _reloadComponents();
            blocksByStartFrame = null;
            _moveFocus(edit.focusFinal == null ? edit.targetID : edit.focusFinal);
        }

        public bool canUndo()
        {
            return editHistoryLocation > 0;
        }

        public void undo()
        {
            if (!canUndo())
                return;
            EditHistoryItem edit = editHistory[editHistoryLocation - 1];
            IEditable target = masterBranch.getEditable(edit.targetID);
            target.revertEdit(edit);
            editHistoryLocation--;
            if (!(edit is ModifyTextEdit))
                _reloadComponents();
            blocksByStartFrame = null;
            _moveFocus(edit.focusInitial == null ? edit.targetID : edit.focusInitial);
            fileChanged();
            showPlaybackFrame(playbackFrame);
        }

        public bool canRedo()
        {
            return editHistoryLocation < editHistory.Count;
        }

        public void redo()
        {
            if (!canRedo())
                return;
            EditHistoryItem edit = editHistory[editHistoryLocation];
            IEditable target = masterBranch.getEditable(edit.targetID);
            target.performEdit(edit);
            editHistoryLocation++;
            if (!(edit is ModifyTextEdit))
                _reloadComponents();
            blocksByStartFrame = null;
            _moveFocus(edit.focusFinal == null ? edit.targetID : edit.focusFinal);
            fileChanged();
            showPlaybackFrame(playbackFrame);
        }

        private void _clearUndoStack()
        {
            editHistory = new List<EditHistoryItem>();
            editHistoryLocation = 0;
        }

        public void newBranch()
        {
            IInputElement focusedElement = FocusManager.GetFocusedElement(panel);
            List<int> id = masterBranch.findEditTargetID(focusedElement, EditableTargetType.InputBlock);
            if (id == null)
                return;
            int inputBlockIndex = id[id.Count - 1];
            id.RemoveAt(id.Count - 1);
            Branch target = (Branch)masterBranch.getEditable(id);
            RestructureBranchEdit edit = target.newBranchGroupEdit(inputBlockIndex);
            if (edit.insertedSections[0] is BranchGroup && id.Count > 0)
            {
                // Ctrl+B at top of branch
                addBranch();
                return;
            }
            int branchInsertedAt = edit.insertedSections.ToList().FindIndex(section => section is BranchGroup);
            if (branchInsertedAt < 0)
                throw new Exception("This should never happen.");
            id.Add(edit.nodeIndex + branchInsertedAt); // added branch group
            id.Add(1); // default active branch
            id.Add(0); // text section
            edit.focusFinal = id;
            requestEdit(target, edit);
        }

        public void addBranch()
        {
            IInputElement focusedElement = FocusManager.GetFocusedElement(panel);
            List<int> id = masterBranch.findEditTargetID(focusedElement, EditableTargetType.BranchGroup);
            if (id == null)
                return;
            BranchGroup target = (BranchGroup)masterBranch.getEditable(id);
            EditHistoryItem edit = target.addBranchEdit();
            id.Add(target.branches.Count); // newly added branch
            id.Add(0); // text section
            edit.focusFinal = id;
            requestEdit(target, edit);
        }

        public bool canChangeBranch(int offset)
        {
            IInputElement focusedElement = FocusManager.GetFocusedElement(panel);
            // This approach kinda sucks since we have to search for the target id twice, but w/e
            List<int> id = masterBranch.findEditTargetID(focusedElement, EditableTargetType.BranchGroup);
            if (id == null)
                return false;
            BranchGroup target = (BranchGroup)masterBranch.getEditable(id);
            return target.canChangeBranch(offset);
        }

        public void changeBranch(int offset)
        {
            IInputElement focusedElement = FocusManager.GetFocusedElement(panel);
            // This approach kinda sucks since we have to search for the target id twice, but w/e
            List<int> id = masterBranch.findEditTargetID(focusedElement, EditableTargetType.BranchGroup);
            if (id == null)
                return;
            BranchGroup target = (BranchGroup) masterBranch.getEditable(id);
            EditHistoryItem edit = target.changeBranchEdit(offset);
            if (edit == null)
                return;
            requestEdit(target, edit);
        }

        public void removeBranch()
        {
            IInputElement focusedElement = FocusManager.GetFocusedElement(panel);
            List<int> id = masterBranch.findEditTargetID(focusedElement, EditableTargetType.BranchGroup);
            if (id == null)
                return;
            BranchGroup target = (BranchGroup)masterBranch.getEditable(id);
            // If only one branch left, the entire group should be deleted.
            if (target.branches.Count <= 1)
            {
                deleteBranchGroup();
                return;
            }
            EditHistoryItem edit = target.removeBranchEdit();
            requestEdit(target, edit);
        }

        public void deleteBranchGroup()
        {
            IInputElement focusedElement = FocusManager.GetFocusedElement(panel);
            List<int> id = masterBranch.findEditTargetID(focusedElement, EditableTargetType.BranchGroup);
            if (id == null)
                return;
            int branchGroupIndex = id[id.Count - 1];
            id.RemoveAt(id.Count - 1);
            Branch target = (Branch)masterBranch.getEditable(id);
            // TODO Confirmation dialogue ("Are you sure you want to delete _ branches (_ subbranches) (_ lines)?")
            RestructureBranchEdit edit = target.deleteBranchGroupEdit(branchGroupIndex);
            id.Add(edit.nodeIndex); // merged text section
            edit.focusFinal = id;
            requestEdit(target, edit);
        }

        public void acceptBranch()
        {
            IInputElement focusedElement = FocusManager.GetFocusedElement(panel);
            List<int> id = masterBranch.findEditTargetID(focusedElement, EditableTargetType.BranchGroup);
            if (id == null)
                return;
            int branchGroupIndex = id[id.Count - 1];
            id.RemoveAt(id.Count - 1);
            Branch target = (Branch)masterBranch.getEditable(id);
            // TODO Confirmation dialogue ("Are you sure you want to delete _ branches (_ subbranches) (_ lines)?")
            RestructureBranchEdit edit = target.acceptBranchGroupEdit(branchGroupIndex);
            id.Add(edit.nodeIndex); // merged text section
            edit.focusFinal = id;
            requestEdit(target, edit);
        }

        public void renameBranch()
        {
            IInputElement focusedElement = FocusManager.GetFocusedElement(panel);
            // This approach kinda sucks since we have to search for the target id twice, but w/e
            List<int> id = masterBranch.findEditTargetID(focusedElement, EditableTargetType.BranchGroup);
            if (id == null)
                return;
            BranchGroup target = (BranchGroup)masterBranch.getEditable(id);
            target.renameBranch();
        }

        public bool moveCaretAcrossDivider(InputsBlock currentFocus, int direction, int offset)
        {
            int i = panel.Children.IndexOf(currentFocus) + direction;
            for (; i >= 0 && i < panel.Children.Count; i += direction)
            {
                if (panel.Children[i] is InputsBlock)
                {
                    ((InputsBlock)panel.Children[i]).receiveCaret(direction, offset);
                    return true;
                }
            }
            return false;
        }

        protected override void _importFromFile(string tas)
        {
            if (tas == null)
                tas = DEFAULT_FILE_TEXT;
            tas = tas.Replace("\r\n", "\n");
            if (!tas.Trim().StartsWith('<'))
                tas = Util.convertOldFormatToNew(tas);
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(tas);
            if (xml.DocumentElement.Name != "tas")
                throw new FormatException();
            header = new EditorHeader(xml.DocumentElement.Attributes);
            masterBranch = Branch.fromXml(xml.DocumentElement, this);
            _reloadComponents();
            tasEditedSinceLastWatch = true;
            _clearUndoStack();
        }

        protected override string _exportToFile()
        {
            return header.toXml(masterBranch.toInnerXml());
        }

        public void onReplaySaved(string levelName, Inputs newInputs)
        {
            App.Current.Dispatcher.Invoke((Action)delegate // need to update on main thread
            {
                if (levelName == header.stage())
                    masterBranch.updateInputs(newInputs.getInputLines(), true);
                else
                {
                    // TODO update to a different level: would you like to open?
                    // no // yes (save current file) // yes (discard changes to current file)
                    _importFromFile(newInputs.toText(levelName, 0));
                }
            });
        }

        public void showPlaybackFrame(int frame)
        {
            if (frame == -1)
                return;
            if (blocksByStartFrame == null)
            {
                blocksByStartFrame = new List<FrameAndBlock>();
                masterBranch.listBlocksByStartFrame(blocksByStartFrame);
            }
            playbackFrame = frame;
            FrameAndBlock fb = blocksByStartFrame.FindLast(fb => fb.frame < frame);
            if (fb == null)
                fb = blocksByStartFrame[blocksByStartFrame.Count - 1];
            InputsBlock block = fb.block;
            block.showPlaybackFrame(frame - fb.frame);
            if (highlightedSection != null && highlightedSection != block)
                highlightedSection.showPlaybackFrame(-1);
            highlightedSection = block;
        }

        private void _watch(int breakpoint)
        {
            if (UserPreferences.get("autosave", "false") == "true")
                save();
            string text = masterBranch.getText();
            Inputs tas = new Inputs(text);
            List<Press> presses = tas.toPresses();
            if (presses.Count > 0 && presses[0].frame == 0)
                MessageBox.Show("Warning: \"" + header.stage() + "\" tas may not be legal due to inputs on the first frame.");
            Replay replay = new Replay(presses);
            string replayBuffer = replay.writeString();
            float[] spawn = header.spawn();
            TcpManager.sendLoadReplayCommand(header.stage(), replayBuffer, breakpoint, spawn);
            tasEditedSinceLastWatch = false;
        }

        public void watchFromStart()
        {
            _watch(-1);
        }

        public void watchToCursor()
        {
            IInputElement focusedElement = FocusManager.GetFocusedElement(panel);
            List<int> id = masterBranch.findEditTargetID(focusedElement, EditableTargetType.InputBlock);
            if (id == null)
                return;
            InputsBlock block = (InputsBlock)masterBranch.getEditable(id);
            int lineWithinBlock = block.TextArea.Caret.Line - 1;
            int frameWithinBlock = block.getInputsData().endingFrameForLine(lineWithinBlock);
            _watch(masterBranch.getStartFrameOfBlock(block) + frameWithinBlock);
        }

        public void bringActiveLineToFocus()
        {
            if (masterBranch == null)
                return;
            IInputElement focusedElement = FocusManager.GetFocusedElement(panel);
            List<int> id = masterBranch.findEditTargetID(focusedElement, EditableTargetType.InputBlock);
            if (id == null)
                return;
            InputsBlock block = (InputsBlock)masterBranch.getEditable(id);
            TopBottom activeLine = masterBranch.activeLineYPos(block);
            if (activeLine == null)
                return;
            activeLine.top += header.RenderSize.Height;
            activeLine.bottom += header.RenderSize.Height;
            double scroll = scrollViewer.VerticalOffset;
            if (scroll > activeLine.top)
                scrollViewer.ScrollToVerticalOffset(activeLine.top);
            if (scroll < activeLine.bottom - scrollViewer.ActualHeight)
                scrollViewer.ScrollToVerticalOffset(activeLine.bottom - scrollViewer.ActualHeight);
        }

        public void comment()
        {
            IInputElement focusedElement = FocusManager.GetFocusedElement(panel);
            List<int> id = masterBranch.findEditTargetID(focusedElement, EditableTargetType.InputBlock);
            if (id == null)
                return;
            InputsBlock block = (InputsBlock)masterBranch.getEditable(id);
            block.comment();
        }

        public bool canTimestampComment()
        {
            return playbackFrame != -1;
        }

        public void timestampComment()
        {
            int milliseconds = (int)Math.Round((playbackFrame % 48) * 1000.0 / 48.0);
            int seconds = (playbackFrame / 48) % 60;
            int minutes = playbackFrame / (48 * 60);
            string timestamp = String.Format("# {0} ({1}:{2:D2}.{3:D3})", playbackFrame, minutes, seconds, milliseconds);

            IInputElement focusedElement = FocusManager.GetFocusedElement(panel);
            List<int> id = masterBranch.findEditTargetID(focusedElement, EditableTargetType.InputBlock);
            if (id == null)
                return;
            InputsBlock block = (InputsBlock)masterBranch.getEditable(id);
            block.insertLine(timestamp);
        }
    }
}
