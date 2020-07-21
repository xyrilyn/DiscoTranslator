using BepInEx;
using BepInEx.Configuration;
using DiscoTranslator.Translation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using HarmonyLib;
using System.Reflection;
using Language.Lua;

namespace DiscoTranslator
{
    [Obfuscation(Feature="renaming", Exclude=true)]
    [BepInPlugin("akintos.DiscoTranslator", "Disco Translator", "0.2.0.0")]
    [BepInProcess("disco.exe")]
    public class DiscoTranslatorPlugin : BaseUnityPlugin
    {
        private const string PLUGIN_DIR = "DiscoTranslator";

        private readonly ConfigEntry<bool> enableStringNo;
        private readonly ConfigEntry<KeyboardShortcut> toggleKey, reloadKey;

        public DiscoTranslatorPlugin() : base()
        {
            enableStringNo = Config.Bind("Translation", "Show translation number", true, "Wheather or not to display string number");
            TranslationManager.EnableStringNumber = enableStringNo.Value;
            
            toggleKey = Config.Bind("Hotkeys", "Toggle interface", new KeyboardShortcut(KeyCode.T, KeyCode.LeftAlt));
            reloadKey = Config.Bind("Hotkeys", "Reload translation", new KeyboardShortcut(KeyCode.R, KeyCode.LeftAlt));

            var harmony = BepInEx.Harmony.HarmonyWrapper.PatchAll(typeof(DiscoTranslatorPlugin).Assembly);

            TranslationManager.LogEvent += Logger.Log;
        }

        public void Awake()
        {
            LoadTranslation();
            ImageManager.LoadImages(Path.Combine(Paths.PluginPath, PLUGIN_DIR, "Images"));
        }

        public void Update()
        {
            if (reloadKey.Value.IsDown())
            {
                TranslationManager.ReloadAllSources();
                Logger.LogInfo("Reload sources");
            }

            if (toggleKey.Value.IsDown())
            {
                showingUI = !showingUI;
            }
        }

        private void ExportCatalog()
        {
            string catalog_dir = Path.Combine(GetPluginDir(), "Catalog");
            if (!Directory.Exists(catalog_dir))
                Directory.CreateDirectory(catalog_dir);

            CatalogExporter.ExportAll(catalog_dir);
            Logger.LogInfo("Export translation");
        }

        private IDictionary<string, string> GetStringNumberConfig()
        {
            string configPath = Path.Combine(GetPluginDir(), "StringNumber.cfg");
            var lines = File.ReadAllLines(configPath);

            var data = new Dictionary<string, string>();
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.StartsWith("#"))
                    continue;
                var parts = line.Trim().Split('=');
                if (parts.Length != 2)
                    continue;
                data.Add(parts[0].Trim().ToLowerInvariant(), parts[1].Trim());
            }
            return data;
        }

        private void LoadTranslation()
        {
            string translation_dir = Path.Combine(GetPluginDir(), "Translation");
            if (!Directory.Exists(translation_dir))
            {
                Logger.LogError("Translation directory does not exist : " + translation_dir);
                return;
            }

            var stringNumberConfig = GetStringNumberConfig();

            foreach (var filePath in Directory.GetFiles(translation_dir))
            {
                if (Path.GetExtension(filePath) != ".po") continue;
                
                var source = new POTranslationSource(filePath, true);
                var poName = Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant();

                if (stringNumberConfig.TryGetValue(poName, out string prefix))
                {
                    source.EnableStrNo = true;
                    source.StrNoPrefix = prefix;
                }

                TranslationManager.AddSource(source);
                Logger.LogInfo("Added translation source " + Path.GetFileName(filePath));
            }

            var imageNameDict = new Dictionary<string, string>();

            var buttonImageSource = Resources.Load<I2.Loc.LanguageSourceAsset>("Languages/ButtonsImagesLanguages").mSource;
            int englishIndex = buttonImageSource.GetLanguageIndex("English");

            foreach (var term in buttonImageSource.mTerms)
                imageNameDict.Add(term.Term, term.Languages[englishIndex]);

            var imageSource = new DictTranslationSource(imageNameDict);
            TranslationManager.AddSource(imageSource);
        }

        #region UI
        private Rect UI = CalculateWindowRect(300, 300, 0.15f, 0.2f);
        private bool prettyPrint = true;

        bool showingUI = false;
        public void OnGUI()
        {
            if (showingUI)
                UI = GUI.Window(this.Info.Metadata.Name.GetHashCode(), UI, WindowFunction, this.Info.Metadata.Name);
        }

        void WindowFunction(int windowID)
        {
            TranslationManager.EnableTranslation = GUILayout.Toggle(TranslationManager.EnableTranslation, "Enable translation");
            TranslationManager.EnableStringNumber = enableStringNo.Value = GUILayout.Toggle(TranslationManager.EnableStringNumber, "Display string number");

            if (GUILayout.Button("Reload translations"))
                TranslationManager.ReloadAllSources();

            if (GUILayout.Button("Export catalog"))
                ExportCatalog();

            GUILayout.Space(15);
            
            if (GUILayout.Button("Export dialogue database"))
            {
                var db = Resources.FindObjectsOfTypeAll<PixelCrushers.DialogueSystem.DialogueDatabase>()[0];
                var json = UnityEngine.JsonUtility.ToJson(db, prettyPrint);
                File.WriteAllText(BepInEx.Utility.CombinePaths(Paths.PluginPath, PLUGIN_DIR, "database.json"), json);
            }
            prettyPrint = GUILayout.Toggle(prettyPrint, "Pretty print(format)  JSON output");
            GUILayout.Space(15);

            if (GUILayout.Button("Export images"))
                ImageManager.ExportImages(Path.Combine(GetPluginDir(), "OriginalImages"));

            GUILayout.Space(15);
            GUILayout.Label($"Press {toggleKey.Value} to close this window.");
            GUI.DragWindow();
        }

        private static Rect CalculateWindowRect(int width, int height, float xpos, float ypos)
        {
            var offsetX = Mathf.RoundToInt(Screen.width * xpos - width / 2);
            var offsetY = Mathf.RoundToInt(Screen.height * ypos - height /2);
            return new Rect(offsetX, offsetY, width, height);
        }
        #endregion

        public static string GetPluginDir()
        {
            return Path.Combine(Paths.PluginPath, PLUGIN_DIR);
        }
    }
}
