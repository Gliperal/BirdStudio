using System.Windows.Input;

namespace BirdStudio.Composer.Commands
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

        public static RoutedUICommand AddFile = makeCommand(
            "Add File",
            "AddFile",
            new KeyGesture(Key.T, ModifierKeys.Control)
        );

        public static RoutedUICommand InsertFile = makeCommand(
            "Insert File",
            "InsertFile",
            new KeyGesture(Key.I, ModifierKeys.Control)
        );

        public static RoutedUICommand RemoveFile = makeCommand(
            "Remove File",
            "RemoveFile",
            null
        );

        public static RoutedUICommand ForceBranch = makeCommand(
            "Force / Unforce Branch",
            "ForceBranch",
            new KeyGesture(Key.F, ModifierKeys.Control)
        );

        public static RoutedUICommand PlayTAS = makeCommand(
            "Play TAS",
            "PlayTAS",
            new KeyGesture(Key.W, ModifierKeys.Control)
        );

        public static RoutedUICommand QueueTAS = makeCommand(
            "Queue TAS",
            "QueueTAS",
            new KeyGesture(Key.Q, ModifierKeys.Control)
        );
    }
}
