using UnityEditor;
using UnityEditor.SceneTemplate;

namespace Vacuous.EditorTools
{
    // Draws the Project Settings > Vacuous Editor Tools page.
    public static class VacuousEditorToolsSettingsProvider
    {
        private const string SettingsPath = "Project/Vacuous Editor Tools";

        [SettingsProvider]
        private static SettingsProvider Create()
        {
            return new SettingsProvider(SettingsPath, SettingsScope.Project)
            {
                label = "Vacuous Editor Tools",
                guiHandler = _ => DrawGui(),
                keywords = new[] { "scene", "template", "source", "blend", "models" }
            };
        }

        private static void DrawGui()
        {
            VacuousEditorToolsSettings settings = VacuousEditorToolsSettings.instance;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Scenes", EditorStyles.boldLabel);
            SceneTemplateAsset template = (SceneTemplateAsset)EditorGUILayout.ObjectField("Default Scene Template", settings.DefaultSceneTemplate, typeof(SceneTemplateAsset), false);
            string sceneName = EditorGUILayout.TextField("New Scene Name", settings.NewSceneName);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Source Art", EditorStyles.boldLabel);
            string modelsRoot = EditorGUILayout.TextField("Models Folder", settings.ModelsRoot);
            string sourceArtRoot = EditorGUILayout.TextField("Source Art Folder", settings.SourceArtRoot);
            string extension = EditorGUILayout.TextField("Source File Extension", settings.SourceFileExtension);
            EditorGUILayout.HelpBox("Models Folder is inside Assets; Source Art Folder is relative to the project root. A model at <Models Folder>/Props/mesh_crate.fbx opens <Source Art Folder>/Props/mesh_crate" + settings.SourceFileExtension + ".", MessageType.None);

            if (!EditorGUI.EndChangeCheck())
                return;

            settings.SetDefaultSceneTemplate(template);
            settings.SetNewSceneName(sceneName);
            settings.SetModelsRoot(modelsRoot);
            settings.SetSourceArtRoot(sourceArtRoot);
            settings.SetSourceFileExtension(extension);
            settings.SaveSettings();
        }
    }
}
