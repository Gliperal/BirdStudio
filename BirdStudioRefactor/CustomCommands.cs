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
    }
}
