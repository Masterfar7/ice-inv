using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemWidget : MonoBehaviour
{
    [Header("Визуал")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private TMP_Text descriptionLabel;
    [SerializeField] private TMP_Text priceLabel;
    [SerializeField] private CanvasGroup cardGroup;

    [Header("Кнопка покупки")]
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyButtonLabel;

    private static readonly Color AffordablePriceColor = new Color32(0xFF, 0xC8, 0x5C, 0xFF);
    private static readonly Color TooExpensivePriceColor = new Color32(0xFF, 0x6B, 0x6B, 0xFF);

    private ItemData item;
    private System.Action<ItemData> purchaseRequested;

    public void Init(ItemData data, System.Action<ItemData> onPurchaseRequested)
    {
        item = data;
        purchaseRequested = onPurchaseRequested;

        icon.sprite = data.Icon;
        icon.preserveAspect = true;
        titleLabel.text = data.Title;
        descriptionLabel.text = data.Description;
        priceLabel.text = $"{data.Price}";

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(HandleBuyClicked);
    }

    public void Refresh(bool owned, int currentBalance)
    {
        if (owned)
        {
            cardGroup.alpha = 0.68f;
            buyButton.interactable = false;
            buyButtonLabel.text = "КУПЛЕНО";
            priceLabel.color = AffordablePriceColor;
            return;
        }

        cardGroup.alpha = 1f;
        bool affordable = currentBalance >= item.Price;
        buyButton.interactable = affordable;
        buyButtonLabel.text = affordable ? "КУПИТЬ" : "НЕ ХВАТАЕТ";
        priceLabel.color = affordable ? AffordablePriceColor : TooExpensivePriceColor;
    }

    public void PlayPurchasePulse()
    {
        if (!gameObject.activeInHierarchy)
            return;

        StopAllCoroutines();
        StartCoroutine(ScalePulseRoutine(1.28f, 0.45f));
    }

    private void HandleBuyClicked() => purchaseRequested?.Invoke(item);

    private IEnumerator ScalePulseRoutine(float peakScale, float duration)
    {
        Transform target = transform;
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(time / duration);
            float scale = 1f + (peakScale - 1f) * Mathf.Sin(Mathf.PI * k);
            target.localScale = Vector3.one * scale;
            yield return null;
        }

        target.localScale = Vector3.one;
    }
}
