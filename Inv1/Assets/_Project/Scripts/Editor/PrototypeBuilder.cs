using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using TMPro;
using ShopPrototype.EditorTools;

public static class PrototypeBuilder
{
    private const string ProjectDir = "Assets/_Project";
    private const string ArtDir = ProjectDir + "/Art";
    private const string IconsDir = ArtDir + "/Icons";
    private const string ItemsDir = ProjectDir + "/Items";
    private const string PrefabsDir = ProjectDir + "/Prefabs";
    private const string ScenePath = "Assets/Scenes/Main.unity";
    private const string FontPath = ArtDir + "/SegoeUI_SDF.asset";
    private const string RoundRectPath = ArtDir + "/RoundRect.png";

    private static readonly Color RootBg = Hex("16161E");
    private static readonly Color ViewportBg = Hex("1B1B26");
    private static readonly Color CardBg = Hex("232331");
    private static readonly Color CardIconBg = Hex("2F2E3C");
    private static readonly Color PillBg = Hex("262635");
    private static readonly Color AccentPurple = Hex("8B5CF6");
    private static readonly Color AccentGreen = Hex("3ECF6E");
    private static readonly Color GoldPrice = Hex("FFC85C");
    private static readonly Color WhiteText = Hex("F2F2F7");
    private static readonly Color GreyText = Hex("A8A8BC");
    private static readonly Color DimText = Hex("70708A");
    private static readonly Color ScrollHandle = Hex("4A4A5E");
    private static readonly Color CameraBg = Hex("101016");
    private static readonly Color BuyLabelColor = Hex("0E2417");

    private static TMP_FontAsset uiFont;
    private static Sprite roundRectSprite;

