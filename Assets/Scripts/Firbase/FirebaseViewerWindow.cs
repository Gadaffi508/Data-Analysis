#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class FirebaseViewerWindow : EditorWindow
{
    private enum Tab { Viewer, Sessions, Bookmarks, Settings }

    private Tab currentTab;

    private FirebaseViewerSettings settings;
    private string idToken;
    private DateTime tokenTime;
    private string fetchPath;
    private string jsonRaw;
    private string searchKeyword;
    private Vector2 scroll;

    private List<string> bookmarks = new();

    private string[] commonPaths = { "players", "zones", "leaderboard", "stats" };

    private string sessionsJsonRaw;
    private Vector2 sessionsScroll;

    [MenuItem("Tools/Firebase Player Data Viewer")]
    public static void ShowWindow()
    {
        var window = GetWindow<FirebaseViewerWindow>("Firebase Viewer");
        window.titleContent = new GUIContent("Firebase Dashboard");
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
        DrawTabs(); // <-- BUNU EKLE
        EditorGUILayout.Space(8);

        switch (currentTab)
        {
            case Tab.Viewer:
                DrawSettingsUI();
                EditorGUILayout.Space(8);
                DrawFetchUI();
                EditorGUILayout.Space(8);
                DrawResultUI();
                break;

            case Tab.Sessions:
                DrawSessionTab();
                break;

            case Tab.Bookmarks:
                DrawBookmarksTab();
                break;

            case Tab.Settings:
                DrawSettingsUI();
                break;
        }
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
        if (GUILayout.Button("Fetch Now", GUILayout.Width(120)))
            _ = FetchAsync();
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(fetchPath))
        {
            var filtered = commonPaths.Where(p => p.StartsWith(fetchPath, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (filtered.Length > 0 && filtered[0] != fetchPath)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                foreach (var p in filtered)
                {
                    if (GUILayout.Button(p, EditorStyles.miniButton))
                    {
                        fetchPath = p;
                        GUI.FocusControl(null);
                    }
                }
                GUILayout.EndVertical();
            }
        }
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

        // Search UI
        EditorGUILayout.BeginHorizontal();
        searchKeyword = EditorGUILayout.TextField("Search", searchKeyword);
        if (GUILayout.Button("Clear", GUILayout.Width(80)))
            searchKeyword = "";
        if (GUILayout.Button("Save JSON...", GUILayout.Width(120)))
            SaveJsonToFile();
        EditorGUILayout.EndHorizontal();

        // Scroll ile tree gösterimi
        scroll = EditorGUILayout.BeginScrollView(scroll);

        try
        {
            var dict = MiniJSON.Json.Deserialize(jsonRaw) as Dictionary<string, object>;
            JsonPrettyUtil.DrawJsonTree(dict); // <-- EKLENECEK
        }
        catch
        {
            EditorGUILayout.HelpBox("Invalid JSON format", MessageType.Warning);
        }

        EditorGUILayout.EndScrollView();

        // Alt kýsýmda zone analytics (varsa)
        DrawZoneAnalytics(jsonRaw);
    }

    private async void DrawSessionTab()
    {
        EditorGUILayout.LabelField("Player Sessions", EditorStyles.boldLabel);

        if (GUILayout.Button("Load sessions", GUILayout.Height(25)))
            LoadSessions();

        if (string.IsNullOrEmpty(sessionsJsonRaw))
        {
            EditorGUILayout.HelpBox("No session data loaded.", MessageType.Info);
            return;
        }

        sessionsScroll = EditorGUILayout.BeginScrollView(sessionsScroll);

        try
        {
            var dict = MiniJSON.Json.Deserialize(sessionsJsonRaw) as Dictionary<string, object>;

            if (dict != null)
            {
                JsonPrettyUtil.DrawJsonTree(dict);
            }
            else
            {
                EditorGUILayout.HelpBox("Invalid JSON structure.", MessageType.Warning);
            }
        }
        catch (Exception ex)
        {
            EditorGUILayout.HelpBox("JSON parse error: " + ex.Message, MessageType.Error);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawBookmarksTab()
    {
        EditorGUILayout.LabelField("Bookmarks", EditorStyles.boldLabel);

        if (GUILayout.Button("Add Current Path"))
        {
            if (!bookmarks.Contains(fetchPath))
                bookmarks.Add(fetchPath);
        }

        foreach (var bm in bookmarks)
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(bm, EditorStyles.miniButton))
            {
                fetchPath = bm;
                _ = FetchAsync();
            }

            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                bookmarks.Remove(bm);
                break;
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawTabs()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Toggle(currentTab == Tab.Viewer, "Viewer", EditorStyles.toolbarButton))
            currentTab = Tab.Viewer;
        if (GUILayout.Toggle(currentTab == Tab.Sessions, "Sessions", EditorStyles.toolbarButton))
            currentTab = Tab.Sessions;
        if (GUILayout.Toggle(currentTab == Tab.Bookmarks, "Bookmarks", EditorStyles.toolbarButton))
            currentTab = Tab.Bookmarks;
        if (GUILayout.Toggle(currentTab == Tab.Settings, "Settings", EditorStyles.toolbarButton))
            currentTab = Tab.Settings;

        GUILayout.EndHorizontal();
    }


    private void DrawZoneAnalytics(string json)
    {
        // Parse: {"zone1":{"visits":4},"zone2":{"visits":1}}
        var data = new Dictionary<string, int>();
        try
        {
            var jsonObj = MiniJSON.Json.Deserialize(json) as Dictionary<string, object>;
            foreach (var kv in jsonObj)
            {
                if (kv.Value is Dictionary<string, object> sub)
                {
                    if (sub.ContainsKey("visits"))
                        data[kv.Key] = Convert.ToInt32(sub["visits"]);
                }
            }
        }
        catch
        {
            EditorGUILayout.HelpBox("Invalid JSON format.", MessageType.Error);
            return;
        }

        if (data.Count == 0)
        {
            EditorGUILayout.HelpBox("No zone data found.", MessageType.Info);
            return;
        }

        int total = 0;
        foreach (var v in data.Values) total += v;
        if (total == 0) total = 1;

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Zone Analytics", EditorStyles.boldLabel);

        foreach (var kv in data)
        {
            float ratio = (float)kv.Value / total;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{kv.Key} ({kv.Value} visits)", GUILayout.Width(150));
            Rect r = GUILayoutUtility.GetRect(1, 20, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(new Rect(r.x, r.y + 5, r.width * ratio, 10), Color.Lerp(Color.red, Color.green, ratio));
            EditorGUILayout.LabelField($"{(ratio * 100f):0.0}%", GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
        }
    }

    private async void LoadSessions()
    {
        try
        {
            sessionsJsonRaw = await FirebaseRealtimeDbClient.GetJsonAsync(
                settings.databaseUrl,
                idToken,
                "sessions"
            );

            Repaint(); // GUI yenile
        }
        catch (Exception ex)
        {
            sessionsJsonRaw = "// ERROR\n" + ex.Message;
        }
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
