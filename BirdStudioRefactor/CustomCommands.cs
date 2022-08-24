using System.Windows.Input;

namespace BirdStudioRefactor.Commands
{
    public class CustomCommands
    {
        private static RoutedUICommand makeCommand(string text, string name, KeyGesture defaultInput)
        {
            InputGestureCollection inputGesture = null;
            KeyGesture input = UserPreferences.getKeyBinding(name, defaultInput);
            if (input != null)
                inputGesture = new InputGestureCollection() { input };
            return new RoutedUICommand(
                text,
                name,
                typeof(CustomCommands),
                inputGesture
            );
        }

        public static RoutedUICommand Undo = makeCommand(
            "Undo",
            "Undo",
            new KeyGesture(Key.Z, ModifierKeys.Control)
        );

        public static RoutedUICommand Redo = makeCommand(
            "Redo",
            "Redo",
            new KeyGesture(Key.Y, ModifierKeys.Control)
        );

        public static RoutedUICommand Comment = makeCommand(
            "Comment/Uncomment Lines",
            "ToggleComment",
            new KeyGesture(Key.OemQuestion, ModifierKeys.Control)
        );

        public static RoutedUICommand AddTimestamp = makeCommand(
            "Add Timestamp",
            "Timestamp",
            new KeyGesture(Key.T, ModifierKeys.Control)
        );

        public static RoutedUICommand NewBranch = makeCommand(
            "New Branch",
            "NewBranch",
            null
        );

        public static RoutedUICommand AddBranch = makeCommand(
            "Add Branch",
            "AddBranch",
            null
        );

        public static RoutedUICommand RemoveBranch = makeCommand(
            "Remove Branch",
            "RemoveBranch",
            null
        );

        public static RoutedUICommand CycleBranch = makeCommand(
            "Cycle Branch",
            "CycleBranch",
            null
        );

        public static RoutedUICommand RenameBranch = makeCommand(
            "Rename Branch",
            "RenameBranch",
            null
        );

        public static RoutedUICommand WatchFromStart = makeCommand(
            "Watch from Start",
            "WatchFromStart",
            new KeyGesture(Key.W, ModifierKeys.Control)
        );

        public static RoutedUICommand WatchToCursor = makeCommand(
            "Watch to Cursor",
            "WatchToCursor",
            new KeyGesture(Key.Q, ModifierKeys.Control)
        );

        public static RoutedUICommand StepFrame = makeCommand(
            "Frame Advance",
            "StepFrame",
            new KeyGesture(Key.OemOpenBrackets)
        );

        public static RoutedUICommand PlayPause = makeCommand(
            "Play / Pause",
            "PlayPause",
            new KeyGesture(Key.OemCloseBrackets)
        );
    }
}
