using System.Windows.Input;

namespace BirdStudio.Commands
{
    public class CustomCommands
    {
        public static RoutedUICommand Undo = new RoutedUICommand(
            "Undo",
            "Undo",
            typeof(CustomCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.Z, ModifierKeys.Control)
            }
        );

        public static RoutedUICommand Redo = new RoutedUICommand(
            "Redo",
            "Redo",
            typeof(CustomCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.Y, ModifierKeys.Control)
            }
        );
    }
}
