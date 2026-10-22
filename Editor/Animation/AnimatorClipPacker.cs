using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Vacuous.EditorTools
{
    // Moves standalone animation clips into an Animator Controller as sub-assets, and back out again.
    public static class AnimatorClipPacker
    {
        private const string MenuRoot = "Tools/Vacuous/Animation/";

        #region Pack

        [MenuItem(MenuRoot + "Pack Selected Clips Into Controller")]
        private static void Pack()
        {
            AnimatorController controller = Selection.activeObject as AnimatorController;
            if (controller == null)
            {
                Debug.LogError("[AnimatorClipPacker] Select an Animator Controller first, then also select the clips to pack.");
                return;
            }

            int packed = 0;

            foreach (Object selected in Selection.objects)
            {
                if (selected == controller)
                    continue;
                if (selected is not AnimationClip source || AssetDatabase.IsSubAsset(source))
                    continue;

                AnimationClip clone = new AnimationClip();
                EditorUtility.CopySerialized(source, clone);
                clone.name = source.name;

                AssetDatabase.AddObjectToAsset(clone, controller);
                AssetDatabase.SaveAssets();
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(source));
                packed++;
            }

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(controller));
            Debug.Log($"[AnimatorClipPacker] Packed {packed} clips into {controller.name}.");
        }

        #endregion

        #region Unpack

        [MenuItem(MenuRoot + "Unpack Clips From Controller")]
        private static void Unpack()
        {
            AnimatorController controller = Selection.activeObject as AnimatorController;
            if (controller == null)
            {
                Debug.LogError("[AnimatorClipPacker] Select an Animator Controller to unpack.");
                return;
            }

            string controllerPath = AssetDatabase.GetAssetPath(controller);
            Dictionary<AnimationClip, AnimationClip> exported = ExportPackedClips(controllerPath);

            if (exported.Count > 0)
            {
                foreach (AnimatorControllerLayer layer in controller.layers)
                    RewireStateMachine(layer.stateMachine, exported);
            }

            foreach (AnimationClip packed in exported.Keys)
                AssetDatabase.RemoveObjectFromAsset(packed);

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(controllerPath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"[AnimatorClipPacker] Unpacked {exported.Count} clips next to {controller.name}.");
        }

        private static Dictionary<AnimationClip, AnimationClip> ExportPackedClips(string controllerPath)
        {
            string folder = Path.GetDirectoryName(controllerPath);
            Dictionary<AnimationClip, AnimationClip> exported = new Dictionary<AnimationClip, AnimationClip>();

            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(controllerPath))
            {
                if (asset is not AnimationClip clip || !AssetDatabase.IsSubAsset(clip))
                    continue;

                string path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folder, $"{clip.name}.anim"));
                AnimationClip clone = Object.Instantiate(clip);
                AssetDatabase.CreateAsset(clone, path);
                exported[clip] = clone;
            }

            return exported;
        }

        private static void RewireStateMachine(AnimatorStateMachine stateMachine, Dictionary<AnimationClip, AnimationClip> replacements)
        {
            foreach (ChildAnimatorState child in stateMachine.states)
            {
                AnimatorState state = child.state;
                if (state == null)
                    continue;

                state.motion = ReplaceMotion(state.motion, replacements);
                EditorUtility.SetDirty(state);
            }

            foreach (ChildAnimatorStateMachine child in stateMachine.stateMachines)
            {
                if (child.stateMachine != null)
                    RewireStateMachine(child.stateMachine, replacements);
            }
        }

        private static Motion ReplaceMotion(Motion motion, Dictionary<AnimationClip, AnimationClip> replacements)
        {
            if (motion == null)
                return null;

            if (motion is AnimationClip clip && replacements.TryGetValue(clip, out AnimationClip replacement))
                return replacement;

            if (motion is not BlendTree blendTree)
                return motion;

            ChildMotion[] children = blendTree.children;
            bool hasChanged = false;

            for (int i = 0; i < children.Length; i++)
            {
                Motion current = children[i].motion;
                Motion updated = ReplaceMotion(current, replacements);
                if (ReferenceEquals(updated, current))
                    continue;

                children[i].motion = updated;
                hasChanged = true;
            }

            if (hasChanged)
            {
                // BlendTree.children returns a copy, so the edited array has to be assigned back.
                blendTree.children = children;
                EditorUtility.SetDirty(blendTree);
            }

            return blendTree;
        }

        #endregion
    }
}
