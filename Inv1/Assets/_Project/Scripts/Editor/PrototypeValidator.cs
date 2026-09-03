using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PrototypeValidator
{
    [MenuItem("Prototype/Валидация (ссылки, логика, сохранение)")]
    public static void Run()
    {
        int failures = 0;
        failures += CheckItems();
        failures += CheckPrefabs();
        failures += CheckScene();
        failures += CheckPurchaseLogic();
        failures += CheckSaveRoundtrip();

        if (failures == 0)
            Debug.Log("[Validator] ВСЕ ПРОВЕРКИ ПРОЙДЕНЫ.");
        else
            Debug.LogError($"[Validator] Провалено проверок: {failures}.");
    }

    private static int CheckItems()
    {
        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/_Project/Items" });
        var ids = new HashSet<string>();
        int failures = 0;

        foreach (string guid in guids)
        {
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if (string.IsNullOrEmpty(item.Id) || string.IsNullOrEmpty(item.Title) || item.Price <= 0 || item.Icon == null)
            {
                Debug.LogError($"[Validator] Предмет заполнен не полностью: {item.name}");
                failures++;
            }
            if (!ids.Add(item.Id))
            {
                Debug.LogError($"[Validator] Дубликат ID: {item.Id}");
                failures++;
            }
        }

        if (guids.Length < 10)
        {
            Debug.LogError($"[Validator] Предметов меньше 10: {guids.Length}");
            failures++;
        }

        Debug.Log($"[Validator] Предметы: {guids.Length} шт., уникальные ID, все поля заполнены.");
        return failures;
    }

    private static int CheckPrefabs()
    {
        int failures = 0;

        var shop = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/ShopItemWidget.prefab");
        var inv = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/InventoryItemWidget.prefab");
        if (shop == null || inv == null)
        {
            Debug.LogError("[Validator] Префабы не найдены.");
            return 1;
        }

        var shopWidget = shop.GetComponent<ShopItemWidget>();
        var invWidget = inv.GetComponent<InventoryItemWidget>();
        if (shopWidget == null || invWidget == null)
        {
            Debug.LogError("[Validator] На префабах нет компонентов виджетов.");
            failures++;
        }

        var shopSo = new SerializedObject(shopWidget);
        var invSo = new SerializedObject(invWidget);
        foreach (string prop in new[] { "icon", "titleLabel", "descriptionLabel", "priceLabel", "cardGroup", "buyButton", "buyButtonLabel" })
            if (shopSo.FindProperty(prop).objectReferenceValue == null)
            {
                Debug.LogError($"[Validator] ShopItemWidget: не заполнено поле {prop}.");
                failures++;
            }

        foreach (string prop in new[] { "icon", "titleLabel", "subtitleLabel" })
            if (invSo.FindProperty(prop).objectReferenceValue == null)
            {
                Debug.LogError($"[Validator] InventoryItemWidget: не заполнено поле {prop}.");
                failures++;
            }

        if (failures == 0)
            Debug.Log("[Validator] Префабы: компоненты и ссылки на месте.");
        return failures;
    }

    private static int CheckScene()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);
        int failures = 0;

        var manager = Object.FindFirstObjectByType<ShopManager>();
        if (manager == null)
        {
            Debug.LogError("[Validator] В сцене нет ShopManager.");
            return 1;
        }

        var so = new SerializedObject(manager);
        foreach (string prop in new[]
                 {
                     "shopItemPrefab", "inventoryItemPrefab", "shopContent", "inventoryContent",
                     "balanceLabel", "shopTabButton", "inventoryTabButton",
                     "shopPanel", "inventoryPanel", "inventoryEmptyHint"
                 })
        {
            if (so.FindProperty(prop).objectReferenceValue == null)
            {
                Debug.LogError($"[Validator] ShopManager: не заполнено поле {prop}.");
                failures++;
            }
        }

        int itemCount = so.FindProperty("items").arraySize;
        if (itemCount < 10)
        {
            Debug.LogError($"[Validator] В каталоге менеджера всего {itemCount} предметов.");
            failures++;
        }

        var scrollRects = Object.FindObjectsByType<UnityEngine.UI.ScrollRect>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (scrollRects.Length != 2)
        {
            Debug.LogError($"[Validator] ScrollRect в сцене: {scrollRects.Length} (ожидалось 2).");
            failures++;
        }

        foreach (UnityEngine.UI.ScrollRect scroll in scrollRects)
        {
            bool hasFitter = scroll.content != null && scroll.content.GetComponent<UnityEngine.UI.ContentSizeFitter>() != null;
            bool hasLayout = scroll.content != null && scroll.content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>() != null;
            if (!hasFitter || !hasLayout)
            {
                Debug.LogError($"[Validator] У контента {scroll.name} нет ContentSizeFitter/VerticalLayoutGroup.");
                failures++;
            }
        }

        var scaler = Object.FindFirstObjectByType<UnityEngine.UI.CanvasScaler>();
        if (scaler == null || scaler.uiScaleMode != UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            Debug.LogError("[Validator] CanvasScaler не в режиме ScaleWithScreenSize.");
            failures++;
        }

        var module = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (module == null)
        {
            Debug.LogError("[Validator] EventSystem отсутствует.");
            failures++;
        }

        if (failures == 0)
            Debug.Log($"[Validator] Сцена: менеджер заполнен ({itemCount} предметов), 2 Scroll View с авто-лейаутом, CanvasScaler ScaleWithScreenSize.");
        return failures;
    }

    private static int CheckPurchaseLogic()
    {
        var sword = ScriptableObject.CreateInstance<ItemData>();
        var swordSo = new SerializedObject(sword);
        swordSo.FindProperty("id").stringValue = "sword";
        swordSo.FindProperty("price").intValue = 120;
        swordSo.ApplyModifiedPropertiesWithoutUndo();

        var map = ScriptableObject.CreateInstance<ItemData>();
        var mapSo = new SerializedObject(map);
        mapSo.FindProperty("id").stringValue = "map";
        mapSo.FindProperty("price").intValue = 600;
        mapSo.ApplyModifiedPropertiesWithoutUndo();

        var state = new PlayerState();
        state.RestoreFrom(new SaveData(), new[] { "sword", "map" });

        int failures = 0;
        if (state.Balance != PlayerState.StartingBalance) { Debug.LogError("[Validator] Стартовый баланс неверный."); failures++; }
        if (!state.TryPurchase(sword)) { Debug.LogError("[Validator] Покупка меча должна была пройти."); failures++; }
        if (state.TryPurchase(sword)) { Debug.LogError("[Validator] Повторная покупка должна быть запрещена."); failures++; }
        if (state.TryPurchase(map)) { Debug.LogError("[Validator] Покупка карты за 600 при остатке 380 должна быть запрещена."); failures++; }
        if (state.Balance != PlayerState.StartingBalance - 120) { Debug.LogError("[Validator] Баланс после покупки неверный."); failures++; }
        if (!state.Owns("sword") || state.Owns("map")) { Debug.LogError("[Validator] Список купленных предметов неверный."); failures++; }

        Object.DestroyImmediate(sword);
        Object.DestroyImmediate(map);

        if (failures == 0)
            Debug.Log("[Validator] Логика покупок: баланс, повторные покупки, нехватка средств — всё верно.");
        return failures;
    }

    private static int CheckSaveRoundtrip()
    {
        var data = new SaveData { balance = 317 };
        data.purchasedItemIds.AddRange(new[] { "torch", "rope", "bread_travel" });

        string path = Path.Combine(Application.persistentDataPath, "shop_save.json");
        File.WriteAllText(path, JsonUtility.ToJson(data, true));

        SaveData loaded = SaveSystem.Load();
        int failures = 0;
        if (loaded == null || loaded.balance != 317 || !loaded.purchasedItemIds.SequenceEqual(data.purchasedItemIds))
        {
            Debug.LogError("[Validator] Roundtrip JSON-сохранения не сошёлся.");
            failures++;
        }

        File.Delete(path);

        if (failures == 0)
            Debug.Log($"[Validator] Сохранение: JSON roundtrip по пути {path} — верно (тестовый файл удалён).");
        return failures;
    }
}
