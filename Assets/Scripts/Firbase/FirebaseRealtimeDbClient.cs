using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class FirebaseRealtimeDbClient
{
    public static async Task<string> GetJsonAsync(string databaseUrl, string idToken, string path)
    {
        if (string.IsNullOrWhiteSpace(databaseUrl))
            throw new ArgumentException("Database URL is empty");
        if (string.IsNullOrWhiteSpace(path))
            path = "/";

        // Normalize
        if (!databaseUrl.EndsWith("/")) databaseUrl += "/";
        path = path.TrimStart('/');

        var url = $"{databaseUrl}{path}.json?auth={idToken}";

        using (var req = UnityWebRequest.Get(url))
        {
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                throw new Exception($"RealtimeDB GET failed: {req.error}\n{req.downloadHandler.text}");
            }

            return req.downloadHandler.text;
        }
    }
}
