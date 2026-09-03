using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class SaveData
{
    public int balance;
    public List<string> purchasedItemIds = new List<string>();
}

public static class SaveSystem
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "shop_save.json");

    public static SaveData Load()
    {
        try
        {
            if (!File.Exists(SavePath))
            {
                Debug.Log($"[SaveSystem] Файл сохранения не найден ({SavePath}), используются значения по умолчанию.");
                return null;
            }

            string json = File.ReadAllText(SavePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            Debug.Log($"[SaveSystem] Загружено: баланс {data.balance}, предметов {data.purchasedItemIds.Count}.");
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Ошибка загрузки сохранения: {e.Message}");
            return null;
        }
    }

    public static void Save(SaveData data)
    {
        try
        {
            string directory = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            Debug.Log($"[SaveSystem] Сохранено в {SavePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Ошибка сохранения: {e.Message}");
        }
    }
}
