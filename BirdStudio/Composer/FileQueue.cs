using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;

namespace BirdStudio
{
    // TODO Would be nice to generalize FileManager so this can use it too
    public class FileQueue : FileManager
    {
        public System.Windows.Controls.TreeView root;
        public string tasFilesLocation;

        public FileQueue(ComposerWindow window, System.Windows.Controls.TreeView root)
            : base(window, "Bird Composer v1.3.0", "Text files (*.txt)|*.txt|All files (*.*)|*.*", "Text file (*.txt)|*.txt")
        {
            this.root = root;
            root.PreviewKeyDown += FileQueue_PreviewKeyDown;
        }

        protected override ImportStatus _importFromFile(string file)
        {
            List<string> lines = (file != null)
                ? File.ReadAllLines(file).ToList()
                : new List<string>();
            lines = lines.FindAll(line => line.Trim() != "");
            while (root.Items.Count > 0)
                root.Items.RemoveAt(0);
            ErrorBox.clear();
            while (lines.Count > 0)
            {
                TreeViewBranch child = TreeViewBranch.from(lines, tasFilesLocation);
                root.Items.Add(child);
            }
            return ImportStatus.Success;
        }

        protected override string _exportToFile()
        {
            string text = "";
            foreach (TreeViewBranch child in root.Items)
                text += child.toText();
            return text;
        }

        public void addFile(int position=-1)
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
                    root.Items.Add(child);
                else
                    root.Items.Insert(position, child);
                fileChanged();
            }
        }

        public void insertFile()
        {
            addFile(root.Items.IndexOf(root.SelectedItem));
        }

        public void insertFileBelow()
        {
            addFile(root.Items.IndexOf(root.SelectedItem) + 1);
        }

        public void removeFile()
        {
            root.Items.Remove(root.SelectedItem);
            fileChanged();
        }

        public void force()
        {
            object selected = root.SelectedItem;
            if (selected is TreeViewBranch)
            {
                ((TreeViewBranch)selected).toggleForced();
                fileChanged();
            }
        }

        public void queue(bool loadFirst)
        {
            foreach (TreeViewBranch child in root.Items)
            {
                string text = child.branch.getText();
                Inputs tas = new Inputs(text);
                List<Press> presses = tas.toPresses();
                Replay replay = new Replay(presses);
                string replayBuffer = replay.writeString();
                if (loadFirst && child == root.Items[0])
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
