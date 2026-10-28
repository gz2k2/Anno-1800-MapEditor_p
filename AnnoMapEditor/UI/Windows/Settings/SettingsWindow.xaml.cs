using AnnoMapEditor.Utilities;
using AnnoMapEditor.DataArchives.Assets;
using Ookii.Dialogs.Wpf;
using System.Windows;
using System.ComponentModel;
using System.Windows.Data;
using System.Text.RegularExpressions;

namespace AnnoMapEditor.UI.Windows.Configuration
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = Settings.Instance;
            ICollectionView packagesView = CollectionViewSource.GetDefaultView(Settings.Instance.MapPackages);
            packagesView.SortDescriptions.Add(new SortDescription(nameof(MapPackage.Name), ListSortDirection.Ascending));
            if (packagesView is ICollectionViewLiveShaping liveShaping && liveShaping.CanChangeLiveSorting)
                liveShaping.IsLiveSorting = true;
        }

        private void SelectGamePath_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog picker = new()
            {
                UseDescriptionForTitle = true,
                Description = "Select the Anno 1800 installation folder",
                SelectedPath = Settings.Instance.GamePath
            };

            if (true == picker.ShowDialog(this))
                Settings.Instance.GamePath = picker.SelectedPath;
        }

        private void SelectTemplateFolder_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog picker = new()
            {
                UseDescriptionForTitle = true,
                Description = "Select the default folder for map template files",
                SelectedPath = Settings.Instance.TemplateFolderPath
            };

            if (true == picker.ShowDialog(this))
                Settings.Instance.TemplateFolderPath = picker.SelectedPath;
        }

        private void AddPackage_Click(object sender, RoutedEventArgs e)
        {
            MapPackage package = new();
            Settings.Instance.MapPackages.Add(package);
            PackagesGrid.SelectedItem = package;
        }

        private void DeletePackage_Click(object sender, RoutedEventArgs e)
        {
            if (PackagesGrid.SelectedItem is MapPackage package)
                Settings.Instance.MapPackages.Remove(package);
        }

        private void SelectCustomMapFolder_Click(object sender, RoutedEventArgs e)
        {
            if (PackagesGrid.SelectedItem is not MapPackage package)
                return;
            VistaFolderBrowserDialog picker = new()
            {
                UseDescriptionForTitle = true,
                Description = "Select the folder containing this package's custom maps",
                SelectedPath = package.CustomMapFolder
            };

            if (true == picker.ShowDialog(this))
                package.CustomMapFolder = picker.SelectedPath;
        }

        private void SelectCustomFilesFolder_Click(object sender, RoutedEventArgs e)
        {
            if (PackagesGrid.SelectedItem is not MapPackage package)
                return;
            VistaFolderBrowserDialog picker = new()
            {
                UseDescriptionForTitle = true,
                Description = "Select the files to export with this map package",
                SelectedPath = package.CustomFilesFolder
            };

            if (true == picker.ShowDialog(this))
                package.CustomFilesFolder = picker.SelectedPath;
        }

        private static bool IsDigitsOnly(string value) => Regex.IsMatch(value, @"^[0-9]*$");

        private void GuidTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !IsDigitsOnly(e.Text);
        }

        private void GuidTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            string text = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
            if (!IsDigitsOnly(text))
                e.CancelCommand();
        }

        private async void Close_Click(object sender, RoutedEventArgs e)
        {
            PackagesGrid.CommitEdit();
            Settings.Instance.SaveMapPackages();
            await CustomIslandCatalog.Instance.RefreshAsync();
            Close();
        }
    }
}
