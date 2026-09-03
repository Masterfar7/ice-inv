using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemWidget : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private TMP_Text subtitleLabel;

    public void Init(ItemData data)
    {
        icon.sprite = data.Icon;
        icon.preserveAspect = true;
        titleLabel.text = data.Title;
        subtitleLabel.text = $"Куплено за {data.Price} монет";
    }

    public void PlayAppearAnimation()
    {
        if (!gameObject.activeInHierarchy)
        {
            transform.localScale = Vector3.one;
            return;
        }

        StopAllCoroutines();
        StartCoroutine(AppearRoutine(0.35f));
    }

    private IEnumerator AppearRoutine(float duration)
    {
        Transform target = transform;
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(time / duration);

            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float eased = 1f + c3 * Mathf.Pow(k - 1f, 3f) + c1 * Mathf.Pow(k - 1f, 2f);

            target.localScale = Vector3.one * Mathf.LerpUnclamped(0.55f, 1f, eased);
            yield return null;
        }

        target.localScale = Vector3.one;
    }
}
