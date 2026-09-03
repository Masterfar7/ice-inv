using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;

public class BalanceLabel : MonoBehaviour
{
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private RectTransform pill;
    [SerializeField, Min(0.05f)] private float countDuration = 0.7f;
    [SerializeField, Min(0.05f)] private float pulseDuration = 0.3f;
    [SerializeField] private float pulseScale = 1.12f;

    private Coroutine animationRoutine;
    private int displayedValue;

    public void SetInstant(int value)
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        displayedValue = value;
        amountText.SetText(Format(value));
    }

    public void AnimateTo(int value)
    {
        if (!gameObject.activeInHierarchy)
        {
            SetInstant(value);
            return;
        }

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(CountRoutine(displayedValue, value));
    }

    private IEnumerator CountRoutine(int from, int to)
    {
        float time = 0f;

        while (time < countDuration)
        {
            time += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(time / countDuration);
            float eased = 1f - Mathf.Pow(1f - k, 3f);

            displayedValue = Mathf.RoundToInt(Mathf.Lerp(from, to, eased));
            amountText.SetText(Format(displayedValue));
            yield return null;
        }

        displayedValue = to;
        amountText.SetText(Format(to));
        animationRoutine = null;

        StartCoroutine(PillPulseRoutine());
    }

    private IEnumerator PillPulseRoutine()
    {
        float time = 0f;

        while (time < pulseDuration)
        {
            time += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(time / pulseDuration);
            float scale = 1f + (pulseScale - 1f) * Mathf.Sin(Mathf.PI * k);
            pill.localScale = Vector3.one * scale;
            yield return null;
        }

        pill.localScale = Vector3.one;
    }

    private static string Format(int value)
    {
        return value.ToString("N0", CultureInfo.InvariantCulture).Replace(',', ' ');
    }
}
