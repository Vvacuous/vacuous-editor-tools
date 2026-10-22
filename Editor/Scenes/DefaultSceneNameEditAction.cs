using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.SceneTemplate;
using UnityEngine;

namespace Vacuous.EditorTools
{
    // Creates the scene from the default template once the user confirms the new scene's name in the Project window.
    public class DefaultSceneNameEditAction : AssetCreationEndAction
    {
        public override void Action(EntityId instanceId, string pathName, string resourceFile)
        {
            SceneTemplateAsset template = VacuousEditorToolsSettings.instance.DefaultSceneTemplate;
            if (template == null)
                return;

            if (!pathName.EndsWith(".unity"))
                pathName += ".unity";

            InstantiationResult result = SceneTemplateService.Instantiate(template, false, pathName);
            if (result == null)
                return;

            SceneAsset created = AssetDatabase.LoadAssetAtPath<SceneAsset>(pathName);
            ProjectWindowUtil.ShowCreatedAsset(created);
        }
    }
}
