using System;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using com.ktgame.core.editor;
using Object = UnityEngine.Object;

namespace com.ktgame.save.editor
{
    [InitializeOnLoad]
    public class SaveLoadEditorModule : IMenuTreeExtension
    {
        static SaveLoadEditorModule()
        {
            var module = new SaveLoadEditorModule();
            MenuTreeExtensionRegistry.Register(module);
        }
        
        public void BuildMenu(OdinMenuTree tree)
        {
            tree.Add("Save & Load", new SaveLoadEditor(), SdfIconType.SaveFill);
        }
    }

    public class SaveLoadEditor
    {
        [Title("Save & Load Configuration", "Manage your save data and inspect runtime models", TitleAlignments.Centered)]
        [InfoBox("Connect to the running game to inspect and modify live data models. Any changes made here will reflect in-game immediately.", InfoMessageType.Info)]
        
        [PropertyOrder(1)]
        [PropertySpace(SpaceBefore = 10, SpaceAfter = 10)]
        [ShowInInspector, HideLabel, ReadOnly, DisplayAsString]
        public string Status => Application.isPlaying ? "✅ System is Active - Ready for connection" : "❌ Play Mode Required";

        [PropertyOrder(2)]
        [ShowIf("@UnityEngine.Application.isPlaying && !IsConnected")]
        [Button("Connect To Live Game", ButtonSizes.Large, Icon = SdfIconType.PlugFill)]
        [GUIColor(0.2f, 0.8f, 0.2f)]
        public void Connect()
        {
            // Use reflection to find SaveLoadManager in the game assembly without a hard dependency
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => {
                    try { return a.GetTypes(); } catch { return new Type[0]; }
                })
                .FirstOrDefault(t => t.Name == "SaveLoadManager" && typeof(MonoBehaviour).IsAssignableFrom(t));
                
            if (type != null)
            {
                _activeManager = Object.FindObjectOfType(type);
            }
            
            if (_activeManager == null)
            {
                Debug.LogWarning("SaveLoadManager not found in the current scene.");
            }
        }

        private bool IsConnected => _activeManager != null;

        [PropertyOrder(3)]
        [ShowIf("IsConnected")]
        [PropertySpace(SpaceBefore = 20)]
        [Title("Live Data Inspection", "Edit variables directly inside the memory", TitleAlignments.Centered)]
        [ShowInInspector, HideLabel, HideReferenceObjectPicker]
        [EnableGUI]
        [InlineEditor(InlineEditorObjectFieldModes.CompletelyHidden)]
        private Object _activeManager;

        [PropertyOrder(4)]
        [BoxGroup("Save Actions", CenterLabel = true)]
        [HorizontalGroup("Save Actions/Buttons")]
        [Button("Delete All Saves", ButtonSizes.Large, Icon = SdfIconType.TrashFill)]
        [GUIColor(1f, 0.2f, 0.2f)]
        public void DeleteAllSaves()
        {
            if (EditorUtility.DisplayDialog("Delete All Saves", "Are you sure you want to wipe all save data? This cannot be undone.", "Yes, Wipe Everything", "Cancel"))
            {
                if (_activeManager != null)
                {
                    InvokeMethod("DeleteAll");
                }
                else
                {
                    PlayerPrefs.DeleteAll();
                    PlayerPrefs.Save();
                }
                Debug.Log("All saves deleted from device.");
            }
        }

        [PropertyOrder(5)]
        [BoxGroup("Save Actions")]
        [HorizontalGroup("Save Actions/Buttons")]
        [Button("Show Raw JSON", ButtonSizes.Large, Icon = SdfIconType.CodeSlash)]
        [GUIColor(0.2f, 0.6f, 1f)]
        public void ToggleRawJson()
        {
            _showRawJson = !_showRawJson;
            if (_showRawJson && _activeManager != null)
            {
                var rawStr = InvokeMethod("LoadRawFromSaveFile") as string;
                try
                {
                    var parsedJson = Newtonsoft.Json.JsonConvert.DeserializeObject(rawStr);
                    RawJsonData = Newtonsoft.Json.JsonConvert.SerializeObject(parsedJson, Newtonsoft.Json.Formatting.Indented);
                }
                catch
                {
                    RawJsonData = rawStr;
                }
            }
        }

        [PropertyOrder(6)]
        [BoxGroup("Save Actions")]
        [HorizontalGroup("Save Actions/Buttons")]
        [Button("Open Folder", ButtonSizes.Large, Icon = SdfIconType.FolderFill)]
        [GUIColor(1f, 0.8f, 0.2f)]
        public void OpenSaveFolder()
        {
            var path = Application.persistentDataPath;
            EditorUtility.RevealInFinder(path);
            Debug.Log($"Opening save folder: {path}");
        }

        [PropertyOrder(7)]
        [ShowIf("_showRawJson")]
        [HideInInspector]
        private bool _showRawJson;

        [PropertyOrder(8)]
        [ShowIf("_showRawJson")]
        [Title("Raw JSON Data", "Modify and click Write to force overwrite", TitleAlignments.Centered)]
        [ShowInInspector, HideLabel, TextArea(15, 30)]
        public string RawJsonData { get; set; }

        [PropertyOrder(9)]
        [ShowIf("_showRawJson")]
        [Button("Force Write JSON", ButtonSizes.Large, Icon = SdfIconType.PencilFill)]
        [GUIColor(1f, 0.6f, 0f)]
        public void ForceWriteJson()
        {
            if (_activeManager != null)
            {
                InvokeMethod("ShortCutWriteRawToSaveFile", RawJsonData);
                Debug.Log("Forced overwrite from JSON.");
            }
        }
        
        private object InvokeMethod(string methodName, params object[] parameters)
        {
            if (_activeManager == null) return null;
            var method = _activeManager.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
            {
                return method.Invoke(_activeManager, parameters);
            }
            Debug.LogWarning($"Method {methodName} not found on {_activeManager.GetType().Name}");
            return null;
        }
    }
}
