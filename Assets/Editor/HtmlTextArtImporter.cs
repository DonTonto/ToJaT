using UnityEditor;
using UnityEngine;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

public class HtmlTextArtImporter : EditorWindow
{
    // fields for storing input output and metrics
    private string inputHtml = "";
    private string outputUnityText = "";
    private int width = 0;
    private int height = 0;
    private int visibleCharCount = 0;
    private int byteCount = 0;
    private bool showDebugInfo = false;
    // single scale multiplier for display size and font
    private float scale = 1f;
    private const int SL_BYTE_LIMIT = 65534;

    [MenuItem("Tools/Import HTML Text Art")]
    public static void ShowWindow() => GetWindow<HtmlTextArtImporter>("HTML Text Art Importer");

    private void OnGUI()
    {
        // watermark label in gray
        var savedColor = GUI.contentColor;
        GUI.contentColor = Color.gray;
        GUILayout.Label("made by @marosl with love", EditorStyles.miniLabel);
        GUI.contentColor = savedColor;

        // paste html art
        GUILayout.Label("paste html art", EditorStyles.boldLabel);
        inputHtml = EditorGUILayout.TextArea(inputHtml, GUILayout.Height(160));

        GUILayout.Space(5);
        // scale field
        scale = EditorGUILayout.FloatField("scale", scale);

        GUILayout.Space(5);
        // convert to unity rich text
        if (GUILayout.Button("convert to unity rich text"))
        {
            outputUnityText = ConvertHtmlToUnityText(inputHtml);
            byteCount = Encoding.UTF8.GetByteCount(outputUnityText);
            GUIUtility.systemCopyBuffer = outputUnityText;
        }

        GUILayout.Space(10);
        // unity output rich text
        GUILayout.Label("unity output rich text", EditorStyles.boldLabel);
        EditorGUILayout.TextArea(outputUnityText, GUILayout.Height(120));

        GUILayout.Space(5);
        // warning if over byte limit
        if (byteCount > SL_BYTE_LIMIT)
        {
            var prevColor = GUI.contentColor;
            GUI.contentColor = Color.red;
            GUI.enabled = false;
            GUILayout.Button("this output is too large for sl");
            GUI.enabled = true;
            GUI.contentColor = prevColor;
        }

        GUILayout.Space(5);
        // copy output to clipboard
        if (GUILayout.Button("copy output to clipboard"))
            GUIUtility.systemCopyBuffer = outputUnityText;

        GUILayout.Space(10);
        // debug info foldout
        showDebugInfo = EditorGUILayout.Foldout(showDebugInfo, "debug info");
        if (showDebugInfo)
        {
            EditorGUILayout.LabelField($"detected width", $"{width} characters");
            EditorGUILayout.LabelField($"detected height", $"{height} lines");
            float charRatio = height > 0 ? width / (float)height : 0f;
            EditorGUILayout.LabelField($"character ratio", $"{charRatio:0.##} : 1");
            if (byteCount <= SL_BYTE_LIMIT)
            {
                EditorGUILayout.LabelField($"total visible characters", $"{visibleCharCount}");
                EditorGUILayout.LabelField($"total bytes", $"{byteCount}");
            }
        }

        // --- MeowEditor integration via reflection ---
        // try to find type
        Type meowType = Type.GetType("MeowEditor.API.Spawnables.MeowEditorText, Assembly-CSharp-Editor")
                         ?? Type.GetType("MeowEditor.API.Spawnables.MeowEditorText");
        bool hasMeowSelection = false;
        if (meowType != null)
        {
            foreach (var go in Selection.gameObjects)
            {
                if (go.GetComponent(meowType) != null)
                {
                    hasMeowSelection = true;
                    break;
                }
            }
        }

        // apply to selected meoweditortext
        bool canApply = meowType != null && hasMeowSelection && byteCount <= SL_BYTE_LIMIT;
        var prevEnabled = GUI.enabled;
        var prevContent = GUI.contentColor;
        if (byteCount > SL_BYTE_LIMIT)
            GUI.contentColor = Color.red;

        GUI.enabled = canApply;
        if (GUILayout.Button("apply to selected MeowEditorText"))
        {
            // cache property infos
            var textProp = meowType.GetProperty("Text", BindingFlags.Public | BindingFlags.Instance);
            var displaySizeProp = meowType.GetProperty("DisplaySize", BindingFlags.Public | BindingFlags.Instance);
            var isAnimatedProp = meowType.GetProperty("IsAnimated", BindingFlags.Public | BindingFlags.Instance);

            var sizeStr = scale.ToString("0.##");
            foreach (var go in Selection.gameObjects)
            {
                var met = go.GetComponent(meowType);
                if (met == null) continue;

                Undo.RecordObject((UnityEngine.Object)met, "apply html text art");

                textProp?.SetValue(met, $"<size={sizeStr}><line-height={sizeStr}>{outputUnityText}");
                displaySizeProp?.SetValue(met, new Vector2(width * scale, height * scale));
                isAnimatedProp?.SetValue(met, true);

                EditorUtility.SetDirty((UnityEngine.Object)met);
            }
        }

        GUI.enabled = prevEnabled;
        GUI.contentColor = prevContent;
    }

    // convert html to unity rich text
    private string ConvertHtmlToUnityText(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            width = height = visibleCharCount = 0;
            return "";
        }
        html = Regex.Replace(html, @"<\/?pre[^>]*>", "", RegexOptions.IgnoreCase)
                   .Replace("\r\n", "\n");
        var lines = html.Split('\n');
        height = lines.Length;
        var sb = new StringBuilder();
        var pattern = new Regex(@"<b\s+style=""color:\s*(#[0-9A-Fa-f]{6})"">(.*?)</b>");
        int maxLineVisible = 0;
        int totalVisChars = 0;
        foreach (var line in lines)
        {
            var conv = pattern.Replace(line, m => $"<color={m.Groups[1].Value}>{m.Groups[2].Value}</color>");
            conv = GroupSameColorRuns(conv);
            var vis = Regex.Replace(conv, @"<[^>]+>", "");
            maxLineVisible = Mathf.Max(maxLineVisible, vis.Length);
            totalVisChars += vis.Length;
            sb.AppendLine(conv);
        }
        width = maxLineVisible;
        visibleCharCount = totalVisChars;
        return sb.ToString().TrimEnd();
    }

    // group same color runs
    private string GroupSameColorRuns(string line)
    {
        var tagPattern = new Regex(@"<color=(#[0-9A-Fa-f]{6})>(.*?)</color>");
        var matches = tagPattern.Matches(line);
        if (matches.Count == 0)
            return line;
        var sb = new StringBuilder();
        string prev = null;
        var run = new StringBuilder();
        foreach (Match m in matches)
        {
            var col = m.Groups[1].Value;
            var txt = m.Groups[2].Value;
            if (col == prev)
                run.Append(txt);
            else
            {
                if (prev != null)
                    sb.Append($"<color={prev}>{run}</color>");
                prev = col;
                run.Clear();
                run.Append(txt);
            }
        }
        if (prev != null)
            sb.Append($"<color={prev}>{run}</color>");
        return sb.ToString();
    }
}
