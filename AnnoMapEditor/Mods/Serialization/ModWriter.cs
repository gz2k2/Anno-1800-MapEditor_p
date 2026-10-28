using Anno.FileDBModels.Anno1800.Gamedata.Models.Shared;
using AnnoMapEditor.DataArchives;
using AnnoMapEditor.DataArchives.Assets.Models;
using AnnoMapEditor.DataArchives.Assets.Repositories;
using AnnoMapEditor.MapTemplates.Serializing;
using AnnoMapEditor.MapTemplates.Models;
using AnnoMapEditor.Mods.Enums;
using AnnoMapEditor.Mods.Models;
using AnnoMapEditor.Utilities;
using FileDBSerializing;
using FileDBSerializing.ObjectSerializer;
using Newtonsoft.Json;
using RDAExplorer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Linq;

namespace AnnoMapEditor.Mods.Serialization
{
    public class ModWriter
    {
        public const string AME_POOL_PATH = @"data\ame\maps\pool";


        private readonly MapTemplateWriter _mapTemplateWriter;

        private readonly AssetRepository _assetRepository;


        public ModWriter()
        {
            _mapTemplateWriter = new();
            _assetRepository = DataManager.Instance.AssetRepository;
        }


        /// <summary>
        /// Determines the list of MapTemplateAssets to be replaced for a mod targeting the given 
        /// <paramref name="session">session</paramref> and <paramref name="mapType">mapType</paramref>.
        /// 
        /// <para>
        /// The way Anno 1800 chooses which template to use differs between sessions. As of GU17.1,
        /// there are three scenarios we support:
        /// </para>
        /// 
        /// 1. <see cref="SessionAsset.OldWorld">The Old World</see><br />
        /// The map template used for The Old World is randomly chosen out of one from a set pools.
        /// Separate map pools exist for different map sizes and <see cref="MapType">MapTypes</see>.
        /// To guarantee the mod's map template will be used, we replace the entire pool for the
        /// given <param name="mapType">mapType</param>.<br />
        /// 
        /// 2. <see cref="SessionAsset.NewWorld">The New World</see><br />
        /// Similarly to The Old World, the The New World's map template is are chosen randomly
        /// from a pool. However, there exist no MapTypes for this session.<br />
        /// Additionally, The 13th DLC "New World Rising" introduced the concept of map extensions.
        /// All new world map templates were extended to make room for the island Manola in the top
        /// corner of the session.<br />
        /// All New World map templates and their extended versions are being targeted to be
        /// replaced.<br/>
        /// 
        /// 3. <see cref="SessionAsset.CapeTrelawney">Cape Trelawney</see>, 
        /// <see cref="SessionAsset.Arctic">Arctic</see> and <see cref="SessionAsset.Enbesa">Enbesa
        /// </see><br />
        /// All other supported sessions reference a map template directly via the attributes
        /// <see cref="SessionAsset.MapTemplate">MapTemplate</see> and
        /// <see cref="SessionAsset.MapTemplateForMultiplayer">MapTemplateForMultiplayer</see>. In
        /// their case, replacing these map templates is sufficient to use the mod's map template
        /// instead.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="mapType">The <see cref="MapType">MapType</see> to be used for the mod.
        /// Must not be null if <paramref name="session">session</paramref> is
        /// <see cref="SessionAsset.OldWorld">OldWorld</see>.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private IList<MapTemplateAsset> GetMapTemplatesToReplace(SessionAsset session, MapType? mapType = null)
        {
            // Sessions may reference the MapTemplates to be used directly via the MapTemplate and
            // MapTemplateForMultiplayer attributes.
            if (session.MapTemplate != null)
            {
                List<MapTemplateAsset> mapTemplates = new();
                mapTemplates.Add(session.MapTemplate);
                if (session.MapTemplateForMultiplayer != null)
                    mapTemplates.Add(session.MapTemplateForMultiplayer);

                return mapTemplates;
            }

            // Otherwise get pool maps matching the Session's region.
            else
            {
                IEnumerable<MapTemplateAsset> mapTemplates = _assetRepository.GetAll<MapTemplateAsset>()
                    .Where(m => m.TemplateRegion == session.Region)
                    .Where(m => m.TemplateFilename.Contains("pool"));

                // For The Old World filter according to the MapType.
                if (session == SessionAsset.OldWorld)
                {
                    if (mapType != null)
                        mapTemplates = mapTemplates.Where(m => m.TemplateMapType == mapType);
                    else
                        throw new ArgumentException($"Cannot determine MapTemplates to replace for session {session.GUID} \"{session.DisplayName}\" without a given {typeof(MapType).FullName}.");
                }

                return mapTemplates.ToList();
            }
        }

