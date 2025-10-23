#if UNITY_EDITOR
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class FirebaseViewerWindow : EditorWindow
{
    private FirebaseViewerSettings settings;
    private string idToken;
    private DateTime tokenTime;
    private string fetchPath;
    private string jsonRaw;
    private string searchKeyword;
    private Vector2 scroll;

    [MenuItem("Tools/Firebase Player Data Viewer")]
    public static void ShowWindow()
    {
        var window = GetWindow<FirebaseViewerWindow>("Firebase Viewer");
        window.minSize = new Vector2(600, 400);
        window.Show();
    }

    private void OnEnable()
    {
        // Otomatik settings bulma
        var guids = AssetDatabase.FindAssets("t:FirebaseViewerSettings");
        if (guids != null && guids.Length > 0)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            settings = AssetDatabase.LoadAssetAtPath<FirebaseViewerSettings>(path);
        }
    }

    private void OnGUI()
    {
        DrawSettingsUI();
        EditorGUILayout.Space(8);
        DrawFetchUI();
        EditorGUILayout.Space(8);
        DrawResultUI();
    }

    private void DrawSettingsUI()
    {
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

        settings = (FirebaseViewerSettings)EditorGUILayout.ObjectField("Settings Asset", settings, typeof(FirebaseViewerSettings), false);

        if (settings == null)
        {
            EditorGUILayout.HelpBox("Create a Settings asset via Create > Tools > Firebase Viewer Settings", MessageType.Info);
            return;
        }

        EditorGUI.BeginChangeCheck();
        settings.webApiKey = EditorGUILayout.TextField("Web API Key", settings.webApiKey);
        settings.viewerEmail = EditorGUILayout.TextField("Viewer Email", settings.viewerEmail);
        settings.viewerPassword = EditorGUILayout.PasswordField("Viewer Password", settings.viewerPassword);
        settings.databaseUrl = EditorGUILayout.TextField("Database URL", settings.databaseUrl);
        settings.defaultPath = EditorGUILayout.TextField("Default Path", settings.defaultPath);
        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(settings);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Sign In (Email/Password)", GUILayout.Height(24)))
        {
            _ = SignInAsync();
        }
        GUI.enabled = !string.IsNullOrEmpty(idToken);
        if (GUILayout.Button("Sign Out", GUILayout.Height(24)))
        {
            idToken = null;
            tokenTime = default;
            ShowNotification(new GUIContent("Signed out"));
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(idToken))
        {
            EditorGUILayout.HelpBox($"Signed in as {settings.viewerEmail} @ {tokenTime:HH:mm:ss}", MessageType.None);
        }
    }

    private async Task SignInAsync()
    {
        try
        {
            var resp = await FirebaseAuthClient.SignInEmailPasswordAsync(settings.webApiKey, settings.viewerEmail, settings.viewerPassword);
            idToken = resp.idToken;
            tokenTime = DateTime.Now;
            ShowNotification(new GUIContent("Sign-in OK"));
        }
        catch (Exception ex)
        {
            idToken = null;
            ShowNotification(new GUIContent("Sign-in failed"));
            Debug.LogException(ex);
        }
    }

    private void DrawFetchUI()
    {
        EditorGUILayout.LabelField("Fetch", EditorStyles.boldLabel);

        if (settings == null) return;

        if (string.IsNullOrEmpty(fetchPath))
            fetchPath = settings.defaultPath;

        EditorGUILayout.BeginHorizontal();
        fetchPath = EditorGUILayout.TextField("Path", fetchPath);
        GUI.enabled = !string.IsNullOrEmpty(idToken) && !string.IsNullOrEmpty(settings.databaseUrl);
        if (GUILayout.Button("Fetch Now", GUILayout.Width(120)))
        {
            _ = FetchAsync();
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
    }

    private async Task FetchAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(idToken))
                throw new Exception("Not signed in");

            jsonRaw = await FirebaseRealtimeDbClient.GetJsonAsync(settings.databaseUrl, idToken, fetchPath);
            Repaint();
        }
        catch (Exception ex)
        {
            jsonRaw = $"// ERROR\n{ex.Message}";
            Debug.LogException(ex);
        }
    }

    private void DrawResultUI()
    {
        EditorGUILayout.LabelField("Result", EditorStyles.boldLabel);

        if (string.IsNullOrEmpty(jsonRaw))
        {
            EditorGUILayout.HelpBox("No data fetched yet.", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        searchKeyword = EditorGUILayout.TextField("Search", searchKeyword);
        if (GUILayout.Button("Clear", GUILayout.Width(80)))
            searchKeyword = "";
        if (GUILayout.Button("Save JSON...", GUILayout.Width(120)))
            SaveJsonToFile();
        EditorGUILayout.EndHorizontal();

        var display = string.IsNullOrEmpty(searchKeyword)
            ? JsonPrettyUtil.Pretty(jsonRaw)
            : JsonPrettyUtil.FilterContains(jsonRaw, searchKeyword);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.TextArea(display, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private void SaveJsonToFile()
    {
        var path = EditorUtility.SaveFilePanel("Save JSON", Application.dataPath, $"firebase_{(string.IsNullOrEmpty(fetchPath) ? "root" : fetchPath.Replace('/', '_'))}.json", "json");
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            File.WriteAllText(path, jsonRaw);
            ShowNotification(new GUIContent("Saved."));
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }
}
#endif
