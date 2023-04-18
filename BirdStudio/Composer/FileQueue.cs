using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Forms;

namespace BirdStudio
{
    // TODO Would be nice to generalize FileManager so this can use it too
    public class FileQueue : System.Windows.Controls.TreeView
    {
        List<TreeViewBranch> children = new List<TreeViewBranch>();

        public void open(string tasFilesLocation)
        {
            using (OpenFileDialog openFileDialogue = new OpenFileDialog())
            {
                openFileDialogue.InitialDirectory = tasFilesLocation;
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
                    TreeViewBranch child = TreeViewBranch.from(lines, tasFilesLocation);
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

        public void addFile(string tasFilesLocation)
        {
            using (OpenFileDialog openFileDialogue = new OpenFileDialog())
            {
                openFileDialogue.InitialDirectory = tasFilesLocation;
                openFileDialogue.Filter = "TAS files (*.tas)|*.tas|All files (*.*)|*.*";
                openFileDialogue.RestoreDirectory = true;
                if (openFileDialogue.ShowDialog() != DialogResult.OK)
                    return;
                string filepath = openFileDialogue.FileName;
                string relpath = Path.GetRelativePath(tasFilesLocation, filepath).Replace('\\', '/');
                TreeViewBranch child = TreeViewBranch.from(relpath, tasFilesLocation);
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

        public void queue(bool loadFirst)
        {
            foreach (TreeViewBranch child in children)
            {
                string text = child.branch.getText();
                TASInputs tas = new TASInputs(text);
                List<Press> presses = tas.toPresses();
                Replay replay = new Replay(presses);
                string replayBuffer = replay.writeString();
                if (loadFirst && child == children.First())
                    TcpManager.sendLoadReplayCommand(child.stage, replayBuffer, -1, null);
                else
                    TcpManager.sendQueueReplayCommand(replayBuffer);
            }
        }
    }
}
