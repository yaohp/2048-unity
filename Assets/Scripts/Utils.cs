
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public static class Utils
{
    public delegate void ImageDelegate(Texture2D texture2D);
    static private Texture2D duplicateTexture(Texture2D source)
    {
        RenderTexture renderTex = RenderTexture.GetTemporary(
                    source.width,
                    source.height,
                    0,
                    RenderTextureFormat.Default,
                    RenderTextureReadWrite.Linear);

        Graphics.Blit(source, renderTex);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTex;
        Texture2D readableText = new Texture2D(source.width, source.height);
        readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
        readableText.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTex);
        return readableText;
    }
    static public string SavePng(Texture2D texture2D, string name)
    {
        texture2D = duplicateTexture(texture2D);
        byte[] bytes = texture2D.EncodeToPNG();
        var dirPath = Application.persistentDataPath + "/SaveImages/";
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }
        File.WriteAllBytes(dirPath + name + ".png", bytes);
        return dirPath + name + ".png";
    }
    static public IEnumerator LoadImage(string path, ImageDelegate callback)
    {
        var url = "file://" + path;
        var unityWebRequestTexture = UnityWebRequestTexture.GetTexture(url);
        yield return unityWebRequestTexture.SendWebRequest();

        var texture = ((DownloadHandlerTexture)unityWebRequestTexture.downloadHandler).texture;
        if (texture == null)
        {
            Debug.LogError("Failed to load texture url:" + url);
        }
        callback(texture);


    }
    static public IEnumerator LoadImageByName(string name, ImageDelegate callback)
    {
        var dirPath = Application.persistentDataPath + "/SaveImages/";
        var url = "file://" + dirPath + name + ".png";
        var unityWebRequestTexture = UnityWebRequestTexture.GetTexture(url);
        yield return unityWebRequestTexture.SendWebRequest();
        Debug.Log("isDone:" + unityWebRequestTexture.isDone + " -- name:" + name + " -- downloadProgress:" + unityWebRequestTexture.downloadProgress);
        var texture = ((DownloadHandlerTexture)unityWebRequestTexture.downloadHandler).texture;
        if (texture == null)
        {
            Debug.LogError("Failed to load texture url:" + url);
        }
        callback(texture);


    }
}
