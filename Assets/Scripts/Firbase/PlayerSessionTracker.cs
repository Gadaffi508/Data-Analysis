using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;
using System.Threading.Tasks;

public class PlayerSessionTracker : MonoBehaviour
{
    public string databaseUrl;
    public string idToken;
    public string userId = "test_user";

    private DateTime loginTime;

    private async void Start()
    {
        loginTime = DateTime.UtcNow;

        await UpdateSession("loginTime", loginTime.ToString("o"));
        await UpdateSession("lastActive", loginTime.ToString("o"));
        await UpdateSession("isOnline", "true");

        InvokeRepeating(nameof(UpdateActivity), 5f, 5f);
    }

    private async void OnApplicationQuit()
    {
        await EndSession();
    }

    private async void UpdateActivity()
    {
        await UpdateSession("lastActive", DateTime.UtcNow.ToString("o"));
    }

    private async Task EndSession()
    {
        DateTime logoutTime = DateTime.UtcNow;
        TimeSpan sessionDuration = logoutTime - loginTime;

        await UpdateSession("logoutTime", logoutTime.ToString("o"));
        await UpdateIncrement("totalPlayTime", (int)sessionDuration.TotalSeconds);
        await UpdateSession("isOnline", "false");
    }

    private async Task UpdateSession(string field, string value)
    {
        string url = $"{databaseUrl}/sessions/{userId}/{field}.json?auth={idToken}";
        using UnityWebRequest req = UnityWebRequest.Put(url, $"\"{value}\"");
        req.SetRequestHeader("Content-Type", "application/json");
        await req.SendWebRequest();
    }

    private async Task UpdateIncrement(string field, int add)
    {
        string url = $"{databaseUrl}/sessions/{userId}/{field}.json?auth={idToken}";
        using UnityWebRequest get = UnityWebRequest.Get(url);
        await get.SendWebRequest();

        int current = 0;
        int.TryParse(get.downloadHandler.text, out current);

        int newValue = current + add;

        using UnityWebRequest put = UnityWebRequest.Put(url, newValue.ToString());
        put.SetRequestHeader("Content-Type", "application/json");
        await put.SendWebRequest();
    }
}
