using System.Windows;
using System.Windows.Controls;
using AnnoMapEditor.Utilities.UndoRedo;
using System.Diagnostics;
using System.Windows.Navigation;
using AnnoMapEditor.UI.Windows.Configuration;

namespace AnnoMapEditor.UI.Controls.Toolbar
{
    public partial class Toolbar : UserControl
    {
        public Toolbar()
        {
            InitializeComponent();
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            UndoRedoStack.Instance.Undo();
        }

        private void ShowUndoPopup_Click(object sender, RoutedEventArgs e)
        {
            UndoHistoryPopup.IsOpen = !UndoHistoryPopup.IsOpen;
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            UndoRedoStack.Instance.Redo();
        }

        private void NewMap_Click(object sender, RoutedEventArgs e)
        {
            ToolbarService.Instance.ButtonClick(ToolbarButtonType.NewMap);
        }

        private void LoadMap_Click(object sender, RoutedEventArgs e)
        {
            ToolbarService.Instance.ButtonClick(ToolbarButtonType.LoadMap);
        }

        private void ImportMap_Click(object sender, RoutedEventArgs e)
        {
            ToolbarService.Instance.ButtonContextClick(ToolbarButtonType.ImportMap, sender);
            ToolbarImportContextMenu.PlacementTarget = sender as Panel;
            ToolbarImportContextMenu.IsOpen = true;
        }

        private void SaveMap_Click(object sender, RoutedEventArgs e)
        {
            ToolbarService.Instance.ButtonClick(ToolbarButtonType.SaveMap);
        }

        private void ExportMap_Click(object sender, RoutedEventArgs e)
        {
            ToolbarService.Instance.ButtonClick(ToolbarButtonType.ExportMap);
        }

        private void ZoomReset_Click(object sender, RoutedEventArgs e)
        {
            ToolbarService.Instance.ButtonClick(ToolbarButtonType.ZoomReset);
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            ToolbarService.Instance.ButtonClick(ToolbarButtonType.ZoomIn);
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            ToolbarService.Instance.ButtonClick(ToolbarButtonType.ZoomOut);
        }

        private void ShowLabels_Click(object sender, RoutedEventArgs e)
        {
            ToolbarService.Instance.ButtonClick(ToolbarButtonType.ShowLabels);
        }

        private void DeleteIsland_Click(object sender, RoutedEventArgs e)
        {
            ToolbarService.Instance.ButtonClick(ToolbarButtonType.DeleteIsland);
        }

        private void CopyIsland_Click(object sender, RoutedEventArgs e)
        {
            ToolbarService.Instance.ButtonClick(ToolbarButtonType.CopyIsland);
        }

        private void PasteIsland_Click(object sender, RoutedEventArgs e)
        {
            ToolbarService.Instance.ButtonClick(ToolbarButtonType.PasteIsland);
        }

        private void CustomIslands_Click(object sender, RoutedEventArgs e)
        {
            ToolbarService.Instance.ButtonContextClick(ToolbarButtonType.ShowCustomIslandsMenu, sender);
            ToolbarCustomIslandsContextMenu.PlacementTarget = sender as UIElement;
            ToolbarCustomIslandsContextMenu.IsOpen = true;
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new()
            {
                Owner = Window.GetWindow(this)
            };
            settingsWindow.ShowDialog();
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            HelpPopup.IsOpen = !HelpPopup.IsOpen;
        }

        private void GithubLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
            {
                UseShellExecute = true
            });
            e.Handled = true;
        }
    }
}
