using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class FirebaseIdpResponse
{
    public string idToken;
    public string refreshToken;
    public string expiresIn;
    public string localId;
    public string email;
}

public static class FirebaseAuthClient
{
    const string SignInUrl = "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={0}";

    public static async Task<FirebaseIdpResponse> SignInEmailPasswordAsync(string webApiKey, string email, string password)
    {
        var url = string.Format(SignInUrl, webApiKey);
        var payload = JsonUtility.ToJson(new SignInRequest { email = email, password = password, returnSecureToken = true });

        using (var req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogError($"[FirebaseAuth] SignIn error: {req.error}\n{req.downloadHandler.text}");
                throw new Exception("Firebase sign-in failed.");
            }

            var json = req.downloadHandler.text;
            var resp = JsonUtility.FromJson<FirebaseIdpResponse>(json);
            if (string.IsNullOrEmpty(resp?.idToken))
                throw new Exception("No idToken received.");

            return resp;
        }
    }

    [Serializable]
    private class SignInRequest
    {
        public string email;
        public string password;
        public bool returnSecureToken;
    }
}
