using System;
using System.Threading;
using System.Windows;

namespace BirdStudioRefactor
{
    public partial class FullgamePlayerWindow : Window
    {
        private TASEditor editor;
        private bool lCtrlDown;
        private bool rCtrlDown;

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

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            fileQueue.addFile();
        }

        private void RemoveFile_Click(object sender, RoutedEventArgs e)
        {
            fileQueue.removeFile();
        }

        private void Force_Click(object sender, RoutedEventArgs e)
        {
            fileQueue.force();
        }
    }
}
