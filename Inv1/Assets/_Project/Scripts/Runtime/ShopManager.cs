using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [Header("Каталог товаров (ScriptableObject'ы)")]
    [SerializeField] private ItemData[] items;

    [Header("Префабы карточек")]
    [SerializeField] private ShopItemWidget shopItemPrefab;
    [SerializeField] private InventoryItemWidget inventoryItemPrefab;

    [Header("Контейнеры списков (Content у Scroll View)")]
    [SerializeField] private RectTransform shopContent;
    [SerializeField] private RectTransform inventoryContent;

    [Header("Баланс")]
    [SerializeField] private BalanceLabel balanceLabel;

    [Header("Вкладки")]
    [SerializeField] private TabButton shopTabButton;
    [SerializeField] private TabButton inventoryTabButton;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject inventoryPanel;

    [Header("Прочее")]
    [SerializeField] private GameObject inventoryEmptyHint;

    private readonly PlayerState state = new PlayerState();
    private readonly Dictionary<string, ItemData> catalog = new Dictionary<string, ItemData>();
    private readonly Dictionary<string, ShopItemWidget> shopCards = new Dictionary<string, ShopItemWidget>();
    private readonly Dictionary<string, InventoryItemWidget> inventoryCards = new Dictionary<string, InventoryItemWidget>();

    private void Awake()
    {
        foreach (ItemData item in items)
            catalog[item.Id] = item;

        state.BalanceChanged += HandleBalanceChanged;

        state.RestoreFrom(SaveSystem.Load(), catalog.Keys);

        BuildShop();
        RebuildInventory();
        balanceLabel.SetInstant(state.Balance);
        RefreshShopCards();

        shopTabButton.Button.onClick.AddListener(OpenShopTab);
        inventoryTabButton.Button.onClick.AddListener(OpenInventoryTab);
        OpenShopTab();
    }

    private void OnDestroy()
    {
        state.BalanceChanged -= HandleBalanceChanged;
    }

    public void TryBuy(ItemData item)
    {
        if (!state.TryPurchase(item))
            return;

        Debug.Log($"[Shop] Куплен «{item.Title}» за {item.Price}. Баланс: {state.Balance}");

        SaveNow();
        balanceLabel.AnimateTo(state.Balance);

        if (shopCards.TryGetValue(item.Id, out ShopItemWidget card))
            card.PlayPurchasePulse();

        AddInventoryCard(item);
    }

    public void OpenShopTab()
    {
        shopPanel.SetActive(true);
        inventoryPanel.SetActive(false);
        shopTabButton.SetVisualState(true);
        inventoryTabButton.SetVisualState(false);
    }

    public void OpenInventoryTab()
    {
        shopPanel.SetActive(false);
        inventoryPanel.SetActive(true);
        shopTabButton.SetVisualState(false);
        inventoryTabButton.SetVisualState(true);
    }

    private void BuildShop()
    {
        foreach (ItemData item in items)
        {
            ShopItemWidget widget = Instantiate(shopItemPrefab, shopContent);
            widget.name = $"ShopCard_{item.Id}";
            widget.Init(item, TryBuy);
            shopCards[item.Id] = widget;
        }
    }

    private void RebuildInventory()
    {
        foreach (InventoryItemWidget widget in inventoryCards.Values)
            Destroy(widget.gameObject);

        inventoryCards.Clear();

        foreach (string id in state.OwnedItemIds)
            if (catalog.TryGetValue(id, out ItemData item))
                AddInventoryCard(item, animate: false);

        UpdateEmptyHint();
    }

    private void AddInventoryCard(ItemData item, bool animate = true)
    {
        InventoryItemWidget widget = Instantiate(inventoryItemPrefab, inventoryContent);
        widget.name = $"InvCard_{item.Id}";
        widget.Init(item);
        inventoryCards[item.Id] = widget;

        if (animate)
            widget.PlayAppearAnimation();

        UpdateEmptyHint();
    }

    private void UpdateEmptyHint()
    {
        if (inventoryEmptyHint != null)
            inventoryEmptyHint.SetActive(inventoryCards.Count == 0);
    }

    private void RefreshShopCards()
    {
        foreach (KeyValuePair<string, ShopItemWidget> pair in shopCards)
            pair.Value.Refresh(state.Owns(pair.Key), state.Balance);
    }

    private void HandleBalanceChanged(int newBalance)
    {
        RefreshShopCards();
    }

    private void SaveNow()
    {
        SaveSystem.Save(state.CaptureSaveData());
    }

    private void OnApplicationQuit() => SaveNow();

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            SaveNow();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveNow();
    }
}
