using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BirdStudio
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
            this.LostFocus += Header_OnLostFocus;
            this.KeyDown += Header_OnKeyDown;
            this.MouseRightButtonUp += Header_OnMouseRightButtonUp;
        }

        public void beginRename()
        {
            nameDisplay.Visibility = Visibility.Collapsed;
            nameEdit.Visibility = Visibility.Visible;
            nameEdit.Text = name;
            nameEdit.SelectAll();
            nameEdit.Focus();
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

        public void setBranch(string prefix, string name)
        {
            this.name = name;
            nameDisplay.Text = $"{prefix} {name}";
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
