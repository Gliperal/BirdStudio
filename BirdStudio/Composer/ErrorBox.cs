using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BirdStudio
{
    public class ErrorBox
    {
        private static TextBox errorBox;
        private static Button clearButton;

        public static void init(TextBox errorBox, Button clearButton)
        {
            ErrorBox.errorBox = errorBox;
            ErrorBox.clearButton = clearButton;
            errorBox.Visibility = Visibility.Collapsed;
            errorBox.IsReadOnly = true;
            errorBox.Foreground = Brushes.Red;
            clearButton.Visibility = Visibility.Collapsed;
            clearButton.Click += ClearButton_Click;
        }

        public static void reportError(string error)
        {
            if (errorBox.Text == "")
                errorBox.Text = error;
            else
                errorBox.Text += "\n" + error;
            errorBox.Visibility = Visibility.Visible;
            clearButton.Visibility = Visibility.Visible;
            errorBox.ScrollToEnd();
        }

        public static void clear()
        {
            errorBox.Text = "";
            errorBox.Visibility = Visibility.Collapsed;
            clearButton.Visibility = Visibility.Collapsed;
        }

        private static void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            clear();
        }
    }
}
