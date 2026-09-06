using UnityEngine;
using System.IO;

public class SaveFolderOpenerScript : MonoBehaviour
{
    public void OpenSaveFolder()
    {
        string path = Application.persistentDataPath;

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        Debug.Log($"Save folder path: {path}");

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        string windowsPath = path.Replace('/', '\\');
        System.Diagnostics.Process.Start("explorer.exe", "\"" + windowsPath + "\"");
#elif UNITY_STANDALONE_OSX
        System.Diagnostics.Process.Start("open", $"\"{path}\"");
#else
        Debug.LogWarning($"OpenSaveFolder is not supported on this platform. Path: {path}");
#endif
    }
}