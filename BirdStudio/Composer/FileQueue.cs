using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;

namespace BirdStudio
{
    // TODO Would be nice to generalize FileManager so this can use it too
    public class FileQueue : System.Windows.Controls.TreeView
    {
        public FileQueue()
        {
            PreviewKeyDown += FileQueue_PreviewKeyDown;
        }

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
                while (Items.Count > 0)
                    Items.RemoveAt(0);
                ErrorBox.clear();
                while (lines.Count > 0)
                {
                    TreeViewBranch child = TreeViewBranch.from(lines, tasFilesLocation);
                    Items.Add(child);
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
            foreach (TreeViewBranch child in Items)
                text += child.toText();
            File.WriteAllText(filename, text);
        }

        public void addFile(string tasFilesLocation, int position=-1)
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
                if (position == -1)
                    Items.Add(child);
                else
                    Items.Insert(position, child);
            }
        }

        public void insertFile(string tasFilesLocation)
        {
            addFile(tasFilesLocation, Items.IndexOf(SelectedItem));
        }

        public void removeFile()
        {
            Items.Remove(SelectedItem);
        }

        public void force()
        {
            object selected = this.SelectedItem;
            if (selected is TreeViewBranch)
                ((TreeViewBranch)selected).toggleForced();
        }

        public void queue(bool loadFirst)
        {
            foreach (TreeViewBranch child in Items)
            {
                string text = child.branch.getText();
                Inputs tas = new Inputs(text);
                List<Press> presses = tas.toPresses();
                Replay replay = new Replay(presses);
                string replayBuffer = replay.writeString();
                if (loadFirst && child == Items[0])
                    TcpManager.sendLoadReplayCommand(child.stage, replayBuffer, -1, null);
                else
                    TcpManager.sendQueueReplayCommand(replayBuffer);
            }
        }

        private void FileQueue_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Back || e.Key == Key.Delete)
                removeFile();
        }
    }
}
