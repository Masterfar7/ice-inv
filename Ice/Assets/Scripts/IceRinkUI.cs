using UnityEngine;
using UnityEngine.UI;

namespace IcePuck
{
    public class IceRinkUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PuckCollisionHandler collisionHandler;
        [SerializeField] private PuckController puckController;

        [Header("UI Text Components")]
        [SerializeField] private Text bounceText;
        [SerializeField] private Text forceText;
        [SerializeField] private Text instructionsText;
        [SerializeField] private Button resetButton;

        private void Start()
        {
            FindReferences();

            if (collisionHandler != null)
            {
                collisionHandler.OnBounceCountChanged += UpdateBounceUI;
                collisionHandler.OnPuckReset += OnPuckReset;
                UpdateBounceUI(collisionHandler.CurrentBounceCount);
            }

            if (resetButton != null)
            {
                resetButton.onClick.AddListener(OnResetButtonClicked);
            }

            if (instructionsText != null)
            {
                instructionsText.text = "<b>Управление:</b>\n" +
                                        "1. Зажмите ЛКМ на шайбе и потяните мышь\n" +
                                        "2. Чем дальше курсор — тем сильнее импульс\n" +
                                        "3. Отпустите ЛКМ для удара\n" +
                                        "4. Более 3 отскоков — автоматический сброс";
            }
        }

        private void OnDestroy()
        {
            if (collisionHandler != null)
            {
                collisionHandler.OnBounceCountChanged -= UpdateBounceUI;
                collisionHandler.OnPuckReset -= OnPuckReset;
            }
        }

        private void FindReferences()
        {
            if (collisionHandler == null)
                collisionHandler = FindFirstObjectByType<PuckCollisionHandler>();

            if (puckController == null)
                puckController = FindFirstObjectByType<PuckController>();
        }

        private void Update()
        {
            if (puckController != null && forceText != null)
            {
                if (puckController.IsDragging)
                {
                    forceText.text = $"Сила impulse: <b>{puckController.CurrentForceMagnitude:F1}</b>";
                }
                else
                {
                    forceText.text = "Сила impulse: 0.0";
                }
            }
        }

        private void UpdateBounceUI(int currentBounces)
        {
            if (bounceText != null && collisionHandler != null)
            {
                bounceText.text = $"Отскоки: <b>{currentBounces} / {collisionHandler.MaxAllowedBounces}</b>";
                if (currentBounces >= collisionHandler.MaxAllowedBounces)
                {
                    bounceText.color = Color.red;
                }
                else
                {
                    bounceText.color = Color.white;
                }
            }
        }

        private void OnPuckReset()
        {
            if (bounceText != null)
            {
                bounceText.text = "Отскоки: <b>0 / 3</b> (Сброс)";
                bounceText.color = Color.cyan;
            }
        }

        private void OnResetButtonClicked()
        {
            if (collisionHandler != null)
            {
                collisionHandler.ResetToInitialPosition();
            }
        }
    }
}
