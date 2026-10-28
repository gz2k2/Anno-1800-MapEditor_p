using System.Windows;
using System.Windows.Controls;

namespace AnnoMapEditor.UI.Overlays.SelectCustomIsland
{
    public partial class SelectCustomIslandOverlay : UserControl
    {
        public SelectCustomIslandOverlay()
        {
            InitializeComponent();
            Visibility = Visibility.Collapsed;
            DataContextChanged += This_DataContextChanged;
        }

        private void This_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Visibility = e.NewValue != null ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SelectCustomIslandViewModel viewModel)
                viewModel.OnCancel();
        }
    }
}
