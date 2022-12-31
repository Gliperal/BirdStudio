﻿using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System;
using System.Xml;

namespace BirdStudioRefactor
{
    class TASEditor : FileManager
    {
        private const string DEFAULT_FILE_TEXT = "<tas stage=\"Twin Tree Village\"><inputs>  29\n</inputs></tas>";

        private TASEditorHeader header;
        private StackPanel panel;
        private ScrollViewer scrollViewer;
        private Branch masterBranch;
        private List<EditHistoryItem> editHistory = new List<EditHistoryItem>();
        private int editHistoryLocation = 0;
        private bool tasEditedSinceLastWatch = true;
        private TASEditorSection highlightedSection;
        private int playbackFrame = -1;
        private List<FrameAndBlock> blocksByStartFrame;

        public TASEditor(MainWindow window, StackPanel panel, ScrollViewer scrollViewer, string initialFile = null) : base(window)
        {
            this.panel = panel;
            this.scrollViewer = scrollViewer;
            if (initialFile == null)
                neww();
            else
                open(initialFile);
        }

        public void editPerformed(IEditable target, EditHistoryItem edit)
        {
            List<int> id = masterBranch.findEditTargetID(target);
            edit.targetID = id;
            if (editHistoryLocation < editHistory.Count)
                editHistory.RemoveRange(editHistoryLocation, editHistory.Count - editHistoryLocation);
            editHistory.Add(edit);
            editHistoryLocation++;
            fileChanged();
            blocksByStartFrame = null;
            if (!tasEditedSinceLastWatch)
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
                reloadComponents();
        }

        private void reloadComponents()
        {
            panel.Children.Clear();
            panel.Children.Add(header);
            foreach (UIElement component in masterBranch.getComponents())
                panel.Children.Add(component);
            bringActiveLineToFocus();
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
                reloadComponents();
            // TODO change focus to sections[edit.sectionIndex]
            fileChanged();
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
                reloadComponents();
            // TODO change focus to sections[edit.sectionIndex]
            fileChanged();
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
            EditHistoryItem edit = target.newBranchGroupEdit(inputBlockIndex, this);
            requestEdit(target, edit);
        }

        public void addBranch()
        {
            IInputElement focusedElement = FocusManager.GetFocusedElement(panel);
            List<int> id = masterBranch.findEditTargetID(focusedElement, EditableTargetType.BranchGroup);
            if (id == null)
                return;
            BranchGroup target = (BranchGroup)masterBranch.getEditable(id);
            EditHistoryItem edit = target.addBranchEdit(this);
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
            EditHistoryItem edit = target.deleteBranchGroupEdit(branchGroupIndex, this);
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
            EditHistoryItem edit = target.acceptBranchGroupEdit(branchGroupIndex, this);
            requestEdit(target, edit);
        }

        public void renameBranch()
        {
            IInputElement focusedElement = FocusManager.GetFocusedElement(panel);
            List<int> id = masterBranch.findEditTargetID(focusedElement, EditableTargetType.Branch);
            if (id == null)
                return;
            Branch target = (Branch)masterBranch.getEditable(id);
            RenameDialogue dialogue = new RenameDialogue();
            bool? res = dialogue.ShowDialog();
            if (res != true)
                return;
            string newName = dialogue.ResponseText;
            EditHistoryItem edit = target.renameBranchEdit(newName);
            requestEdit(target, edit);
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
            header = new TASEditorHeader(xml.DocumentElement.Attributes);
            masterBranch = Branch.fromXml(xml.DocumentElement, this);
            reloadComponents();
            tasEditedSinceLastWatch = true;
            _clearUndoStack();
        }

        protected override string _exportToFile()
        {
            return header.toXml(masterBranch.toInnerXml());
        }

        public void onReplaySaved(string levelName, TASInputs newInputs)
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
            TASEditorSection block = fb.block;
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
            TASInputs tas = new TASInputs(text);
            List<Press> presses = tas.toPresses();
            Replay replay = new Replay(presses);
            string replayBuffer = replay.writeString();
            TcpManager.sendLoadReplayCommand(header.stage(), replayBuffer, breakpoint);
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
            TASEditorSection block = (TASEditorSection)masterBranch.getEditable(id);
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
            TASEditorSection block = (TASEditorSection)masterBranch.getEditable(id);
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
            TASEditorSection block = (TASEditorSection)masterBranch.getEditable(id);
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
            TASEditorSection block = (TASEditorSection)masterBranch.getEditable(id);
            block.insertLine(timestamp);
        }
    }
}

// undo/redo: https://stackoverflow.com/questions/1900450/wpf-how-to-prevent-a-control-from-stealing-a-key-gesture
// Can maybe use avalonEdit code folding to get the correct line numbers?
// Use UndoStack.ClearAll() after every change to prevent undo/redo
// When changing components: component.Focus();
