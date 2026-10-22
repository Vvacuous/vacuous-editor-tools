using System;
using System.Reflection;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Vacuous.EditorTools
{
    // Draws a rule-of-thirds grid over the Game view through the Cinemachine Brain's editor-only GUI hook, so nothing reaches a scene or a build.
    [InitializeOnLoad]
    public static class CameraThirdsGuide
    {
        private const string MenuPath = "Tools/Vacuous/Camera/Rule of Thirds Guide";
        private const string IsEnabledPrefKey = "Vacuous.EditorTools.CameraThirdsGuide.IsEnabled";
        private const float LineThickness = 1f;
        private const float OutlineThickness = 3f;

        private static readonly Color LineColor = new Color(1f, 1f, 1f, 0.75f);
        private static readonly Color OutlineColor = new Color(0f, 0f, 0f, 0.35f);

        // CinemachineDebug is internal to the Cinemachine assembly, so its handler list is reached by reflection.
        private static readonly FieldInfo HandlersField = typeof(CinemachineBrain).Assembly
            .GetType("Unity.Cinemachine.CinemachineDebug")
            ?.GetField("OnGUIHandlers", BindingFlags.Public | BindingFlags.Static);

        private static bool IsEnabled
        {
            get => EditorPrefs.GetBool(IsEnabledPrefKey, false);
            set => EditorPrefs.SetBool(IsEnabledPrefKey, value);
        }

        static CameraThirdsGuide()
        {
            // Cinemachine clears its handler list whenever play mode starts, so the hook is re-added after every change.
            EditorApplication.playModeStateChanged += _ => Hook();
            Hook();
        }

        [MenuItem(MenuPath)]
        private static void Toggle()
        {
            IsEnabled = !IsEnabled;
            Hook();
            InternalEditorUtility.RepaintAllViews();
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateToggle()
        {
            Menu.SetChecked(MenuPath, IsEnabled);
            return true;
        }

        private static void Hook()
        {
            if (HandlersField == null)
            {
                if (IsEnabled)
                    Debug.LogWarning("[CameraThirdsGuide] Cinemachine no longer exposes CinemachineDebug.OnGUIHandlers, so the grid cannot be drawn.");

                return;
            }

            Action<CinemachineBrain> handlers = (Action<CinemachineBrain>)HandlersField.GetValue(null);
            handlers -= Draw;

            if (IsEnabled)
                handlers += Draw;

            HandlersField.SetValue(null, handlers);
        }

        private static void Draw(CinemachineBrain brain)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Camera outputCamera = brain.OutputCamera;
            if (outputCamera == null)
                return;

            // pixelRect counts up from the bottom of the screen and IMGUI counts down from the top.
            Rect frame = outputCamera.pixelRect;
            frame.y = Screen.height - frame.yMax;

            DrawGrid(frame, OutlineThickness, OutlineColor);
            DrawGrid(frame, LineThickness, LineColor);
        }

        private static void DrawGrid(Rect frame, float thickness, Color color)
        {
            for (int i = 1; i <= 2; i++)
            {
                float x = frame.x + frame.width * i / 3f;
                float y = frame.y + frame.height * i / 3f;

                DrawRect(new Rect(x - thickness * 0.5f, frame.y, thickness, frame.height), color);
                DrawRect(new Rect(frame.x, y - thickness * 0.5f, frame.width, thickness), color);
            }
        }

        private static void DrawRect(Rect rect, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = previousColor;
        }
    }
}
