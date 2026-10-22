using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Vacuous.EditorTools
{
    // Exports a Terrain's heightmap as an OBJ mesh at a chosen resolution.
    public class TerrainToObjExporter : EditorWindow
    {
        private const int DefaultResolution = 513;
        private static readonly string[] ResolutionLabels = { "65", "129", "257", "513", "1025" };
        private static readonly int[] ResolutionValues = { 65, 129, 257, 513, 1025 };

        private Terrain _terrain;
        private int _resolution = DefaultResolution;
        private string _savePath = "";

        [MenuItem("Tools/Vacuous/Terrain/Export Terrain to OBJ")]
        private static void Init()
        {
            TerrainToObjExporter window = GetWindow<TerrainToObjExporter>("Terrain to OBJ");
            window.minSize = new Vector2(380, 220);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Space(8f);
            GUILayout.Label("Terrain to OBJ Exporter", EditorStyles.boldLabel);
            GUILayout.Space(6f);

            _terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", _terrain, typeof(Terrain), true);

            EditorGUILayout.HelpBox(
                "Higher mesh resolution creates more detail but also a larger file. " +
                "For sculpting or artistic work, 129, 257, or 513 is usually sufficient.",
                MessageType.Info);

            _resolution = EditorGUILayout.IntPopup("Mesh Resolution", _resolution, ResolutionLabels, ResolutionValues);

            GUILayout.Space(10f);

            if (GUILayout.Button("Choose Save Location", GUILayout.Height(28f)))
            {
                string defaultName = _terrain != null ? _terrain.name : "terrain_export";
                string path = EditorUtility.SaveFilePanel("Save OBJ", Application.dataPath, defaultName, "obj");
                if (!string.IsNullOrEmpty(path))
                {
                    _savePath = EnsureObjExtension(path);
                }
            }

            if (!string.IsNullOrEmpty(_savePath))
            {
                EditorGUILayout.LabelField("Save Path", _savePath);
            }

            GUILayout.Space(10f);

            EditorGUI.BeginDisabledGroup(_terrain == null || string.IsNullOrEmpty(_savePath));
            if (GUILayout.Button("Export", GUILayout.Height(32f)))
            {
                ExportTerrain(_terrain, _resolution, _savePath);
            }
            EditorGUI.EndDisabledGroup();
        }

        private static void ExportTerrain(Terrain terrain, int targetResolution, string outputPath)
        {
            if (terrain == null)
            {
                EditorUtility.DisplayDialog("Export Failed", "Please assign a Terrain object first.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                EditorUtility.DisplayDialog("Export Failed", "Please choose a save location first.", "OK");
                return;
            }

            TerrainData data = terrain.terrainData;
            if (data == null)
            {
                EditorUtility.DisplayDialog("Export Failed", "The selected Terrain has no terrain data.", "OK");
                return;
            }

            int heightmapResolution = data.heightmapResolution;
            int step = Mathf.Max(1, (heightmapResolution - 1) / Mathf.Max(1, targetResolution - 1));
            int width = (heightmapResolution - 1) / step + 1;
            int height = (heightmapResolution - 1) / step + 1;

            Vector3 size = data.size;
            Vector3 terrainPosition = terrain.transform.position;
            StringBuilder stringBuilder = new StringBuilder(1024 * 1024);

            stringBuilder.AppendLine("# Terrain exported from Unity");
            stringBuilder.Append("o ").AppendLine(terrain.name);

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    int sampleX = Mathf.Min(x * step, heightmapResolution - 1);
                    int sampleZ = Mathf.Min(z * step, heightmapResolution - 1);
                    float heightValue = data.GetHeight(sampleX, sampleZ);
                    float worldX = ((float)sampleX / (heightmapResolution - 1)) * size.x + terrainPosition.x;
                    float worldZ = ((float)sampleZ / (heightmapResolution - 1)) * size.z + terrainPosition.z;

                    stringBuilder.Append("v ")
                        .Append(worldX.ToString("F4", CultureInfo.InvariantCulture)).Append(' ')
                        .Append((heightValue + terrainPosition.y).ToString("F4", CultureInfo.InvariantCulture)).Append(' ')
                        .Append(worldZ.ToString("F4", CultureInfo.InvariantCulture)).AppendLine();
                }
            }

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = width > 1 ? (float)x / (width - 1) : 0f;
                    float v = height > 1 ? (float)z / (height - 1) : 0f;
                    stringBuilder.Append("vt ")
                        .Append(u.ToString("F4", CultureInfo.InvariantCulture)).Append(' ')
                        .Append(v.ToString("F4", CultureInfo.InvariantCulture)).AppendLine();
                }
            }

            for (int z = 0; z < height - 1; z++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    int i0 = z * width + x + 1;
                    int i1 = z * width + x + 2;
                    int i2 = (z + 1) * width + x + 1;
                    int i3 = (z + 1) * width + x + 2;

                    stringBuilder.Append("f ").Append(i0).Append("/").Append(i0).Append(' ')
                        .Append(i2).Append("/").Append(i2).Append(' ')
                        .Append(i1).Append("/").Append(i1).AppendLine();

                    stringBuilder.Append("f ").Append(i1).Append("/").Append(i1).Append(' ')
                        .Append(i2).Append("/").Append(i2).Append(' ')
                        .Append(i3).Append("/").Append(i3).AppendLine();
                }
            }

            File.WriteAllText(outputPath, stringBuilder.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();

            int triangleCount = (width - 1) * (height - 1) * 2;
            EditorUtility.DisplayDialog(
                "Export Complete",
                $"Terrain exported to:\n{outputPath}\n\nVertices: {width}x{height} = {width * height}\nTriangles: {triangleCount}",
                "OK");

            Debug.Log($"Terrain exported as OBJ: {outputPath} ({width}x{height} vertices, {triangleCount} triangles)");
        }

        private static string EnsureObjExtension(string path)
        {
            return Path.GetExtension(path) == ".obj" ? path : path + ".obj";
        }
    }
}
