using UnityEditor;
using UnityEditor.SceneTemplate;
using UnityEngine;

namespace Vacuous.EditorTools
{
    // Per-project settings for the tools, stored in ProjectSettings so the whole team shares them.
    [FilePath("ProjectSettings/VacuousEditorToolsSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class VacuousEditorToolsSettings : ScriptableSingleton<VacuousEditorToolsSettings>
    {
        #region Scenes

        [SerializeField] private string defaultSceneTemplateGuid = "";
        [SerializeField] private string newSceneName = "scene_new";

        #endregion

        #region Source Art

        [SerializeField] private string modelsRoot = "Assets/_Project/Graphics/Models";
        [SerializeField] private string sourceArtRoot = "SourceArt";
        [SerializeField] private string sourceFileExtension = ".blend";

        #endregion

        public string NewSceneName => newSceneName;
        public string ModelsRoot => modelsRoot.TrimEnd('/');
        public string SourceArtRoot => sourceArtRoot.TrimEnd('/');
        public string SourceFileExtension => sourceFileExtension.StartsWith(".") ? sourceFileExtension : "." + sourceFileExtension;

        public SceneTemplateAsset DefaultSceneTemplate
        {
            get
            {
                if (string.IsNullOrEmpty(defaultSceneTemplateGuid))
                    return null;

                string path = AssetDatabase.GUIDToAssetPath(defaultSceneTemplateGuid);
                return AssetDatabase.LoadAssetAtPath<SceneTemplateAsset>(path);
            }
        }

        public void SetDefaultSceneTemplate(SceneTemplateAsset template)
        {
            defaultSceneTemplateGuid = template != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(template)) : "";
        }

        public void SetNewSceneName(string value) => newSceneName = value;
        public void SetModelsRoot(string value) => modelsRoot = value;
        public void SetSourceArtRoot(string value) => sourceArtRoot = value;
        public void SetSourceFileExtension(string value) => sourceFileExtension = value;

        public void SaveSettings() => Save(true);
    }
}
