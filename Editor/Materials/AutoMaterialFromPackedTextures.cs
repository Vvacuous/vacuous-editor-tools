using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Vacuous.EditorTools
{
    // Builds URP Lit materials from texture sets named <base>_<map>, such as tex_crate_wooden_albedo.
    public static class AutoMaterialFromPackedTextures
    {
        private const string MenuPath = "Assets/Create/Vacuous/Auto Material From Textures (URP)";
        private const string LitShaderName = "Universal Render Pipeline/Lit";

        private static readonly string[] TextureExtensions = { ".png", ".jpg", ".jpeg", ".tga", ".exr", ".tif", ".tiff", ".psd" };

        private static readonly Dictionary<string, string> PropertyBySuffix = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "albedo", "_BaseMap" },
            { "basecolor", "_BaseMap" },
            { "diffuse", "_BaseMap" },
            { "ao", "_OcclusionMap" },
            { "ambientocclusion", "_OcclusionMap" },
            { "metal", "_MetallicGlossMap" },
            { "metallic", "_MetallicGlossMap" },
            { "metalic", "_MetallicGlossMap" },
            { "metalness", "_MetallicGlossMap" },
            { "normal", "_BumpMap" },
            { "normalgl", "_BumpMap" },
            { "height", "_ParallaxMap" },
            { "displace", "_ParallaxMap" },
            { "emission", "_EmissionMap" },
            { "emissive", "_EmissionMap" }
        };

        private static readonly Regex SuffixRegex = new Regex(@"_(?<suffix>[A-Za-z]+)$", RegexOptions.IgnoreCase);

        #region Menu

        [MenuItem(MenuPath, true)]
        private static bool ValidateCreateFromSelection() => Selection.assetGUIDs.Length > 0;

        [MenuItem(MenuPath)]
        private static void CreateFromSelection()
        {
            List<string> texturePaths = CollectTexturePaths();
            if (texturePaths.Count == 0)
            {
                Debug.LogWarning("[AutoMaterial] No textures found in the selection.");
                return;
            }

            Dictionary<string, List<string>> groups = GroupByBaseName(texturePaths);
            int created = 0;
            int updated = 0;

            foreach (KeyValuePair<string, List<string>> group in groups)
            {
                bool isNew = BuildMaterial(group.Key, group.Value);
                if (isNew)
                    created++;
                else
                    updated++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[AutoMaterial] Created {created}, updated {updated} materials from {groups.Count} texture sets.");
        }

        #endregion

        #region Building

        private static bool BuildMaterial(string baseName, List<string> texturePaths)
        {
            string folder = Path.GetDirectoryName(texturePaths[0]).Replace("\\", "/");
            string materialPath = $"{folder}/{ToMaterialName(baseName)}.mat";

            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            bool isNew = material == null;

            if (isNew)
            {
                material = new Material(Shader.Find(LitShaderName));
                AssetDatabase.CreateAsset(material, materialPath);
            }

            foreach (string texturePath in texturePaths)
                AssignTexture(material, texturePath);

            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 1f);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.5f);

            EditorUtility.SetDirty(material);
            return isNew;
        }

        private static void AssignTexture(Material material, string texturePath)
        {
            string suffix = GetSuffix(Path.GetFileNameWithoutExtension(texturePath));
            if (suffix == null || !PropertyBySuffix.TryGetValue(suffix, out string propertyName))
                return;

            ConfigureImporter(texturePath, propertyName);

            Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
            if (texture == null || !material.HasProperty(propertyName))
                return;

            material.SetTexture(propertyName, texture);

            if (propertyName != "_EmissionMap")
                return;

            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.white);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        }

        // Guidelines name textures tex_<subject>_<map>, so tex_crate_wooden_albedo becomes mat_crate_wooden.
        private static string ToMaterialName(string baseName)
        {
            string name = baseName.ToLowerInvariant().Replace(' ', '_');
            if (name.StartsWith("tex_"))
                name = name.Substring(4);

            return "mat_" + name;
        }

        #endregion

        #region Collection

        private static List<string> CollectTexturePaths()
        {
            List<string> paths = new List<string>();

            foreach (string guid in Selection.assetGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (AssetDatabase.IsValidFolder(path))
                {
                    paths.AddRange(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                        .Where(IsTexturePath)
                        .Select(found => found.Replace("\\", "/")));
                    continue;
                }

                if (IsTexturePath(path))
                    paths.Add(path);
            }

            return paths.Distinct().ToList();
        }

        private static Dictionary<string, List<string>> GroupByBaseName(List<string> texturePaths)
        {
            Dictionary<string, List<string>> groups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (string texturePath in texturePaths)
            {
                string fileName = Path.GetFileNameWithoutExtension(texturePath);
                Match match = SuffixRegex.Match(fileName);
                if (!match.Success || !PropertyBySuffix.ContainsKey(match.Groups["suffix"].Value))
                    continue;

                string baseName = fileName.Substring(0, match.Index);
                if (!groups.TryGetValue(baseName, out List<string> members))
                {
                    members = new List<string>();
                    groups[baseName] = members;
                }

                members.Add(texturePath);
            }

            return groups;
        }

        private static bool IsTexturePath(string path)
        {
            string extension = Path.GetExtension(path);
            return TextureExtensions.Any(known => string.Equals(known, extension, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetSuffix(string fileName)
        {
            Match match = SuffixRegex.Match(fileName);
            return match.Success ? match.Groups["suffix"].Value : null;
        }

        #endregion

        #region Import Settings

        private static void ConfigureImporter(string texturePath, string propertyName)
        {
            if (AssetImporter.GetAtPath(texturePath) is not TextureImporter importer)
                return;

            bool isNormalMap = propertyName == "_BumpMap";
            bool isColor = propertyName == "_BaseMap" || propertyName == "_EmissionMap";
            bool hasChanged = false;

            TextureImporterType wantedType = isNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (importer.textureType != wantedType)
            {
                importer.textureType = wantedType;
                hasChanged = true;
            }

            // Data maps (AO, metallic, height) hold linear values, so they must not be gamma-corrected.
            if (!isNormalMap && importer.sRGBTexture != isColor)
            {
                importer.sRGBTexture = isColor;
                hasChanged = true;
            }

            if (!isColor && importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = false;
                hasChanged = true;
            }

            if (propertyName == "_OcclusionMap" && importer.alphaSource != TextureImporterAlphaSource.FromGrayScale)
            {
                importer.alphaSource = TextureImporterAlphaSource.FromGrayScale;
                hasChanged = true;
            }

            if (hasChanged)
                importer.SaveAndReimport();
        }

        #endregion
    }
}