    [MenuItem("Prototype/Сгенерировать всё (сцена, предметы, префабы)")]
    public static void GenerateAll()
    {
        EnsureFolders();
        EnsureTmpEssentials();

        uiFont = BuildOrLoadFont();
        roundRectSprite = BuildOrLoadRoundRect();
        List<ItemData> items = BuildItems();
        GameObject shopPrefab = BuildShopItemPrefab(items);
        GameObject inventoryPrefab = BuildInventoryItemPrefab(items);
        BuildScene(items, shopPrefab, inventoryPrefab);

        Debug.Log("[PrototypeBuilder] Готово: сцена Main, 12 предметов, префабы и UI созданы.");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "_Project");
        EnsureFolder(ProjectDir, "Art");
        EnsureFolder(ArtDir, "Icons");
        EnsureFolder(ProjectDir, "Items");
        EnsureFolder(ProjectDir, "Prefabs");
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, name);
    }

    private static void EnsureTmpEssentials()
    {
        if (TMP_Settings.instance == null)
            Debug.LogWarning("[PrototypeBuilder] TMP Settings не найден — импортируйте Window → TextMeshPro → Import TMP Essential Resources.");
    }

    private static TMP_FontAsset BuildOrLoadFont()
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (existing != null)
            return existing;

        TMP_FontAsset fontAsset;

        const string segoeFilePath = @"C:\Windows\Fonts\segoeui.ttf";
        if (File.Exists(segoeFilePath))
        {
            fontAsset = TMP_FontAsset.CreateFontAsset(
                segoeFilePath, 0, 48, 9, GlyphRenderMode.SDFAA, 2048, 1024);
        }
        else
        {
            fontAsset = TMP_FontAsset.CreateFontAsset("Segoe UI", "Regular", 48);
        }

        if (fontAsset == null)
            throw new System.InvalidOperationException("Не удалось создать шрифт Segoe UI SDF.");

        fontAsset.name = "Segoe UI SDF";

        string charset =
            "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ" +
            "абвгдеёжзийклмнопрстуфхцчшщъыьэюя" +
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz" +
            "0123456789 .,:;!?()[]«»—-+/№%";

        if (!fontAsset.TryAddCharacters(charset, out string missing))
            Debug.LogWarning($"[PrototypeBuilder] Глифы, которых нет в шрифте: {missing}");

        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;

        AssetDatabase.CreateAsset(fontAsset, FontPath);
        AssetDatabase.AddObjectToAsset(fontAsset.material, FontPath);
        foreach (Texture2D atlas in fontAsset.atlasTextures)
            if (atlas != null)
                AssetDatabase.AddObjectToAsset(atlas, FontPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(FontPath);
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
    }

    private static Sprite BuildOrLoadRoundRect()
    {
        if (!File.Exists(RoundRectPath))
            File.WriteAllBytes(RoundRectPath, IconPainter.RenderRoundRectPng(96, 26));

        return ImportSprite(RoundRectPath, border: new Vector4(26, 26, 26, 26));
    }

    private static Sprite ImportSprite(string path, Vector4? border = null)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear;
        importer.alphaIsTransparency = true;
        importer.spritePixelsPerUnit = 100f;
        if (border.HasValue)
            importer.spriteBorder = border.Value;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private struct ItemDef
    {
        public string Id, Title, Description;
        public int Price;
        public IconShape Shape;
        public string Hex;

        public ItemDef(string id, string title, string description, int price, IconShape shape, string hex)
        {
            Id = id; Title = title; Description = description; Price = price; Shape = shape; Hex = hex;
        }
    }

    private static readonly ItemDef[] ItemDefs =
    {
        new ItemDef("sword_wooden", "Тренировочный меч", "Деревянный меч для первых шагов в ратном деле.", 120, IconShape.Sword, "C98F4A"),
        new ItemDef("sword_iron", "Железный меч", "Надёжный клинок из кузницы северных гор.", 340, IconShape.Sword, "9DB2C7"),
        new ItemDef("shield_oak", "Дубовый щит", "Крепкий щит — выдержит даже удар тролля.", 260, IconShape.Shield, "B08A5A"),
        new ItemDef("bow_hunter", "Охотничий лук", "Тисовый лук с тетивой из драконьего шёлка.", 300, IconShape.Bow, "7BC47F"),
        new ItemDef("potion_health", "Зелье здоровья", "Восстанавливает силы после тяжёлого боя.", 80, IconShape.Potion, "E5484D"),
        new ItemDef("potion_mana", "Зелье маны", "Возвращает магическую энергию её расточительному владельцу.", 90, IconShape.Potion, "4C7DE0"),
        new ItemDef("bread_travel", "Хлеб путника", "Чёрствый, но сытный. Наедаешься на весь день.", 25, IconShape.Bread, "D9A05B"),
        new ItemDef("cheese_mountain", "Горный сыр", "Выдержанный сыр с острым высокогорным характером.", 35, IconShape.Cheese, "E9C46A"),
        new ItemDef("torch", "Факел", "Осветит даже самый тёмный подвал заброшенной башни.", 40, IconShape.Torch, "F76B15"),
        new ItemDef("rope", "Верёвка", "Десять локтей прочной пеньковой верёвки.", 30, IconShape.Rope, "C9A66B"),
        new ItemDef("map_treasure", "Карта сокровищ", "Потёртая карта с отметкой в форме креста. Проверим?", 600, IconShape.Map, "D8C3A5"),
        new ItemDef("amulet_luck", "Амулет удачи", "Говорят, приносит владельцу удачу. Говорят.", 850, IconShape.Amulet, "9B7EDE"),
    };

    private static List<ItemData> BuildItems()
    {
        var result = new List<ItemData>();

        foreach (ItemDef def in ItemDefs)
        {
            string iconPath = $"{IconsDir}/{def.Id}.png";
            if (!File.Exists(iconPath))
                File.WriteAllBytes(iconPath, IconPainter.RenderPng(def.Shape, Hex(def.Hex)));

            Sprite icon = ImportSprite(iconPath);

            string assetPath = $"{ItemsDir}/Item_{def.Id}.asset";
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(item, assetPath);
            }

            var so = new SerializedObject(item);
            so.FindProperty("id").stringValue = def.Id;
            so.FindProperty("title").stringValue = def.Title;
            so.FindProperty("description").stringValue = def.Description;
            so.FindProperty("price").intValue = def.Price;
            so.FindProperty("icon").objectReferenceValue = icon;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(item);
            result.Add(item);
        }

        AssetDatabase.SaveAssets();
        return result;
    }

    private static GameObject BuildShopItemPrefab(List<ItemData> items)
    {
        string path = PrefabsDir + "/ShopItemWidget.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            return existing;

        var root = new GameObject("ShopItemWidget", typeof(RectTransform));
        RectTransform rootRt = (RectTransform)root.transform;
        rootRt.sizeDelta = new Vector2(0f, 150f);

        Image cardImage = root.AddComponent<Image>();
        cardImage.sprite = roundRectSprite;
        cardImage.type = Image.Type.Sliced;
        cardImage.color = CardBg;
        cardImage.raycastTarget = false;

        CanvasGroup group = root.AddComponent<CanvasGroup>();
        ShopItemWidget widget = root.AddComponent<ShopItemWidget>();
        LayoutElement layout = root.AddComponent<LayoutElement>();
        layout.minHeight = 150f;
        layout.preferredHeight = 150f;

        RectTransform iconBg = At(Rt("IconBg", rootRt), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(110f, 110f));
        Image iconBgImage = Img(iconBg, roundRectSprite, CardIconBg, sliced: true);
        iconBgImage.raycastTarget = false;

        RectTransform iconRt = Fill(Rt("Icon", iconBg));
        iconRt.offsetMin = new Vector2(9f, 9f);
        iconRt.offsetMax = new Vector2(-9f, -9f);
        Image iconImg = Img(iconRt, items[0].Icon, Color.white, sliced: false);
        iconImg.raycastTarget = false;

        RectTransform titleRt = At(Rt("TitleLabel", rootRt), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(152f, -16f), new Vector2(500f, 40f));
        TextMeshProUGUI titleLabel = Txt(titleRt, "Название", 32f, WhiteText, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);

        RectTransform descRt = At(Rt("DescriptionLabel", rootRt), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(152f, -60f), new Vector2(560f, 66f));
        TextMeshProUGUI descriptionLabel = Txt(descRt, "Описание", 21f, GreyText, FontStyles.Normal, TextAlignmentOptions.TopLeft);

        RectTransform priceRt = At(Rt("PriceLabel", rootRt), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-36f, -18f), new Vector2(280f, 42f));
        TextMeshProUGUI priceLabel = Txt(priceRt, "0", 32f, GoldPrice, FontStyles.Bold, TextAlignmentOptions.Right);

        RectTransform buyRt = At(Rt("BuyButton", rootRt), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-36f, 20f), new Vector2(280f, 60f));
        Image buyImage = Img(buyRt, roundRectSprite, AccentGreen, sliced: true);
        Button buyButton = buyRt.gameObject.AddComponent<Button>();
        buyButton.targetGraphic = buyImage;
        buyButton.colors = TintBlock(Hex("FFFFFF"), Hex("E9FFF0"), Hex("C7E9D6"), Hex("787884"));

        RectTransform buyLabelRt = Fill(Rt("Label", buyRt));
        buyLabelRt.offsetMin = Vector2.zero;
        buyLabelRt.offsetMax = Vector2.zero;
        TextMeshProUGUI buyButtonLabel = Txt(buyLabelRt, "КУПИТЬ", 26f, BuyLabelColor, FontStyles.Bold, TextAlignmentOptions.Center);

        var so = new SerializedObject(widget);
        SetRef(so, "icon", iconImg);
        SetRef(so, "titleLabel", titleLabel);
        SetRef(so, "descriptionLabel", descriptionLabel);
        SetRef(so, "priceLabel", priceLabel);
        SetRef(so, "cardGroup", group);
        SetRef(so, "buyButton", buyButton);
        SetRef(so, "buyButtonLabel", buyButtonLabel);
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject BuildInventoryItemPrefab(List<ItemData> items)
    {
        string path = PrefabsDir + "/InventoryItemWidget.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            return existing;

        var root = new GameObject("InventoryItemWidget", typeof(RectTransform));
        RectTransform rootRt = (RectTransform)root.transform;
        rootRt.sizeDelta = new Vector2(0f, 96f);

        Image cardImage = root.AddComponent<Image>();
        cardImage.sprite = roundRectSprite;
        cardImage.type = Image.Type.Sliced;
        cardImage.color = CardBg;
        cardImage.raycastTarget = false;

        InventoryItemWidget widget = root.AddComponent<InventoryItemWidget>();
        LayoutElement layout = root.AddComponent<LayoutElement>();
        layout.minHeight = 96f;
        layout.preferredHeight = 96f;

        RectTransform iconBg = At(Rt("IconBg", rootRt), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(72f, 72f));
        Image iconBgImage = Img(iconBg, roundRectSprite, CardIconBg, sliced: true);
        iconBgImage.raycastTarget = false;

        RectTransform iconRt = Fill(Rt("Icon", iconBg));
        iconRt.offsetMin = new Vector2(7f, 7f);
        iconRt.offsetMax = new Vector2(-7f, -7f);
        Image iconImg = Img(iconRt, items[0].Icon, Color.white);
        iconImg.raycastTarget = false;

        RectTransform titleRt = At(Rt("TitleLabel", rootRt), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(108f, -12f), new Vector2(700f, 36f));
        TextMeshProUGUI titleLabel = Txt(titleRt, "Название", 28f, WhiteText, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);

        RectTransform subtitleRt = At(Rt("SubtitleLabel", rootRt), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(108f, -54f), new Vector2(700f, 30f));
        TextMeshProUGUI subtitleLabel = Txt(subtitleRt, "Подпись", 20f, GreyText, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);

        var so = new SerializedObject(widget);
        SetRef(so, "icon", iconImg);
        SetRef(so, "titleLabel", titleLabel);
        SetRef(so, "subtitleLabel", subtitleLabel);
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void BuildScene(List<ItemData> items, GameObject shopPrefab, GameObject inventoryPrefab)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera", typeof(Camera));
        camGo.tag = "MainCamera";
        Camera cam = camGo.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = CameraBg;
        cam.orthographic = true;
        cam.orthographicSize = 5f;

        var lightGo = new GameObject("Directional Light", typeof(Light));
        Light light = lightGo.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        _ = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var canvasGo = new GameObject("ShopCanvas", typeof(Canvas));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        _ = canvasGo.AddComponent<GraphicRaycaster>();

        RectTransform rootRt = Fill(Rt("Root", canvasGo.transform));
        Image rootImg = Img(rootRt, null, RootBg);
        rootImg.raycastTarget = false;

        RectTransform header = At(Rt("Header", rootRt), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(-80f, 110f));

        RectTransform titleRt = At(Rt("TitleText", header), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(760f, 70f));
        _ = Txt(titleRt, "ЛАВКА СТРАННИКА", 52f, WhiteText, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);

        RectTransform pillRt = At(Rt("BalancePill", header), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(360f, 76f));
        Image pillImg = Img(pillRt, roundRectSprite, PillBg, sliced: true);
        pillImg.raycastTarget = false;
        BalanceLabel balanceLabel = pillRt.gameObject.AddComponent<BalanceLabel>();

        RectTransform captionRt = At(Rt("Caption", pillRt), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(26f, 0f), new Vector2(150f, 40f));
        _ = Txt(captionRt, "МОНЕТ", 26f, GreyText, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);

        RectTransform amountRt = At(Rt("Amount", pillRt), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-28f, 0f), new Vector2(200f, 52f));
        TextMeshProUGUI amountText = Txt(amountRt, "500", 42f, GoldPrice, FontStyles.Bold, TextAlignmentOptions.Right);

        var balanceSo = new SerializedObject(balanceLabel);
        SetRef(balanceSo, "amountText", amountText);
        SetRef(balanceSo, "pill", pillRt);
        balanceSo.ApplyModifiedPropertiesWithoutUndo();

        RectTransform tabBar = At(Rt("TabBar", rootRt), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -142f), new Vector2(-80f, 64f));

        TabButton shopTab = BuildTab(tabBar, "ShopTab", "МАГАЗИН", 0f);
        TabButton inventoryTab = BuildTab(tabBar, "InventoryTab", "ИНВЕНТАРЬ", 344f);

        RectTransform panels = Rt("Panels", rootRt);
        Box(panels, Vector2.zero, Vector2.one, new Vector2(0f, 20f), new Vector2(0f, -222f));

        RectTransform shopPanel = Fill(Rt("ShopPanel", panels));
        CreateScrollView("ShopScroll", shopPanel, out RectTransform shopContent);

        RectTransform inventoryPanel = Fill(Rt("InventoryPanel", panels));
        CreateScrollView("InventoryScroll", inventoryPanel, out RectTransform inventoryContent);

        RectTransform hintRt = Fill(Rt("EmptyHint", inventoryPanel));
        hintRt.offsetMin = new Vector2(0f, -120f);
        hintRt.offsetMax = new Vector2(0f, 120f);
        TextMeshProUGUI hintText = Txt(hintRt, "Инвентарь пуст.\nЗагляните в магазин!", 36f, DimText, FontStyles.Bold, TextAlignmentOptions.Center);
        hintText.raycastTarget = false;

        var gameRootGo = new GameObject("=== GameRoot ===");
        ShopManager manager = gameRootGo.AddComponent<ShopManager>();

        var so = new SerializedObject(manager);
        var itemsProp = so.FindProperty("items");
        itemsProp.arraySize = items.Count;
        for (int i = 0; i < items.Count; i++)
            itemsProp.GetArrayElementAtIndex(i).objectReferenceValue = items[i];

        SetRef(so, "shopItemPrefab", shopPrefab.GetComponent<ShopItemWidget>());
        SetRef(so, "inventoryItemPrefab", inventoryPrefab.GetComponent<InventoryItemWidget>());
        SetRef(so, "shopContent", shopContent);
        SetRef(so, "inventoryContent", inventoryContent);
        SetRef(so, "balanceLabel", balanceLabel);
        SetRef(so, "shopTabButton", shopTab);
        SetRef(so, "inventoryTabButton", inventoryTab);
        SetRef(so, "shopPanel", shopPanel.gameObject);
        SetRef(so, "inventoryPanel", inventoryPanel.gameObject);
        SetRef(so, "inventoryEmptyHint", hintRt.gameObject);
        so.ApplyModifiedPropertiesWithoutUndo();

        inventoryPanel.gameObject.SetActive(false);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }

    private static TabButton BuildTab(RectTransform parent, string name, string label, float x)
    {
        RectTransform rt = At(Rt(name, parent), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(x, 0f), new Vector2(328f, 60f));
        Image bg = Img(rt, roundRectSprite, Hex("2A2A3A"), sliced: true);

        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = bg;
        button.colors = TintBlock(Hex("FFFFFF"), Hex("FFFFFF"), Hex("CFCFDA"), Hex("CFCFDA"));

        RectTransform labelRt = Fill(Rt("Label", rt));
        TextMeshProUGUI labelTxt = Txt(labelRt, label, 28f, WhiteText, FontStyles.Bold, TextAlignmentOptions.Center);

        TabButton tab = rt.gameObject.AddComponent<TabButton>();
        var so = new SerializedObject(tab);
        SetRef(so, "button", button);
        SetRef(so, "background", bg);
        SetRef(so, "label", labelTxt);
        so.ApplyModifiedPropertiesWithoutUndo();
        return tab;
    }

    private static void CreateScrollView(string name, RectTransform parent, out RectTransform content)
    {
        RectTransform scrollRt = Fill(Rt(name, parent));

        RectTransform viewport = Fill(Rt("Viewport", scrollRt));
        viewport.offsetMax = new Vector2(-22f, 0f);
        Image viewportImg = Img(viewport, null, ViewportBg);
        viewportImg.raycastTarget = false;
        viewport.gameObject.AddComponent<RectMask2D>();

        content = At(Rt("Content", viewport), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);

        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 18, 18);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform barRt = Rt("Scrollbar Vertical", scrollRt);
        barRt.anchorMin = new Vector2(1f, 0f);
        barRt.anchorMax = new Vector2(1f, 1f);
        barRt.pivot = new Vector2(1f, 0.5f);
        barRt.sizeDelta = new Vector2(14f, 0f);
        barRt.anchoredPosition = Vector2.zero;

        RectTransform sliding = Fill(Rt("Sliding Area", barRt));
        RectTransform handle = Fill(Rt("Handle", sliding));
        handle.offsetMin = new Vector2(2f, 2f);
        handle.offsetMax = new Vector2(-2f, -2f);
        Image handleImg = Img(handle, roundRectSprite, ScrollHandle, sliced: true);

        var scrollbar = barRt.gameObject.AddComponent<Scrollbar>();
        scrollbar.targetGraphic = handleImg;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.value = 1f;

        var scrollRect = scrollRt.gameObject.AddComponent<ScrollRect>();
        scrollRect.content = content;
        scrollRect.viewport = viewport;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 40f;
    }

    private static RectTransform Rt(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static RectTransform Fill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    private static RectTransform Box(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return rt;
    }

    private static RectTransform At(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return rt;
    }

    private static Image Img(RectTransform rt, Sprite sprite, Color color, bool sliced = false)
    {
        var image = rt.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        if (sliced && sprite != null)
            image.type = Image.Type.Sliced;
        return image;
    }

    private static TextMeshProUGUI Txt(RectTransform rt, string text, float size, Color color, FontStyles styles, TextAlignmentOptions alignment)
    {
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.font = uiFont;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = styles;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static ColorBlock TintBlock(Color normal, Color highlighted, Color pressed, Color disabled)
    {
        return new ColorBlock
        {
            normalColor = normal,
            highlightedColor = highlighted,
            pressedColor = pressed,
            selectedColor = highlighted,
            disabledColor = disabled,
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };
    }

    private static void SetRef(SerializedObject so, string property, Object value)
    {
        so.FindProperty(property).objectReferenceValue = value;
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color color);
        return color;
    }
}
