using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace BirdStudio
{
    public enum ImportStatus
    {
        Success,
        SuccessButCloseFile,
        Failure,
    }

    public abstract class FileManager
    {
        private string title;
        private string openFileFilter;
        private string saveFileFilter;
        private Window window;
        private string filePath;
        private bool unsavedChanges = false;

        public FileManager(Window window, string title, string openFileFilter, string saveFileFilter)
        {
            this.window = window;
            this.title = title;
            this.openFileFilter = openFileFilter;
            this.saveFileFilter = (saveFileFilter == null) ? openFileFilter : saveFileFilter;
        }

        protected abstract ImportStatus _importFromFile(string filePath);
        protected abstract string _exportToFile();

        private void _updateWindowTitle()
        {
            if (filePath == null)
                window.Title = title;
            else
                window.Title = (unsavedChanges ? "*" : "") + Util.filePathToFileName(filePath) + " - " + title;
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
                    if (gameDirectory != null && File.Exists(gameDirectory + @"tas-files\"))
                        openFileDialogue.InitialDirectory = gameDirectory + @"tas-files\";
                    else if (gameDirectory != null)
                        openFileDialogue.InitialDirectory = gameDirectory;
                    openFileDialogue.Filter = openFileFilter;
                    openFileDialogue.RestoreDirectory = true;
                    if (openFileDialogue.ShowDialog() == DialogResult.OK)
                        file = openFileDialogue.FileName;
                    else
                        return;
                }
            }

            ImportStatus status = _importFromFile(file);
            if (status == ImportStatus.Success)
                _setTasFile(file);
            else if (status == ImportStatus.SuccessButCloseFile)
                _setTasFile(null);
        }

        public bool saveAs(string file)
        {
            if (file == null)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = saveFileFilter;
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
