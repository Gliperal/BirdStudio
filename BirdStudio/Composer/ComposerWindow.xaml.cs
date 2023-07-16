using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;

namespace BirdStudio
{
    public partial class ComposerWindow : Window
    {
        private FileQueue fileQueue;

        public ComposerWindow()
        {
            InitializeComponent();
            string[] args = Environment.GetCommandLineArgs();
            string file = (args.Length > 1) ? args[1] : null;
            ErrorBox.init(errorBox, clearErrorsButton);
            fileQueue = new FileQueue(this, fileQueueRoot);
            filesLocation.TextChanged += (object sender, TextChangedEventArgs e) =>
            {
                fileQueue.tasFilesLocation = filesLocation.Text + "/";
            };
            Thread t = new Thread(new ThreadStart(TalkWithGame));
            t.IsBackground = true;
            t.Start();
        }

        private void TalkWithGame()
        {
            Util.handleCrash(() =>
            {
                while (true)
                {
                    if (!TcpManager.isConnected())
                        TcpManager.connect();
                    Message message = TcpManager.listenForMessage();
                    if (message == null)
                        continue;
                    switch (message.type)
                    {
                        case "SaveReplay":
                            break;
                        case "Frame":
                            int currentFrame = (int)message.args[0];
                            // TODO
                            break;
                        default:
                            Util.logAndReportException(new Exception("Unknown message type: \"" + message.type + "\""));
                            break;
                    }
                }
            });
        }

        private bool _ensureFileLocation()
        {
            if (filesLocation.Text != "")
                return true;
            string gameDir = Util.getGameDirectory();
            if (gameDir != null)
            {
                filesLocation.Text = gameDir.Replace('\\', '/');
                return true;
            }
            using (FolderBrowserDialog folderBrowserDialogue = new FolderBrowserDialog())
            {
                folderBrowserDialogue.Description = "Select TAS files location.";
                if (folderBrowserDialogue.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    filesLocation.Text = folderBrowserDialogue.SelectedPath.Replace('\\', '/');
                    return true;
                }
                else
                    return false;
            }
        }

        private void NewCommand_Execute(object sender, RoutedEventArgs e)
        {
            fileQueue.neww();
        }

        private void OpenCommand_Execute(object sender, RoutedEventArgs e)
        {
            if (!_ensureFileLocation())
                return;
            fileQueue.open();
        }

        private void SaveCommand_Execute(object sender, RoutedEventArgs e)
        {
            fileQueue.save();
        }

        private void SaveAsCommand_Execute(object sender, RoutedEventArgs e)
        {
            fileQueue.saveAs(null);
        }

        private void AddFileCommand_Execute(object sender, RoutedEventArgs e)
        {
            if (!_ensureFileLocation())
                return;
            fileQueue.addFile();
        }

        private void InsertFileCommand_Execute(object sender, RoutedEventArgs e)
        {
            if (!_ensureFileLocation())
                return;
            fileQueue.insertFile();
        }

        private void InsertFileBelowCommand_Execute(object sender, RoutedEventArgs e)
        {
            if (!_ensureFileLocation())
                return;
            fileQueue.insertFileBelow();
        }

        private void RemoveFileCommand_Execute(object sender, RoutedEventArgs e)
        {
            fileQueue.removeFile();
        }

        private void ForceBranchCommand_Execute(object sender, RoutedEventArgs e)
        {
            fileQueue.force();
        }

        private void TCP_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = TcpManager.isConnected();
        }

        private void PlayTASCommand_Execute(object sender, RoutedEventArgs e)
        {
            fileQueue.queue(true);
        }

        private void QueueTASCommand_Execute(object sender, RoutedEventArgs e)
        {
            fileQueue.queue(false);
        }
    }
}