        public async Task<bool> WriteAsync(Mod mod, string modPath)
        {
            FileUtils.TryDeleteDirectory(modPath);
            Directory.CreateDirectory(modPath);

            SessionAsset session = mod.MapTemplate.Session;

            IList<MapTemplateAsset> mapTemplatesToReplace = GetMapTemplatesToReplace(session, mod.MapType);

            await WriteModinfoJson(mod, modPath);

            //Only write Language XML for OW Maps, as only they need naming in a menu
            if (session == SessionAsset.OldWorld)
                await WriteLanguageXml(modPath, mod.Name, mod.MapType!.Guid);

            // create the first copy of a7t, a7tinfo and a7te
            MapTemplateAsset firstMapTemplate = mapTemplatesToReplace.First();
            string mapFileDirectory = Path.Combine(modPath, AME_POOL_PATH);
            string a7tPath = Path.Combine(mapFileDirectory, Path.GetFileName(firstMapTemplate.TemplateFilename));
            string a7tinfoPath = Path.ChangeExtension(a7tPath, "a7tinfo");
            string a7tePath = Path.ChangeExtension(a7tPath, "a7te");

            await _mapTemplateWriter.WriteA7tinfoAsync(mod.MapTemplate, Path.Combine(modPath, a7tinfoPath));
            WriteA7T(mod, a7tPath);
            WriteA7te(mod.MapTemplate.Size.X, a7tePath);

            if (firstMapTemplate.EnlargedTemplateFilename != null)
            {
                string enlargedA7tPath = GetEnlargedTemplatePath(a7tPath);
                CopyTemplateFiles(a7tPath, a7tinfoPath, a7tePath, enlargedA7tPath);
            }

            // copy a7t, a7tinfo and a7te for all MapTemplates that must be replaced
            for (int i = 1; i < mapTemplatesToReplace.Count; ++i)
            {
                MapTemplateAsset mapTemplate = mapTemplatesToReplace[i];
                string mapFilename = Path.GetFileNameWithoutExtension(mapTemplate.TemplateFilename);

                string targetA7tPath = Path.Combine(mapFileDirectory, mapFilename + ".a7t");
                CopyTemplateFiles(a7tPath, a7tinfoPath, a7tePath, targetA7tPath);

                if (mapTemplate.EnlargedTemplateFilename != null)
                {
                    CopyTemplateFiles(a7tPath, a7tinfoPath, a7tePath, GetEnlargedTemplatePath(targetA7tPath));
                }
            }

            CopyCustomIslandDirectories(mod.MapTemplate, modPath);
            CopyAdditionalFiles(mod.MapTemplate, modPath);
            await WriteAssetsXml(mod, modPath, mapTemplatesToReplace);

            return true;
        }

