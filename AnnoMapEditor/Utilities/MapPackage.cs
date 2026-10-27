using System;

namespace AnnoMapEditor.Utilities
{
    public class MapPackage : ObservableBase
    {
        private string _name = "New map package";
        private string _customMapFolder = string.Empty;
        private string _customFilesFolder = string.Empty;

        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get => _name; set => SetProperty(ref _name, value); }
        public string CustomMapFolder { get => _customMapFolder; set => SetProperty(ref _customMapFolder, value); }
        public string CustomFilesFolder { get => _customFilesFolder; set => SetProperty(ref _customFilesFolder, value); }
    }
}
