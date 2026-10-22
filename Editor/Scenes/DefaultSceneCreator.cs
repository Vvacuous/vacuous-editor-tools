using UnityEditor;
using UnityEngine;

namespace Vacuous.EditorTools
{
    // Adds Create > Scene > Scene (Default Template), which makes a new scene from the template chosen in Project Settings.
    public static class DefaultSceneCreator
    {
        [MenuItem("Assets/Create/Scene/Scene (Default Template)", false, 0)]
        private static void CreateScene()
        {
            VacuousEditorToolsSettings settings = VacuousEditorToolsSettings.instance;
            if (settings.DefaultSceneTemplate == null)
            {
                Debug.LogError("[DefaultSceneCreator] No default scene template set. Choose one in Project Settings > Vacuous Editor Tools.");
                SettingsService.OpenProjectSettings("Project/Vacuous Editor Tools");
                return;
            }

            Texture2D icon = EditorGUIUtility.IconContent("SceneAsset Icon").image as Texture2D;
            DefaultSceneNameEditAction action = ScriptableObject.CreateInstance<DefaultSceneNameEditAction>();
            string fileName = settings.NewSceneName + ".unity";

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(default(EntityId), action, fileName, icon, null);
        }
    }
}
