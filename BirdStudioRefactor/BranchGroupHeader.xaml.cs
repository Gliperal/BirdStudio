using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BirdStudioRefactor
{
    /// <summary>
    /// Interaction logic for BranchGroupHeader.xaml
    /// </summary>
    public partial class BranchGroupHeader : UserControl
    {
        private BranchGroup parent;
        private string name;
        private bool editing = false;

        public BranchGroupHeader(BranchGroup parent)
        {
            this.parent = parent;
            InitializeComponent();
            SetResourceReference(BackgroundProperty, "TextBlock.Background");
            SetResourceReference(ForegroundProperty, "TextBlock.Foreground");
            this.LostFocus += Header_OnLostFocus;
            this.KeyDown += Header_OnKeyDown;
            this.MouseRightButtonUp += Header_OnMouseRightButtonUp;
        }

        public void beginRename()
        {
            nameEdit.Focus();
            // TODO Grab user cursor, scroll to rename box if out of view (editor.bringComponentIntoFocus(this))
            nameDisplay.Visibility = Visibility.Collapsed;
            nameEdit.Visibility = Visibility.Visible;
            nameEdit.Text = name;
            nameEdit.SelectAll();
            editing = true;
        }

        private void _finalizeRename()
        {
            nameDisplay.Visibility = Visibility.Visible;
            nameEdit.Visibility = Visibility.Collapsed;
            if (name != nameEdit.Text)
                parent.requestBranchNameChange(nameEdit.Text);
            editing = false;
        }

        private void _cancelRename()
        {
            nameDisplay.Visibility = Visibility.Visible;
            nameEdit.Visibility = Visibility.Collapsed;
            editing = false;
        }

        public void setBranch(int number, string name)
        {
            this.name = name;
            nameDisplay.Text = $"[{number}] {name}";
        }

        public void Header_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (editing)
                _finalizeRename();
        }

        public void Header_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (editing && e.Key == Key.Enter)
                _finalizeRename();
            if (editing && e.Key == Key.Escape)
                _cancelRename();
        }

        public void Header_OnMouseRightButtonUp(object sender, RoutedEventArgs e)
        {
            beginRename();
        }
    }
}
