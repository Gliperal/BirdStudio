using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace BirdStudioRefactor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TASEditor editor;

        public MainWindow()
        {
            InitializeComponent();
            editor = new TASEditor(this, editorBase);
            if (UserPreferences.get("dark mode", "false") == "true")
            {
                ColorScheme.DarkMode();
                darkModeMenuItem.IsChecked = true;
            }
            else
            {
                ColorScheme.LightMode();
            }
            updateColorScheme();
            if (UserPreferences.get("show help", "false") == "true")
                helpBlock.Visibility = Visibility.Visible;
            new Thread(new ThreadStart(TalkWithGame)).Start();
        }

        private void TalkWithGame()
        {
            while (true)
            {
                if (!TcpManager.isConnected())
                    TcpManager.connect(); // TODO need to update on main thread probably ??
                Message message = TcpManager.listenForMessage();
                if (message == null)
                    continue;
                switch (message.type)
                {
                    case "SaveReplay":
                        string levelName = (string)message.args[0];
                        string replayBuffer = (string)message.args[1];
                        int breakpoint = (int)message.args[2];
                        OnReplaySaved(levelName, replayBuffer, breakpoint);
                        break;
                    case "Frame":
                        // TODO when to set playback frame back to -1?
                        int currentFrame = (int)message.args[0];
                        editor.showPlaybackFrame(currentFrame);
                        float pos_x = (float)message.args[1];
                        float pos_y = (float)message.args[2];
                        float vel_x = (float)message.args[3];
                        float vel_y = (float)message.args[4];
                        // TODO
                        break;
                    default:
                        // TODO Log the unknown message.type
                        break;
                }
            }
        }

        private void OnReplaySaved(string levelName, string replayBuffer, int breakpoint)
        {
            Replay replay;
            try
            {
                replay = new Replay(replayBuffer, false);
            }
            catch (FormatException e) { return; }
            List<Press> presses = replay.toPresses();
            TASInputs newInputs = new TASInputs(presses);
            editor.onReplaySaved(levelName, newInputs);
        }

        private void NewCommand_Execute(object sender, RoutedEventArgs e)
        {
            editor.neww();
        }

        private void OpenCommand_Execute(object sender, RoutedEventArgs e)
        {
            editor.open();
        }

        private void SaveCommand_Execute(object sender, RoutedEventArgs e)
        {
            editor.save();
        }

        private void SaveAsCommand_Execute(object sender, RoutedEventArgs e)
        {
            editor.saveAs(null);
        }

        private void UndoCommand_Execute(object sender, RoutedEventArgs e)
        {
            editor.undo();
        }

        private void UndoCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = editor.canUndo();
        }

        private void RedoCommand_Execute(object sender, RoutedEventArgs e)
        {
            editor.redo();
        }

        private void RedoCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = editor.canRedo();
        }

        private void NewBranch_Execute(object sender, RoutedEventArgs e)
        {
            editor.newBranch();
        }

        private void AddBranch_Execute(object sender, RoutedEventArgs e)
        {
            editor.addBranch();
        }

        private void CycleBranch_Execute(object sender, RoutedEventArgs e)
        {
            editor.cycleBranch();
        }

        private void RemoveBranch_Execute(object sender, RoutedEventArgs e)
        {
            editor.removeBranch();
        }

        private void RenameBranch_Execute(object sender, RoutedEventArgs e)
        {
            editor.renameBranch();
        }

        private void TCP_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = TcpManager.isConnected();
        }

        private void WatchFromStart_Execute(object sender, RoutedEventArgs e)
        {
            editor.watchFromStart();
        }

        private void WatchToCursor_Execute(object sender, RoutedEventArgs e)
        {
            editor.watchToCursor();
        }

        private void PlayPause_Execute(object sender, RoutedEventArgs e)
        {
            TcpManager.sendCommand("TogglePause");
        }

        private void StepFrame_Execute(object sender, RoutedEventArgs e)
        {
            TcpManager.sendCommand("StepFrame");
        }

        private void updateColorScheme()
        {
            foreach (KeyValuePair<string, SolidColorBrush> kvp in ColorScheme.resources)
                Resources[kvp.Key] = kvp.Value;
            // TODO Syntax highlighting
        }

        private void Menu_LightMode(object sender, RoutedEventArgs e)
        {
            ColorScheme.LightMode();
            updateColorScheme();
            UserPreferences.set("dark mode", "false");
        }

        private void Menu_DarkMode(object sender, RoutedEventArgs e)
        {
            ColorScheme.DarkMode();
            updateColorScheme();
            UserPreferences.set("dark mode", "true");
        }

        private void Menu_ToggleHelp(object sender, RoutedEventArgs e)
        {
            if (helpBlock.Visibility == Visibility.Visible)
            {
                helpBlock.Visibility = Visibility.Collapsed;
                UserPreferences.set("show help", "false");
            }
            else
            {
                helpBlock.Visibility = Visibility.Visible;
                UserPreferences.set("show help", "true");
            }
        }
    }
}
