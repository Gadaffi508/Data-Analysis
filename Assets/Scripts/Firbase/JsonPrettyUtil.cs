using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class JsonPrettyUtil
{
    public static string Pretty(string json)
    {
        if (string.IsNullOrEmpty(json)) return "";
        try
        {
            var obj = JsonUtility.FromJson<Wrapper>("{\"v\":" + json + "}");

            return json;
        }
        catch
        {
            return json;
        }
    }

    [System.Serializable] private class Wrapper { public object v; }

    public static string FilterContains(string json, string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return json;
        keyword = keyword.ToLowerInvariant();

        var lines = json.Split('\n');
        var sb = new StringBuilder();
        foreach (var ln in lines)
        {
            if (ln.ToLowerInvariant().Contains(keyword))
                sb.AppendLine(ln);
        }
        var r = sb.ToString();
        return string.IsNullOrEmpty(r) ? "// no matches" : r;
    }

    public static void DrawJsonTree(Dictionary<string, object> node, int indent = 0)
    {
        foreach (var kv in node)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(indent * 16);

            if (kv.Value is Dictionary<string, object> child)
            {
                bool expanded = EditorPrefs.GetBool("fold_" + kv.Key, false);
                bool newState = EditorGUILayout.Foldout(expanded, kv.Key, true);

                if (newState != expanded)
                    EditorPrefs.SetBool("fold_" + kv.Key, newState);

                if (newState)
                    DrawJsonTree(child, indent + 1);
            }
            else
            {
                EditorGUILayout.LabelField($"{kv.Key}: {kv.Value}");
            }

            GUILayout.EndHorizontal();
        }
    }

}
