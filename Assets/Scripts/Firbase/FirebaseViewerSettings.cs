using UnityEngine;

[CreateAssetMenu(fileName = "FirebaseViewerSettings", menuName = "Tools/Firebase Viewer Settings")]
public class FirebaseViewerSettings : ScriptableObject
{
    [Header("Auth (Email/Password)")]
    [Tooltip("Firebase Web API Key (Project Settings > General)")]
    public string webApiKey;

    [Tooltip("Viewer account email (read-only user)")]
    public string viewerEmail;

    [Tooltip("Viewer account password (read-only user)")]
    [HideInInspector]
    public string viewerPassword;

    [Header("Realtime Database")]
    [Tooltip("Database root URL. e.g. https://your-db.firebaseio.com or ...firebasedatabase.app")]
    public string databaseUrl;

    [Header("Optional")]
    [Tooltip("Default path to fetch, e.g. players, leaderboard, players/<uid>")]
    public string defaultPath = "players";
}
