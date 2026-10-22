using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Vacuous.EditorTools
{
    // Reports meshes that keep a CPU-side copy (Read/Write) and clears it on standalone mesh assets in the selected folders.
    public static class MeshReadWriteStripper
    {
        private const string MenuRoot = "Assets/Vacuous/Meshes/";

        #region Menu Items

        [MenuItem(MenuRoot + "Report Read-Write State")]
        private static void ReportReadWriteState()
        {
            Debug.Log(BuildReport(GetSelectedFolders()));
        }

        [MenuItem(MenuRoot + "Clear Read-Write On Mesh Assets", true)]
        private static bool ValidateClear() => GetSelectedFolders().Length > 0;

        [MenuItem(MenuRoot + "Clear Read-Write On Mesh Assets")]
        private static void ClearReadWriteOnMeshAssets()
        {
            string[] folders = GetSelectedFolders();
            List<Mesh> targets = CollectReadableMeshAssets(folders);

            if (targets.Count == 0)
            {
                Debug.Log("[MeshReadWrite] No readable mesh assets in the selected folders; nothing to do.");
                return;
            }

            long reclaimed = SumRuntimeSize(targets);

            bool isConfirmed = EditorUtility.DisplayDialog(
                "Clear Read/Write",
                $"Clear Read/Write on {targets.Count} mesh assets?\n\n" +
                $"This drops the CPU-side copy, freeing about {ToMegabytes(reclaimed)} of mesh memory.\n\n" +
                "It cannot be undone from the Editor. Restore from version control or re-bake if you need the data back.",
                "Clear Read/Write",
                "Cancel");

            if (!isConfirmed)
                return;

            try
            {
                foreach (Mesh mesh in targets)
                {
                    // Standalone mesh assets have no importer, so the Inspector cannot clear this flag.
                    // UploadMeshData(true) pushes the mesh to the GPU and releases the system-memory copy.
                    mesh.UploadMeshData(true);
                    EditorUtility.SetDirty(mesh);
                }
            }
            finally
            {
                AssetDatabase.SaveAssets();
            }

            Debug.Log($"[MeshReadWrite] Cleared Read/Write on {targets.Count} mesh assets, freeing about {ToMegabytes(reclaimed)}.");
        }

        #endregion

        #region Collection

        // With no folder selected, the report covers the whole Assets folder.
        private static string[] GetSelectedFolders()
        {
            return Selection.assetGUIDs
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(AssetDatabase.IsValidFolder)
                .ToArray();
        }

        private static List<Mesh> CollectReadableMeshAssets(string[] folders)
        {
            List<Mesh> meshes = new List<Mesh>();

            foreach (string path in FindMeshPaths(folders))
            {
                if (AssetImporter.GetAtPath(path) is ModelImporter)
                    continue;

                meshes.AddRange(LoadReadableMeshes(path));
            }

            return meshes;
        }

        private static Dictionary<string, List<Mesh>> CollectReadableModelMeshes(string[] folders)
        {
            Dictionary<string, List<Mesh>> byPath = new Dictionary<string, List<Mesh>>();

            foreach (string path in FindMeshPaths(folders))
            {
                if (AssetImporter.GetAtPath(path) is not ModelImporter)
                    continue;

                List<Mesh> meshes = LoadReadableMeshes(path);
                if (meshes.Count > 0)
                    byPath[path] = meshes;
            }

            return byPath;
        }

        private static IEnumerable<string> FindMeshPaths(string[] folders)
        {
            string[] searchIn = folders.Length > 0 ? folders : new[] { "Assets" };
            return AssetDatabase.FindAssets("t:Mesh", searchIn)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct();
        }

        private static List<Mesh> LoadReadableMeshes(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Mesh>()
                .Where(mesh => mesh.isReadable)
                .ToList();
        }

        #endregion

        #region Reporting

        private static string BuildReport(string[] folders)
        {
            List<Mesh> meshAssets = CollectReadableMeshAssets(folders);
            Dictionary<string, List<Mesh>> models = CollectReadableModelMeshes(folders);
            string scope = folders.Length > 0 ? string.Join(", ", folders) : "Assets";

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"[MeshReadWrite] Read/Write audit of {scope}");
            builder.AppendLine();
            builder.AppendLine($"Mesh assets still readable: {meshAssets.Count}  ({ToMegabytes(SumRuntimeSize(meshAssets))})");
            builder.AppendLine("  Fix: right-click the folder > Vacuous > Meshes > Clear Read-Write On Mesh Assets.");
            builder.AppendLine();

            long modelBytes = models.Values.Sum(SumRuntimeSize);
            int modelCount = models.Values.Sum(meshes => meshes.Count);

            builder.AppendLine($"Model-imported meshes still readable: {modelCount}  ({ToMegabytes(modelBytes)})");
            builder.AppendLine("  These have an importer. Untick Read/Write Enabled in the model's import settings instead.");

            foreach (KeyValuePair<string, List<Mesh>> entry in models)
                builder.AppendLine($"    {entry.Value.Count} mesh(es)\t{ToMegabytes(SumRuntimeSize(entry.Value))}\t{entry.Key}");

            return builder.ToString();
        }

        private static long SumRuntimeSize(List<Mesh> meshes)
        {
            return meshes.Sum(Profiler.GetRuntimeMemorySizeLong);
        }

        private static string ToMegabytes(long bytes)
        {
            return $"{bytes / 1048576f:F1} MB";
        }

        #endregion
    }
}
