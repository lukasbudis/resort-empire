using UnityEngine;
using System.IO;
using UnityEngine.Networking;
using System.Collections;
using System;

public class FileMover : MonoBehaviour
{
    private string streamingPath;
    private string persistentPath;

    void Awake()
    {
        streamingPath = Path.Combine(Application.streamingAssetsPath, "config.json");
        persistentPath = Path.Combine(Application.persistentDataPath, "config.json");

        StartCoroutine(LoadAndShowConfig());
    }

    IEnumerator LoadAndShowConfig()
    {
        string jsonText = "";

        if (File.Exists(persistentPath))
        {
            jsonText = File.ReadAllText(persistentPath);
            yield break;
        }

        if (streamingPath.Contains("://") || streamingPath.Contains(":///"))
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(streamingPath))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    jsonText = webRequest.downloadHandler.text;
                    File.WriteAllText(persistentPath, jsonText);
                }
            }
        }
        else
        {
            if (File.Exists(streamingPath))
            {
                jsonText = File.ReadAllText(streamingPath);
                
                File.WriteAllText(persistentPath, jsonText);
            }
        }
    }
}