        /// <summary>
        /// Custom islands are self-contained folders. Besides the three a7m sidecars they contain
        /// _gamedata with meshes and the preview image, so copying individual files is insufficient.
        /// </summary>
        private static void CopyCustomIslandDirectories(MapTemplate mapTemplate, string modPath)
        {
            IEnumerable<IslandAsset> customIslands = mapTemplate.Elements
                .OfType<FixedIslandElement>()
                .Select(element => element.IslandAsset)
                .Where(asset => !string.IsNullOrWhiteSpace(asset.CustomSourceDirectory))
                .GroupBy(asset => asset.FilePath, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First());

            foreach (IslandAsset island in customIslands)
            {
                string sourceDirectory = island.CustomSourceDirectory!;
                string sourceA7m = Path.Combine(sourceDirectory, Path.GetFileName(island.FilePath));
                if (!Directory.Exists(sourceDirectory) || !File.Exists(sourceA7m))
                    throw new FileNotFoundException($"Custom island '{island.DisplayName}' is no longer available at '{sourceDirectory}'.", sourceA7m);

                string? virtualDirectory = Path.GetDirectoryName(island.FilePath);
                if (string.IsNullOrWhiteSpace(virtualDirectory) || Path.IsPathRooted(virtualDirectory))
                    throw new InvalidDataException($"Invalid custom-island export path '{island.FilePath}'.");

                string targetDirectory = Path.Combine(modPath, virtualDirectory);
                foreach (string sourceFile in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
                {
                    string relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
                    string targetFile = Path.Combine(targetDirectory, relativePath);
                    string? targetFileDirectory = Path.GetDirectoryName(targetFile);
                    if (targetFileDirectory != null)
                        Directory.CreateDirectory(targetFileDirectory);
                    File.Copy(sourceFile, targetFile, overwrite: true);
                }
            }
        }

        /// <summary>
        /// Copies the shared custom files of every map package used in this export.
        /// </summary>
        internal static void CopyAdditionalFiles(MapTemplate mapTemplate, string modPath)
        {
            IEnumerable<string> customFilesDirectories = mapTemplate.Elements
                .OfType<FixedIslandElement>()
                .Select(element => element.IslandAsset.CustomFilesDirectory)
                .Where(directory => !string.IsNullOrWhiteSpace(directory))
                .Distinct(StringComparer.OrdinalIgnoreCase)!;

            foreach (string customFilesDirectory in customFilesDirectories)
            {
                if (!Directory.Exists(customFilesDirectory))
                    continue;

                CopyDirectoryContents(customFilesDirectory, modPath);
            }
        }

        private static void CopyDirectoryContents(string sourceDirectory, string targetDirectory)
        {
            foreach (string sourceFile in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
                string targetFile = Path.Combine(targetDirectory, relativePath);
                string? targetFileDirectory = Path.GetDirectoryName(targetFile);
                if (targetFileDirectory != null)
                    Directory.CreateDirectory(targetFileDirectory);
                File.Copy(sourceFile, targetFile, overwrite: true);
            }
        }



        private async Task WriteAssetsXml(Mod mod, string modPath, IEnumerable<MapTemplateAsset> mapTemplatesToReplace)
        {
            string assetsXmlPath = Path.Combine(modPath, @"data\config\export\main\asset\assets.xml");

            string? assetsXmlDir = Path.GetDirectoryName(assetsXmlPath);
            if (assetsXmlDir is not null)
                Directory.CreateDirectory(assetsXmlDir);

            XElement modOps = new("ModOps");
            HashSet<long> usedGuids = new();

            // A package may already provide an assets.xml through Custom Files. Preserve all of
            // its ModOps and include its asset GUIDs when searching for a free regional GUID.
            if (File.Exists(assetsXmlPath))
            {
                XDocument existingDocument = XDocument.Load(assetsXmlPath);
                XElement? existingRoot = existingDocument.Root;
                if (existingRoot?.Name.LocalName != "ModOps")
                    throw new InvalidDataException($"Existing assets.xml '{assetsXmlPath}' does not have a ModOps root element.");

                foreach (XElement existingModOp in existingRoot.Elements())
                    modOps.Add(new XElement(existingModOp));

                foreach (XElement guidElement in existingRoot.Descendants("Standard").Elements("GUID"))
                {
                    if (long.TryParse(guidElement.Value, out long usedGuid))
                        usedGuids.Add(usedGuid);
                }
            }

            foreach (MapTemplateAsset mapTemplateAsset in mapTemplatesToReplace)
            {
                string templateFilename = Path.Combine(AME_POOL_PATH, Path.GetFileName(mapTemplateAsset.TemplateFilename));
                string? enlargedTemplateFilename = mapTemplateAsset.EnlargedTemplateFilename != null
                    ? GetEnlargedTemplatePath(templateFilename)
                    : null;

                StringBuilder replacement = new();
                WriteModOpReplaceTemplateFilename(replacement, mapTemplateAsset.GUID, templateFilename, enlargedTemplateFilename);
                XElement replacementElement = XElement.Parse(replacement.ToString());
                modOps.Add(replacementElement);
            }

            IReadOnlyList<IslandAsset> customIslands = GetCustomIslands(mod.MapTemplate);
            if (customIslands.Count > 0)
            {
                long rangeStart = GetCustomIslandGuidRangeStart(mod.MapTemplate.Session);
                string islandRegion = GetIslandRegionId(mod.MapTemplate.Session);
                Queue<long> freeGuids = GetFreeCustomIslandGuids(rangeStart, usedGuids);

                if (freeGuids.Count < customIslands.Count)
                {
                    throw new InvalidOperationException(
                        $"The GUID range {rangeStart}-{rangeStart + 24} for '{mod.MapTemplate.Session.DisplayName}' " +
                        $"does not contain enough free GUIDs. Required: {customIslands.Count}; available: {freeGuids.Count}.");
                }

                foreach (IslandAsset island in customIslands)
                {
                    long guid = freeGuids.Dequeue();
                    string mapName = Path.GetFileNameWithoutExtension(island.FilePath);
                    string exportedFilePath = island.FilePath.Replace('\\', '/');
                    modOps.Add(CreateCustomIslandModOp(guid, mapName, exportedFilePath, islandRegion));
                }
            }

            XDocument document = new(new XDeclaration("1.0", "utf-8", null), modOps);
            await using FileStream stream = new(
                assetsXmlPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true);
            await document.SaveAsync(stream, SaveOptions.None, default);
        }

        private static IReadOnlyList<IslandAsset> GetCustomIslands(MapTemplate mapTemplate)
        {
            return mapTemplate.Elements
                .OfType<FixedIslandElement>()
                .Select(element => element.IslandAsset)
                .Where(asset => !string.IsNullOrWhiteSpace(asset.CustomSourceDirectory))
                .GroupBy(asset => asset.FilePath, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(asset => asset.FilePath, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static long GetCustomIslandGuidRangeStart(SessionAsset session)
        {
            if (!long.TryParse(Settings.Instance.CustomIslandGuid, out long baseGuid) || baseGuid <= 0)
            {
                throw new InvalidOperationException(
                    "A valid positive numeric GUID must be configured in Settings > GUID Management before exporting a map with custom islands.");
            }

            int regionOffset = session.GUID switch
            {
                SessionAsset.SESSION_OLDWORLD_GUID => 0,
                SessionAsset.SESSION_NEWWORLD_GUID => 25,
                SessionAsset.SESSION_SUNKENTREASURES_GUID => 50,
                SessionAsset.SESSION_ARCTIC_GUID => 75,
                SessionAsset.SESSION_ENBESA_GUID => 100,
                _ => throw new InvalidOperationException(
                    $"Custom-island GUID allocation is not supported for session GUID {session.GUID}.")
            };

            if (baseGuid > long.MaxValue - regionOffset - 24)
                throw new InvalidOperationException("The configured custom-island GUID is too large.");

            return baseGuid + regionOffset;
        }

        private Queue<long> GetFreeCustomIslandGuids(long rangeStart, ISet<long> additionallyUsedGuids)
        {
            Queue<long> freeGuids = new();
            for (long guid = rangeStart; guid <= rangeStart + 24; guid++)
            {
                bool existsInGameData = _assetRepository.TryGet(guid, out StandardAsset? _);
                if (!existsInGameData && !additionallyUsedGuids.Contains(guid))
                    freeGuids.Enqueue(guid);
            }
            return freeGuids;
        }

        private static string GetIslandRegionId(SessionAsset session)
        {
            return session.GUID switch
            {
                SessionAsset.SESSION_OLDWORLD_GUID => "Moderate",
                SessionAsset.SESSION_NEWWORLD_GUID => "Colony01",
                SessionAsset.SESSION_SUNKENTREASURES_GUID => "Moderate",
                SessionAsset.SESSION_ARCTIC_GUID => "Arctic",
                SessionAsset.SESSION_ENBESA_GUID => "Africa",
                _ => throw new InvalidOperationException(
                    $"Cannot determine IslandRegion for session GUID {session.GUID}.")
            };
        }

        private static XElement CreateCustomIslandModOp(long guid, string mapName, string filePath, string islandRegion)
        {
            return new XElement("ModOp",
                new XAttribute("GUID", "116676"),
                new XAttribute("Type", "addNextSibling"),
                new XElement("Asset",
                    new XElement("Template", "RandomIsland"),
                    new XElement("Values",
                        new XElement("Standard",
                            new XElement("GUID", guid),
                            new XElement("Name", mapName)),
                        new XElement("RandomIsland",
                            new XElement("FilePath", filePath),
                            new XElement("IslandRegion", islandRegion),
                            new XElement("IslandDifficulty", "Normal;Hard"),
                            new XElement("IslandType", "Normal;Starter")))));
        }


        private static async Task WriteModinfoJson(Mod mod, string modPath)
        {
            string modDescription;
            if (mod.MapTemplate.Session == SessionAsset.OldWorld)
                modDescription = $"Select Map Type '{mod.Name}' to play this map.\n"
                               + $"World and island sizes are fixed.\n\n";
            else
                modDescription = $"This mod will automatically replace {mod.MapTemplate.Session.DisplayName}.\n\n";

            modDescription += $"Note:\n" +
                              $"- Do not rename the mod folder. It will lead to a loading screen freeze.\n" +
                              $"- You can combine map mods as long as they do not replace the same session and map type.\n" +
                              $"\n" +
                              $"This mod has been created with the {App.Title}.\n" +
                              $"You can download the editor at:\nhttps://github.com/anno-mods/AnnoMapEditor/releases/latest";

            Modinfo modinfo = new()
            {
                Version = "1",
                ModID = string.IsNullOrEmpty(mod.Id) ? $"ame_{MakeSafeName(mod.Name)}_{Guid.NewGuid().ToString().Split('-').FirstOrDefault("")}" : mod.Id,
                ModName = new(mod.Name),
                Category = new("Map"),
                Description = new(modDescription),
                CreatorName = App.TitleShort,
                CreatorContact = "https://github.com/anno-mods/AnnoMapEditor"
            };

            string modinfoPath = Path.Combine(modPath, "modinfo.json");

            using StreamWriter writer = new(File.Create(modinfoPath));
            await writer.WriteAsync(JsonConvert.SerializeObject(modinfo, Newtonsoft.Json.Formatting.Indented));
        }

        private static void WriteModOpReplaceTemplateFilename(StringBuilder sb, long mapTemplateGuid, string templateFilename, string? enlargedTemplateFilename)
        {
            sb.Append($"  <ModOp GUID=\"{mapTemplateGuid}\" Type=\"merge\" Path=\"/Values/MapTemplate\">\n")
              .Append($"    <TemplateFilename>{templateFilename.Replace('\\', '/')}</TemplateFilename>\n");

            if (enlargedTemplateFilename != null)
                sb.Append($"    <EnlargedTemplateFilename>{enlargedTemplateFilename.Replace('\\', '/')}</EnlargedTemplateFilename>\n");

            sb.Append($"  </ModOp>\n");
        }

        private static string GetEnlargedTemplatePath(string templatePath)
        {
            string filename = Path.GetFileNameWithoutExtension(templatePath) + "_enlarged.a7t";
            return Path.Combine(Path.GetDirectoryName(templatePath)!, filename);
        }

        private static void CopyTemplateFiles(string a7tPath, string a7tinfoPath, string a7tePath, string targetA7tPath)
        {
            File.Copy(a7tPath, targetA7tPath);
            File.Copy(a7tinfoPath, Path.ChangeExtension(targetA7tPath, "a7tinfo"));
            File.Copy(a7tePath, Path.ChangeExtension(targetA7tPath, "a7te"));
        }


        private static void WriteA7T(Mod mod, string a7tPath)
        {
            using MemoryStream nestedDataStream = new();

            // If the session has existing Gamedata, it must be updated to reflect all changes made within the AME.
            (int x, int y, int size) playableArea = (mod.MapTemplate.PlayableArea.X, mod.MapTemplate.PlayableArea.Y, mod.MapTemplate.PlayableArea.Width);
            Gamedata gameDataItem = new(mod.MapTemplate.Size.X, playableArea, mod.MapTemplate.Session.Region.Ambiente!, true);

            //Create actual a7t File
            FileDBDocumentSerializer serializer = new(new() { Version = FileDBDocumentVersion.Version1 });
            IFileDBDocument generatedFileDB = serializer.WriteObjectStructureToFileDBDocument(gameDataItem);

            using MemoryStream fileDbStream = new();

            DocumentWriter gamedataDocWriter = new();
            gamedataDocWriter.WriteFileDBToStream(generatedFileDB, fileDbStream);

            if (fileDbStream.Position > 0)
            {
                fileDbStream.Seek(0, SeekOrigin.Begin);
            }

            RDABlockCreator.FileType_CompressedExtensions.Add(".data");

            using (RDAReader rdaReader = new())
            using (BinaryReader reader = new(fileDbStream))
            {
                RDAFolder rdaFolder = new(FileHeader.Version.Version_2_2);

                rdaReader.rdaFolder = rdaFolder;
                DirEntry gamedataFileDirEntry = new()
                {
                    filename = RDAFile.FileNameToRDAFileName("gamedata.data", ""),
                    offset = 0,
                    compressed = (ulong)fileDbStream.Length,
                    filesize = (ulong)fileDbStream.Length,
                    timestamp = RDAExplorer.Misc.DateTimeExtension.ToTimeStamp(DateTime.Now),
                };

                BlockInfo gamedataFileBlockInfo = new()
                {
                    flags = 0,
                    fileCount = 1,
                    directorySize = (ulong)fileDbStream.Length,
                    decompressedSize = (ulong)fileDbStream.Length,
                    nextBlock = 0
                };

                RDAFile rdaFile = RDAFile.FromUnmanaged(FileHeader.Version.Version_2_2, gamedataFileDirEntry, gamedataFileBlockInfo, reader, null);
                rdaFolder.AddFiles(new List<RDAFile>() { rdaFile });
                RDAWriter writer = new(rdaFolder);
                bool compress = true;
                writer.Write(a7tPath, FileHeader.Version.Version_2_2, compress, rdaReader, null);

            }

            RDABlockCreator.FileType_CompressedExtensions.Remove(".data");
        }

        private static void WriteA7te(int mapRadius, string a7tePath)
        {
            AnnoEditorLevel a7te = new(mapRadius);

            XmlSerializer a7teSerializer = new(typeof(AnnoEditorLevel));
            XmlWriterSettings xmlSettings = new() { Indent = true, IndentChars = "  ", OmitXmlDeclaration = true, Async = true };
            XmlSerializerNamespaces noNamespaces = new(new XmlQualifiedName[] { XmlQualifiedName.Empty });

            using StreamWriter streamWriter = new(a7tePath, false, Encoding.UTF8);
            using XmlWriter xmlWriter = XmlWriter.Create(streamWriter, xmlSettings);

            a7teSerializer.Serialize(xmlWriter, a7te, noNamespaces);
        }

        private static async Task WriteLanguageXml(string modPath, string name, string guid)
        {
            string[] languages = new string[] { "chinese", "english", "french", "german", "italian", "japanese", "korean", "polish", "russian", "spanish", "taiwanese" };

            foreach (var language in languages)
            {
                string languageXmlPath = Path.Combine(modPath, $@"data\config\gui\texts_{language}.xml");
                string? languageXmlDir = Path.GetDirectoryName(languageXmlPath);
                if (languageXmlDir is not null)
                    Directory.CreateDirectory(languageXmlDir);

                string content =
                    $"<ModOps>\n" +
                    $"  <ModOp Type=\"replace\" Path=\"//TextExport/Texts/Text[GUID='{guid}']/Text\">\n" +
                    $"    <Text>{name}</Text>\n" +
                    $"  </ModOp>\n" +
                    $"</ModOps>\n";

                using StreamWriter writer = new(File.Create(languageXmlPath));
                await writer.WriteAsync(content);
            }
        }


        private static string MakeSafeName(string unsafeName) => new Regex(@"\W").Replace(unsafeName, "_").ToLower();
    }
}
