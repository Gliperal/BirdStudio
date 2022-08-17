using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;

namespace BirdStudioRefactor
{
    abstract class FileManager
    {
        private MainWindow window;
        private string filePath;
        private bool unsavedChanges = false;

        public FileManager(MainWindow window)
        {
            this.window = window;
        }

        private string filePathToFileName(string path)
        {
            return path.Split('\\').Last().Split('/').Last();
        }

        private string filePathToNameOnly(string path)
        {
            string name = filePathToFileName(path);
            int i = name.LastIndexOf('.');
            if (i == -1)
                return name;
            else
                return name.Substring(0, i);
        }

        protected abstract void _importFromFile(string contents);
        protected abstract string _exportToFile();

        private void _updateWindowTitle()
        {
            if (filePath == null)
                window.Title = "Bird Studio";
            else
                window.Title = (unsavedChanges ? "*" : "") + filePathToFileName(filePath) + " - Bird Studio";
        }

        private void _setTasFile(string path)
        {
            filePath = path;
            unsavedChanges = false;
            _updateWindowTitle();
        }

        protected void fileChanged()
        {
            unsavedChanges = true;
            _updateWindowTitle();
        }

        public bool permissionToClose()
        {
            if (!unsavedChanges)
                return true;
            string message = "Do you want to save changes?";
            if (filePath != null)
                message = "Do you want to save changes to " + filePathToFileName(filePath) + "?";
            MessageBoxResult result = System.Windows.MessageBox.Show(
                message,
                "caption",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning,
                MessageBoxResult.Cancel
            );
            if (result == MessageBoxResult.Cancel)
                return false;
            else if (result == MessageBoxResult.Yes)
                return save();
            else // MessageBoxResult.No
                return true;
        }

        public void neww()
        {
            if (!permissionToClose())
                return;
            _setTasFile(null);
            _importFromFile(null);
        }

        public void open()
        {
            if (!permissionToClose())
                return;
            string gameDirectory = null;
            try
            {
                Process[] processes = Process.GetProcessesByName("TheKingsBird");
                string path = processes.First().MainModule.FileName;
                int i = path.LastIndexOf('\\');
                gameDirectory = path.Substring(0, i + 1);
            }
            catch { }

            string file;
            using (OpenFileDialog openFileDialogue = new OpenFileDialog())
            {
                if (gameDirectory != null && File.Exists(gameDirectory + @"Replays\"))
                    openFileDialogue.InitialDirectory = gameDirectory + @"Replays\";
                else if (gameDirectory != null)
                    openFileDialogue.InitialDirectory = gameDirectory;
                openFileDialogue.Filter = "TAS files (*.tas)|*.tas|Replay files (*.txt)|*.txt|All files (*.*)|*.*";
                openFileDialogue.RestoreDirectory = true;
                if (openFileDialogue.ShowDialog() == DialogResult.OK)
                    file = openFileDialogue.FileName;
                else
                    return;
            }

            // TODO handle file IO exceptions
            if (!file.EndsWith(".tas"))
            {
                // replay file
                try
                {
                    Replay replay = new Replay(file);
                    List<Press> presses = replay.toPresses();
                    TASInputs inputs = new TASInputs(presses);
                    _setTasFile(null);
                    _importFromFile(">stage unknown\n>rerecords 0\n\n" + inputs.toText());
                    return;
                }
                catch (FormatException ex) { }
            }
            // tas file
            string tas = File.ReadAllText(file);
            _setTasFile(file);
            _importFromFile(tas);
        }

        public bool saveAs(string file)
        {
            if (file == null)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "TAS file (*.tas)|*.tas";
                saveFileDialog.ShowDialog();
                if (saveFileDialog.FileName == "")
                    return false;
                file = saveFileDialog.FileName;
            }
            // TODO else if file != filePath
            // TODO handle fle IO errors
            // TODO test user rejects dialogue box
            string tas = _exportToFile();
            File.WriteAllText(file, tas);
            _setTasFile(file);
            return true;
        }

        public bool save()
        {
            return saveAs(filePath);
        }
    }
}
