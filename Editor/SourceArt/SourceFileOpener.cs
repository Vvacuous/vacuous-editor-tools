using System.IO;
using UnityEditor;
using UnityEngine;

namespace Vacuous.EditorTools
{
    // Opens the source file (by default a .blend in SourceArt/) that an imported model was exported from.
    public static class SourceFileOpener
    {
        private const string MenuPath = "Assets/Vacuous/Open Source File";

        [MenuItem(MenuPath, true)]
        private static bool ValidateOpen()
        {
            return TryGetSelectedModelPath(out string modelPath) && TryMapToSourcePath(modelPath, out _);
        }

        [MenuItem(MenuPath, false, 20)]
        private static void Open()
        {
            if (!TryGetSelectedModelPath(out string modelPath))
                return;
            if (!TryMapToSourcePath(modelPath, out string sourcePath))
                return;

            if (File.Exists(sourcePath))
            {
                EditorUtility.OpenWithDefaultApp(sourcePath);
                return;
            }

            string folder = FindNearestExistingFolder(Path.GetDirectoryName(sourcePath));
            bool shouldReveal = EditorUtility.DisplayDialog(
                "Source file not found",
                $"No source file at:\n{sourcePath}\n\nIts name and folder should mirror the model's.",
                folder != null ? "Show Folder" : "OK",
                "Cancel");

            if (shouldReveal && folder != null)
                EditorUtility.RevealInFinder(folder);
        }

        private static bool TryGetSelectedModelPath(out string modelPath)
        {
            modelPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            return !string.IsNullOrEmpty(modelPath) && AssetImporter.GetAtPath(modelPath) is ModelImporter;
        }

        // Assets/_Project/Graphics/Models/Props/mesh_crate.fbx maps to <project>/SourceArt/Props/mesh_crate.blend.
        private static bool TryMapToSourcePath(string modelPath, out string sourcePath)
        {
            sourcePath = null;
            VacuousEditorToolsSettings settings = VacuousEditorToolsSettings.instance;
            string modelsRoot = settings.ModelsRoot + "/";

            if (!modelPath.StartsWith(modelsRoot))
                return false;

            string relativeModel = modelPath.Substring(modelsRoot.Length);
            string relativeFolder = Path.GetDirectoryName(relativeModel) ?? "";
            string fileName = Path.GetFileNameWithoutExtension(relativeModel) + settings.SourceFileExtension;
            string projectRoot = Path.GetDirectoryName(Application.dataPath);

            sourcePath = Path.GetFullPath(Path.Combine(projectRoot, settings.SourceArtRoot, relativeFolder, fileName));
            return true;
        }

        private static string FindNearestExistingFolder(string folder)
        {
            while (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                folder = Path.GetDirectoryName(folder);

            return folder;
        }
    }
}
