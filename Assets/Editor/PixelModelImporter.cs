using System;
using MeowEditor.API.Enums;
using MeowEditor.API.Models;
using UnityEditor;
using UnityEngine;

namespace MeowEditor.API.Editors
{
    /// <summary>
    /// Converts a texture into a set of colored quads using the Meow Editor API.
    /// </summary>
    public sealed class PixelModelImporter : EditorWindow
    {
        [MenuItem("GameObject/Meow Editor/Import Pixel Model", false, 60)]
        public static void ShowWindow() => GetWindow<PixelModelImporter>(true, "Convert image to quads");

        private static Texture2D _image;
        private static bool _merge;
        private static float _mergeThreshold = 0.01f;

        private const string MergeQuadsDescription =
            "Should multiple neighboring quads (per row) with the same color be merged into a single one?";

        private const string MergeThresholdDescription =
            "The threshold for merging quads. A value of 0.01 means that the difference between two colors must be less than 1% to be considered the same. Alpha is not taken into account.";

        private void OnGUI()
        {
            GUILayout.Label("Image", EditorStyles.boldLabel);
            _image = (Texture2D)EditorGUI.ObjectField(new Rect(5, 20, 100, 100), _image, typeof(Texture2D), true);
            GUILayout.Space(105);
            _merge = EditorGUILayout.Toggle(new GUIContent("Merge Quads*", MergeQuadsDescription), _merge);
            if (_merge)
                _mergeThreshold = EditorGUILayout.Slider(new GUIContent("Merge Threshold*", MergeThresholdDescription), _mergeThreshold, 0f, 1f);
            EditorGUILayout.HelpBox("This operation may create an excessive amount of GameObjects. Make sure to use it on small images.", MessageType.Warning);
            if (GUILayout.Button("Generate"))
                Generate();
        }

        private static void Generate()
        {
            if (_image == null || !_image.isReadable)
            {
                EditorUtility.DisplayDialog("Conversion Error", "You must provide a readable image! Make sure to apply the proper import settings.", "OK");
                return;
            }

            try
            {
                AssetDatabase.DisallowAutoRefresh();
                var rootObject = new GameObject($"{_image.name}-prefab");
                var bundle = rootObject.AddComponent<MeowEditorPrefab>();

                if (_merge)
                    GenerateQuadsMerged(bundle);
                else
                    GenerateQuadsNotMerged(bundle);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                EditorUtility.DisplayDialog("Conversion failed", "Failed to convert the image. See the debug log for details.", "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.AllowAutoRefresh();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static void GenerateQuadsMerged(MeowEditorPrefab bundle)
        {
            float height = _image.height;
            for (int y = 0; y < height; y++)
            {
                float progress = y / height;
                EditorUtility.DisplayProgressBar($"Generating quads (merged): {progress:P2}", $"Processing row {y}", progress);
                int x = 0;
                while (x < _image.width)
                {
                    Color color = _image.GetPixel(x, y);
                    int width = CalculateWidth(x, y, color);
                    if (color.a > 0f)
                        CreatePrimitive(new Vector3(x + width * 0.5f, 1f, y), color, width, bundle);
                    x += width;
                }
            }
        }

        private static int CalculateWidth(int x, int y, Color color)
        {
            int width = 1;
            while (x + width < _image.width)
            {
                Color nextColor = _image.GetPixel(x + width, y);
                if (CheckThreshold(nextColor.r, color.r) || CheckThreshold(nextColor.g, color.g) || CheckThreshold(nextColor.b, color.b))
                    break;
                width++;
            }

            return width;
        }

        private static bool CheckThreshold(float a, float b) => Mathf.Abs(a - b) > _mergeThreshold;

        private static void GenerateQuadsNotMerged(MeowEditorPrefab bundle)
        {
            float height = _image.height;
            for (int y = 0; y < height; y++)
            {
                float progress = y / height;
                EditorUtility.DisplayProgressBar($"Generating quads: {progress:P2}", $"Processing row {y}", progress);
                for (int x = 0; x < _image.width; x++)
                {
                    Color color = _image.GetPixel(x, y);
                    if (color.a > 0f)
                        CreatePrimitive(new Vector3(x, 1f, y), color, 1, bundle);
                }
            }
        }

        private static void CreatePrimitive(Vector3 position, Color color, int width, MeowEditorPrefab bundle)
        {
            var primitive = bundle.AddPrimitive(bundle.transform, SpawnablePrimitiveType.Quad);
            var t = primitive.transform;
            t.localPosition = position;
            t.localScale = new Vector3(width, 1f, 1f);
            t.localRotation = Quaternion.Euler(90f, 0f, 0f);
            primitive.Color = color;
        }
    }
}