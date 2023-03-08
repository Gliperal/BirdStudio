﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace BirdStudio
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TASEditor editor;
        private bool lCtrlDown;
        private bool rCtrlDown;

        public MainWindow()
        {
            InitializeComponent();
            string[] args = Environment.GetCommandLineArgs();
            string file = (args.Length > 1) ? args[1] : null;
            editor = new TASEditor(this, editorBase, editorScrollViewer, file);
            if (UserPreferences.get("dark mode", "false") == "true")
            {
                ColorScheme.instance().DarkMode();
                darkModeMenuItem.IsChecked = true;
            }
            else
            {
                ColorScheme.instance().LightMode();
            }
            updateColorScheme();
            if (UserPreferences.get("show help", "false") == "true")
                helpBlock.Visibility = Visibility.Visible;
            if (UserPreferences.get("autosave", "false") == "true")
                autosaveMenuItem.IsChecked = true;
            Thread t = new Thread(new ThreadStart(TalkWithGame));
            t.IsBackground = true;
            t.Start();
            this.PreviewKeyDown += Window_PreviewKeyDown;
            this.PreviewKeyUp += Window_PreviewKeyUp;
            this.PreviewMouseWheel += Window_PreviewMouseWheel;
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
                            Util.logAndReportException(new Exception("Unknown message type: \"" + message.type + "\""));
                            break;
                    }
                }
            });
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
            Util.handleCrash(() =>
            {
                editor.neww();
            });
        }

        private void OpenCommand_Execute(object sender, RoutedEventArgs e)
        {
            Util.handleCrash(() =>
            {
                editor.open();
            });
        }

        private void SaveCommand_Execute(object sender, RoutedEventArgs e)
        {
            Util.handleCrash(() =>
            {
                editor.save();
            });
        }

        private void SaveAsCommand_Execute(object sender, RoutedEventArgs e)
        {
            Util.handleCrash(() =>
            {
                editor.saveAs(null);
            });
        }

        private void UndoCommand_Execute(object sender, RoutedEventArgs e)
        {
            Util.handleCrash(() =>
            {
                editor.undo();
            });
        }

        private void UndoCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = editor.canUndo();
        }

        private void RedoCommand_Execute(object sender, RoutedEventArgs e)
        {
            Util.handleCrash(() =>
            {
                editor.redo();
            });
        }

        private void RedoCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = editor.canRedo();
        }

        private void CommentCommand_Execute(object sender, RoutedEventArgs e)
        {
            Util.handleCrash(() =>
            {
                editor.comment();
            });
        }

        private void TimestampCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = editor.canTimestampComment();
        }

        private void TimestampCommand_Execute(object sender, RoutedEventArgs e)
        {
            Util.handleCrash(() =>
            {
                editor.timestampComment();
            });
        }

        private void NewBranch_Execute(object sender, RoutedEventArgs e)
        {
            Util.handleCrash(() =>
            {
                editor.newBranch();
            });
        }

        private void AddBranch_Execute(object sender, RoutedEventArgs e)
        {
            Util.handleCrash(() =>
            {
                editor.addBranch();
            });
        }

        private void PrevBranch_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = editor.canChangeBranch(-1);
        }

        private void PrevBranch_Execute(object sender, RoutedEventArgs e)
        {
            Util.handleCrash(() =>
            {
                editor.changeBranch(-1);
            });
        }

        private void NextBranch_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = editor.canChangeBranch(1);
        }

        private void NextBranch_Execute(object sender, RoutedEventArgs e)
        {
            Util.handleCrash(() =>
            {
                editor.changeBranch(1);
            });
        }

        private void RemoveBranch_Execute(object sender, RoutedEventArgs e)
        {
            Util.handleCrash(() =>
            {
                editor.removeBranch();
            });
        }

        private void AcceptBranch_Execute(object sender, RoutedEventArgs e)
        {
            Util.handleCrash(() =>
            {
                editor.acceptBranch();
            });
        }

        private void RenameBranch_Execute(object sender, RoutedEventArgs e)
        {
            Util.handleCrash(() =>
            {
                editor.renameBranch();
            });
        }

        private void TCP_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = TcpManager.isConnected();
        }

        private void WatchFromStart_Execute(object sender, RoutedEventArgs e)
        {
            Util.handleCrash(() =>
            {
                editor.watchFromStart();
            });
        }

        private void WatchToCursor_Execute(object sender, RoutedEventArgs e)
        {
            Util.handleCrash(() =>
            {
                editor.watchToCursor();
            });
        }

        private void PlayPause_Execute(object sender, RoutedEventArgs e)
        {
            Util.handleCrash(() =>
            {
                TcpManager.sendCommand("TogglePause");
            });
        }

        private void StepFrame_Execute(object sender, RoutedEventArgs e)
        {
            Util.handleCrash(() =>
            {
                TcpManager.sendCommand("StepFrame");
            });
        }

        private void updateColorScheme()
        {
            foreach (KeyValuePair<string, SolidColorBrush> kvp in ColorScheme.instance().resources)
                Resources[kvp.Key] = kvp.Value;
        }

        private void Menu_LightMode(object sender, RoutedEventArgs e)
        {
            ColorScheme.instance().LightMode();
            updateColorScheme();
            UserPreferences.set("dark mode", "false");
        }

        private void Menu_DarkMode(object sender, RoutedEventArgs e)
        {
            ColorScheme.instance().DarkMode();
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

        private void Menu_AutosaveOn(object sender, RoutedEventArgs e)
        {
            UserPreferences.set("autosave", "true");
        }

        private void Menu_AutosaveOff(object sender, RoutedEventArgs e)
        {
            UserPreferences.set("autosave", "false");
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Util.handleCrash(() =>
            {
                // Workaround for avalonEdit stealing my key gestures
                if (e.Key == Key.LeftCtrl)
                    lCtrlDown = true;
                if (e.Key == Key.RightCtrl)
                    rCtrlDown = true;
                bool ctrlDown = lCtrlDown || rCtrlDown;
                if (ctrlDown && e.Key == Key.Z)
                {
                    editor.undo();
                    e.Handled = true;
                }
                if (ctrlDown && e.Key == Key.Y)
                {
                    editor.redo();
                    e.Handled = true;
                }
            });
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl)
                lCtrlDown = false;
            if (e.Key == Key.RightCtrl)
                rCtrlDown = false;
        }

        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Workaround for avalonEdit stealing my scroll inputs
            if (e.Delta > 0)
                editorScrollViewer.ScrollToVerticalOffset(editorScrollViewer.VerticalOffset - 64);
            else
                editorScrollViewer.ScrollToVerticalOffset(editorScrollViewer.VerticalOffset + 64);
            e.Handled = true;
        }
    }
}