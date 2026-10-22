using System;
using System.Reflection;
using UnityEditor;

namespace Vacuous.EditorTools
{
    // Toggles the lock on the focused Inspector with Alt+R.
    public static class InspectorLockMenu
    {
        [MenuItem("Tools/Vacuous/Workflow/Toggle Inspector Lock &r")]
        private static void ToggleLock()
        {
            // InspectorWindow and its isLocked property are internal, so they are reached by reflection.
            Type inspectorType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            PropertyInfo isLockedProperty = inspectorType?.GetProperty("isLocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (isLockedProperty == null)
                return;

            EditorWindow window = EditorWindow.GetWindow(inspectorType);
            bool isLocked = (bool)isLockedProperty.GetValue(window);
            isLockedProperty.SetValue(window, !isLocked);
            window.Repaint();
        }
    }
}
