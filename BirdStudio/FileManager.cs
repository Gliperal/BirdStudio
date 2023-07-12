using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace BirdStudio
{
    public abstract class FileManager
    {
        private const string TITLE = "Bird Studio v1.2.4";
        private MainWindow window;
        private string filePath;
        private bool unsavedChanges = false;

        public FileManager(MainWindow window)
        {
            this.window = window;
        }

        protected abstract void _importFromFile(string contents);
        protected abstract string _exportToFile();

        private void _updateWindowTitle()
        {
            if (filePath == null)
                window.Title = TITLE;
            else
                window.Title = (unsavedChanges ? "*" : "") + Util.filePathToFileName(filePath) + " - " + TITLE;
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
                message = "Do you want to save changes to " + Util.filePathToFileName(filePath) + "?";
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

        public void open(string file = null)
        {
            if (!permissionToClose())
                return;

            if (file == null)
            {
                string gameDirectory = Util.getGameDirectory();
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
            }

            try
            {
                if (!file.EndsWith(".tas"))
                {
                    // replay file
                    try
                    {
                        Replay replay = new Replay(file);
                        List<Press> presses = replay.toPresses();
                        Inputs inputs = new Inputs(presses);
                        _importFromFile(inputs.toText("unknown", 0));
                        _setTasFile(null);
                        return;
                    }
                    catch (FormatException ex) { }
                }
                // tas file
                string tas = File.ReadAllText(file);
                _importFromFile(tas);
                _setTasFile(file);
            }
            catch (Exception e) // FormatException XmlException IOException
            {
                Util.logAndReportException(e);
            }
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
