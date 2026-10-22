using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Vacuous.EditorTools
{
    // Poses a character on any frame of a clip and bakes a stripped, static copy for screenshots and Recorder captures.
    public class CharacterPoseBaker : EditorWindow
    {
        #region Fields

        private static readonly System.Type[] KeptTypes =
        {
            typeof(Transform),
            typeof(SkinnedMeshRenderer),
            typeof(MeshFilter),
            typeof(MeshRenderer),
            typeof(LODGroup)
        };

        [SerializeField] private GameObject target;
        [SerializeField] private AnimationClip clip;
        [SerializeField] private float sampleTime;
        [SerializeField] private bool shouldKeepRootInPlace = true;
        [SerializeField] private bool shouldStripColliders = true;

        private Animator _animator;
        private AnimationClip[] _clips = new AnimationClip[0];
        private Vector2 _clipScroll;

        private GameObject _snapshotOwner;
        private readonly List<Transform> _snapshotTransforms = new();
        private readonly List<Vector3> _snapshotPositions = new();
        private readonly List<Quaternion> _snapshotRotations = new();
        private readonly List<Vector3> _snapshotScales = new();

        #endregion

        #region Window

        [MenuItem("Tools/Vacuous/Characters/Pose Baker")]
        private static void Open()
        {
            GetWindow<CharacterPoseBaker>("Pose Baker").minSize = new Vector2(320f, 380f);
        }

        private void OnGUI()
        {
            DrawTargetSection();

            if (_animator == null)
            {
                EditorGUILayout.HelpBox("Select a character with an Animator in its hierarchy. SampleAnimation binds through the Animator, so a rig without one cannot be posed.", MessageType.Info);
                return;
            }

            DrawClipSection();

            if (clip == null)
                return;

            DrawTimeSection();
            DrawActionSection();
        }

        #endregion

        #region Drawing

        private void DrawTargetSection()
        {
            EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            target = (GameObject)EditorGUILayout.ObjectField("Character", target, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck())
                RebindTarget();

            if (GUILayout.Button("Use Selection"))
            {
                target = Selection.activeGameObject;
                RebindTarget();
            }

            if (_animator != null && _animator.gameObject != target)
                EditorGUILayout.LabelField("Animator", _animator.gameObject.name, EditorStyles.miniLabel);

            EditorGUILayout.Space();
        }

        private void DrawClipSection()
        {
            EditorGUILayout.LabelField("Clip", EditorStyles.boldLabel);
            clip = (AnimationClip)EditorGUILayout.ObjectField("Clip", clip, typeof(AnimationClip), false);

            if (_clips.Length == 0)
            {
                EditorGUILayout.LabelField("No clips found on the source model.", EditorStyles.miniLabel);
                EditorGUILayout.Space();
                return;
            }

            _clipScroll = EditorGUILayout.BeginScrollView(_clipScroll, GUILayout.Height(Mathf.Min(120f, _clips.Length * 20f + 4f)));
            foreach (AnimationClip found in _clips)
            {
                if (!GUILayout.Button(found.name, EditorStyles.miniButton))
                    continue;

                clip = found;
                sampleTime = 0f;
                SamplePose();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
        }

        private void DrawTimeSection()
        {
            EditorGUILayout.LabelField("Frame", EditorStyles.boldLabel);

            int lastFrame = Mathf.Max(0, Mathf.RoundToInt(clip.length * clip.frameRate));
            int currentFrame = Mathf.RoundToInt(sampleTime * clip.frameRate);

            EditorGUI.BeginChangeCheck();
            float newTime = EditorGUILayout.Slider("Time", sampleTime, 0f, clip.length);
            int newFrame = EditorGUILayout.IntSlider("Frame", currentFrame, 0, lastFrame);
            if (EditorGUI.EndChangeCheck())
            {
                sampleTime = newFrame != currentFrame ? newFrame / Mathf.Max(1f, clip.frameRate) : newTime;
                SamplePose();
            }

            EditorGUILayout.LabelField($"{clip.length:0.00}s at {clip.frameRate:0} fps", EditorStyles.miniLabel);
            EditorGUILayout.Space();
        }

        private void DrawActionSection()
        {
            EditorGUILayout.LabelField("Bake", EditorStyles.boldLabel);
            shouldKeepRootInPlace = EditorGUILayout.Toggle("Keep Root In Place", shouldKeepRootInPlace);
            shouldStripColliders = EditorGUILayout.Toggle("Strip Colliders", shouldStripColliders);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Re-sample"))
                SamplePose();

            using (new EditorGUI.DisabledScope(_snapshotOwner == null))
            {
                if (GUILayout.Button("Restore Pose"))
                    RestorePose();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Bake Mannequin", GUILayout.Height(28f)))
                BakeMannequin();

            EditorGUILayout.HelpBox("Bake duplicates the character, unpacks it from its prefab, then strips the Animator, CharacterController and every script. What is left cannot move in play mode, so Recorder captures the exact pose shown here.", MessageType.None);
        }

        #endregion

        #region Posing

        private void RebindTarget()
        {
            _animator = target != null ? target.GetComponentInChildren<Animator>(true) : null;
            _clips = _animator != null ? FindClips(_animator) : new AnimationClip[0];
            ClearSnapshot();
        }

        private void SamplePose()
        {
            if (_animator == null || clip == null)
                return;

            GameObject root = _animator.gameObject;
            EnsureSnapshot(root);

            Undo.RegisterFullObjectHierarchyUndo(root, "Sample Pose");

            Transform rootTransform = root.transform;
            Vector3 rootPosition = rootTransform.localPosition;
            Quaternion rootRotation = rootTransform.localRotation;

            clip.SampleAnimation(root, sampleTime);

            // Root motion walks the character away from where it was placed.
            if (shouldKeepRootInPlace)
            {
                rootTransform.localPosition = rootPosition;
                rootTransform.localRotation = rootRotation;
            }

            MarkDirty(root);
        }

        private void RestorePose()
        {
            if (_snapshotOwner == null)
                return;

            Undo.RegisterFullObjectHierarchyUndo(_snapshotOwner, "Restore Pose");

            for (int i = 0; i < _snapshotTransforms.Count; i++)
            {
                Transform bone = _snapshotTransforms[i];
                if (bone == null)
                    continue;

                bone.localPosition = _snapshotPositions[i];
                bone.localRotation = _snapshotRotations[i];
                bone.localScale = _snapshotScales[i];
            }

            MarkDirty(_snapshotOwner);
        }

        private void EnsureSnapshot(GameObject root)
        {
            if (_snapshotOwner == root)
                return;

            ClearSnapshot();
            _snapshotOwner = root;

            foreach (Transform bone in root.GetComponentsInChildren<Transform>(true))
            {
                _snapshotTransforms.Add(bone);
                _snapshotPositions.Add(bone.localPosition);
                _snapshotRotations.Add(bone.localRotation);
                _snapshotScales.Add(bone.localScale);
            }
        }

        private void ClearSnapshot()
        {
            _snapshotOwner = null;
            _snapshotTransforms.Clear();
            _snapshotPositions.Clear();
            _snapshotRotations.Clear();
            _snapshotScales.Clear();
        }

        #endregion

        #region Baking

        private void BakeMannequin()
        {
            if (target == null || clip == null)
                return;

            GameObject copy = Instantiate(target);
            copy.name = BuildName();
            copy.transform.SetParent(target.transform.parent, false);
            copy.transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
            copy.transform.localScale = target.transform.localScale;

            if (PrefabUtility.IsPartOfPrefabInstance(copy))
                PrefabUtility.UnpackPrefabInstance(copy, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            Strip(copy);

            Undo.RegisterCreatedObjectUndo(copy, "Bake Mannequin");
            Selection.activeGameObject = copy;
            EditorGUIUtility.PingObject(copy);
            MarkDirty(copy);

            Debug.Log($"Baked mannequin {copy.name}", copy);
        }

        private void Strip(GameObject root)
        {
            // Scripts go first so nothing kept afterwards is still held by a RequireComponent attribute.
            foreach (MonoBehaviour script in root.GetComponentsInChildren<MonoBehaviour>(true).Reverse())
            {
                if (script != null)
                    DestroyImmediate(script);
            }

            foreach (Component component in root.GetComponentsInChildren<Component>(true).Reverse())
            {
                if (component == null || ShouldKeep(component))
                    continue;

                DestroyImmediate(component);
            }
        }

        private bool ShouldKeep(Component component)
        {
            if (!shouldStripColliders && component is Collider)
                return true;

            return KeptTypes.Any(kept => kept.IsInstanceOfType(component));
        }

        private string BuildName()
        {
            int frame = Mathf.RoundToInt(sampleTime * clip.frameRate);
            return $"mannequin_{target.name}_{clip.name}_f{frame:000}".ToLowerInvariant().Replace(' ', '_');
        }

        #endregion

        #region Helpers

        private static AnimationClip[] FindClips(Animator animator)
        {
            var found = new List<AnimationClip>();

            Object source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(animator.gameObject);
            string path = source != null ? AssetDatabase.GetAssetPath(source) : null;

            if (!string.IsNullOrEmpty(path))
                found.AddRange(AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>());

            if (animator.runtimeAnimatorController != null)
                found.AddRange(animator.runtimeAnimatorController.animationClips);

            return found
                .Where(candidate => candidate != null && !candidate.name.StartsWith("__preview__"))
                .Distinct()
                .OrderBy(candidate => candidate.name)
                .ToArray();
        }

        private static void MarkDirty(GameObject root)
        {
            EditorUtility.SetDirty(root);

            if (root.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(root.scene);

            SceneView.RepaintAll();
        }

        #endregion
    }
}
