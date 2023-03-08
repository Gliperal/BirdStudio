using System;
using System.Threading;
using System.Windows;
using System.Windows.Forms;

namespace BirdStudioRefactor
{
    public partial class FullgamePlayerWindow : Window
    {
        public FullgamePlayerWindow()
        {
            InitializeComponent();
            string[] args = Environment.GetCommandLineArgs();
            string file = (args.Length > 1) ? args[1] : null;
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

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            if (!_ensureFileLocation())
                return;
            fileQueue.open(filesLocation.Text + "/");
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            fileQueue.save();
        }

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            if (!_ensureFileLocation())
                return;
            fileQueue.addFile(filesLocation.Text + "/");
        }

        private void RemoveFile_Click(object sender, RoutedEventArgs e)
        {
            fileQueue.removeFile();
        }

        private void Force_Click(object sender, RoutedEventArgs e)
        {
            fileQueue.force();
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            fileQueue.play();
        }
    }
}
