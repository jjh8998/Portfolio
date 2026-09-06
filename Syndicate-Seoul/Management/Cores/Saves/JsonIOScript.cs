using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class JsonIOScript : MonoBehaviour
{
    public const int AutoSaveSlotIndex = 0;
    public const int MainSlotMinIndex = 1;
    public const int MainSlotMaxIndex = 3;

    private const string SaveFileNameFormat = "save_slot_{0}.json";

    public static bool SaveFileExists(int slotIndex)
    {
        if (slotIndex < 0)
            return false;

        return File.Exists(GetSaveFilePathForSlot(slotIndex));
    }

    public static bool AnyMainSaveFileExists()
    {
        for (int i = MainSlotMinIndex; i <= MainSlotMaxIndex; i++)
        {
            if (SaveFileExists(i))
                return true;
        }
        return false;
    }

    public static IEnumerable<int> EnumerateExistingMainSlots()
    {
        for (int i = MainSlotMinIndex; i <= MainSlotMaxIndex; i++)
        {
            if (SaveFileExists(i))
                yield return i;
        }
    }

    private static string GetSaveFilePathForSlot(int slotIndex)
    {
        return Path.Combine(Application.persistentDataPath, string.Format(SaveFileNameFormat, slotIndex));
    }

    private string GetSaveFilePath(int slotIndex)
    {
        return GetSaveFilePathForSlot(slotIndex);
    }

    public bool SaveToJson<T>(int slotIndex, T data)
    {
        if (slotIndex < 0)
        {
            Debug.LogWarning($"Save failed. Invalid slot index: {slotIndex}");
            return false;
        }

        if (ReferenceEquals(data, null))
        {
            Debug.LogWarning($"Save failed. Data is null for slot {slotIndex}");
            return false;
        }

        try
        {
            string filePath = GetSaveFilePath(slotIndex);
            string directoryPath = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrEmpty(directoryPath))
                Directory.CreateDirectory(directoryPath);

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(filePath, json);

            Debug.Log($"Save succeeded. Slot: {slotIndex}, Path: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Save failed for slot {slotIndex}: {ex.Message}");
            return false;
        }
    }

    public bool TryLoadFromJson<T>(int slotIndex, out T data)
    {
        data = default(T);

        if (slotIndex < 0)
        {
            Debug.LogWarning($"Load failed. Invalid slot index: {slotIndex}");
            return false;
        }

        string filePath = GetSaveFilePath(slotIndex);
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"Load skipped. Save file does not exist for slot {slotIndex}");
            return false;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            data = JsonUtility.FromJson<T>(json);

            if (ReferenceEquals(data, null))
            {
                Debug.LogWarning($"Load failed. Json deserialization returned null for slot {slotIndex}");
                return false;
            }

            Debug.Log($"Load succeeded. Slot: {slotIndex}, Path: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Load failed for slot {slotIndex}: {ex.Message}");
            data = default(T);
            return false;
        }
    }

    public static bool TryReadSlotMetadata(int slotIndex, out SaveSlotMetadata metadata)
    {
        metadata = null;

        if (slotIndex < 0)
            return false;

        string filePath = GetSaveFilePathForSlot(slotIndex);
        if (!File.Exists(filePath))
            return false;

        try
        {
            string json = File.ReadAllText(filePath);
            metadata = JsonUtility.FromJson<SaveSlotMetadata>(json);

            if (metadata != null)
            {
                metadata.fileLastWriteTime = File.GetLastWriteTime(filePath);
                metadata.slotIndex = slotIndex;
            }
            return metadata != null;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Read slot metadata failed for slot {slotIndex}: {ex.Message}");
            metadata = null;
            return false;
        }
    }

    public bool HasSaveFile(int slotIndex)
    {
        if (slotIndex < 0)
            return false;

        return File.Exists(GetSaveFilePath(slotIndex));
    }

    public bool DeleteSaveFile(int slotIndex)
    {
        if (slotIndex < 0)
        {
            Debug.LogWarning($"Delete failed. Invalid slot index: {slotIndex}");
            return false;
        }

        string filePath = GetSaveFilePath(slotIndex);
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"Delete skipped. Save file does not exist for slot {slotIndex}");
            return false;
        }

        try
        {
            File.Delete(filePath);
            Debug.Log($"Delete succeeded. Slot: {slotIndex}, Path: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Delete failed for slot {slotIndex}: {ex.Message}");
            return false;
        }
    }
}

[Serializable]
public class SaveSlotMetadata
{
    public int saveVersion;
    public string saveTime;
    public string playerFactionName;
    public string playerPortraitId;
    public string displayDate;
    public int ownedCityCount;

    [NonSerialized] public int slotIndex;
    [NonSerialized] public DateTime fileLastWriteTime;
}
