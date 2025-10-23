using System.Text;
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
}
