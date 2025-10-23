using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class FirebaseZoneTracker : MonoBehaviour
{
    [Header("Firebase Settings")]
    public string databaseUrl = "https://data-analysis-d6b57-default-rtdb.firebaseio.com";
    public string idToken;

    private string userId = "test_user"; 

    public async Task SendZoneVisit(string zoneName)
    {
        string path = $"zones/{zoneName}/visits.json";
        string url = $"{databaseUrl}/{path}?auth={idToken}";

        int current = await GetVisitCount(url);
        int newValue = current + 1;

        string json = newValue.ToString();
        using (UnityWebRequest req = UnityWebRequest.Put(url, json))
        {
            req.method = "PUT";
            req.SetRequestHeader("Content-Type", "application/json");
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isHttpError || req.isNetworkError)
#endif
                Debug.LogError(req.error);
            else
                Debug.Log($"[Firebase] {zoneName} visit updated → {newValue}");
        }
    }

    private async Task<int> GetVisitCount(string url)
    {
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isHttpError || req.isNetworkError)
#endif
                return 0;

            string text = req.downloadHandler.text;
            if (int.TryParse(text, out int val))
                return val;
            return 0;
        }
    }
}
