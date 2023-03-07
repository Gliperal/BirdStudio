using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Forms;

namespace BirdStudioRefactor
{
    // TODO Would be nice to generalize FileManager so this can use it too
    public class FileQueue : System.Windows.Controls.TreeView
    {
        public const string TAS_FILES_LOCATION = "C:/Users/Gliperal/Gliperal/Games/The King's Bird/executable versions/TASbot/tas-files/"; // TODO

        List<TreeViewBranch> children = new List<TreeViewBranch>();

        public void open()
        {
            using (OpenFileDialog openFileDialogue = new OpenFileDialog())
            {
                openFileDialogue.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                openFileDialogue.RestoreDirectory = true;
                if (openFileDialogue.ShowDialog() != DialogResult.OK)
                    return;
                string filepath = openFileDialogue.FileName;
                List<string> lines = File.ReadAllLines(filepath).ToList();
                lines = lines.FindAll(line => line.Trim() != "");
                children.Clear();
                while (Items.Count > 0)
                    Items.RemoveAt(0);
                while (lines.Count > 0)
                {
                    // TODO check for format errors
                    TreeViewBranch child = TreeViewBranch.from(lines, TAS_FILES_LOCATION);
                    children.Add(child);
                    AddChild(child);
                }
            }
        }

        public void save()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text file (*.txt)|*.txt";
            saveFileDialog.ShowDialog();
            if (saveFileDialog.FileName == "")
                return;
            string filename = saveFileDialog.FileName;
            string text = "";
            foreach (TreeViewBranch child in children)
                text += child.toText();
            File.WriteAllText(filename, text);
        }

        public void addFile()
        {
            using (OpenFileDialog openFileDialogue = new OpenFileDialog())
            {
                openFileDialogue.Filter = "TAS files (*.tas)|*.tas|All files (*.*)|*.*";
                openFileDialogue.RestoreDirectory = true;
                if (openFileDialogue.ShowDialog() != DialogResult.OK)
                    return;
                string filepath = openFileDialogue.FileName;
                TreeViewBranch child = TreeViewBranch.from(filepath);
                children.Add(child);
                AddChild(child);
            }
        }

        public void removeFile()
        {
            foreach (TreeViewBranch child in children)
                if (child == this.SelectedItem)
                {
                    children.Remove(child);
                    Items.Remove(child);
                    break;
                }
        }

        public void force()
        {
            object selected = this.SelectedItem;
            if (selected is TreeViewBranch)
                ((TreeViewBranch)selected).toggleForced();
        }

        public void play()
        {
            foreach (TreeViewBranch child in children)
            {
                string text = child.branch.getText();
                TASInputs tas = new TASInputs(text);
                List<Press> presses = tas.toPresses();
                Replay replay = new Replay(presses);
                string replayBuffer = replay.writeString();
                if (child == children.First())
                    TcpManager.sendLoadReplayCommand(child.stage, replayBuffer, -1, null);
                else
                    TcpManager.sendQueueReplayCommand(child.stage, replayBuffer);
            }
        }
    }
}
