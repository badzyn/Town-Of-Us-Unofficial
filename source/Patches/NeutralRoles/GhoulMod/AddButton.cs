using UnityEngine;
using UnityEngine.UI;
using System;

namespace TownOfUs.GhoulMod
{
    public class CustomButton
    {
        private GameObject buttonObject;
        private PassiveButton passiveButton;
        private TMPro.TMP_Text buttonText;
        private Func<bool> hasButton;
        private Func<float> getCooldown;
        private Action onClickAction;

        public CustomButton(Action action, Func<bool> hasButton, Func<float> getCooldown, string buttonTextStr, HudManager hud, Vector2 anchoredPosition)
        {
            try
            {
                this.hasButton = hasButton;
                this.getCooldown = getCooldown;
                this.onClickAction = action;

                Debug.Log("Creating EatButton from scratch");

                // Tworzenie nowego obiektu przycisku
                buttonObject = new GameObject("EatButton");
                buttonObject.transform.SetParent(hud.transform);
                buttonObject.transform.SetAsLastSibling();
                Debug.Log($"EatButton hierarchy set: {buttonObject.transform.GetSiblingIndex()}");

                // Dodanie RectTransform
                var rect = buttonObject.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(1, 0); // Kotwica w prawym dolnym rogu
                rect.anchorMax = new Vector2(1, 0);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(100, 100); // Rozmiar w pikselach
                rect.anchoredPosition = anchoredPosition; // Absolutna pozycja
                Debug.Log($"EatButton position: anchoredPosition={rect.anchoredPosition}, sizeDelta={rect.sizeDelta}");

                // Dodanie Image
                var image = buttonObject.AddComponent<Image>();
                if (image != null)
                {
                    // Użycie domyślnego sprite'a (biały kwadrat dla testów)
                    image.sprite = Sprite.Create(
                        Texture2D.whiteTexture,
                        new Rect(0, 0, 100, 100),
                        new Vector2(0.5f, 0.5f)
                    );
                    image.color = Color.gray; // Szary dla widoczności
                    Debug.Log("Image component added with default sprite");
                }
                else
                {
                    Debug.LogError("Failed to add Image component!");
                }

                // Dodanie PassiveButton
                passiveButton = buttonObject.AddComponent<PassiveButton>();
                if (passiveButton == null)
                {
                    Debug.LogError("Failed to add PassiveButton!");
                    return;
                }
                Debug.Log("PassiveButton added successfully");

                // Dodanie tekstu
                var textObject = new GameObject("ButtonText");
                textObject.transform.SetParent(buttonObject.transform);
                buttonText = textObject.AddComponent<TMPro.TMP_Text>();
                if (buttonText != null)
                {
                    buttonText.text = buttonTextStr;
                    buttonText.fontSize = 20;
                    buttonText.alignment = TMPro.TextAlignmentOptions.Center;
                    buttonText.color = Color.white;
                    var textRect = textObject.GetComponent<RectTransform>();
                    textRect.sizeDelta = new Vector2(100, 30);
                    textRect.anchoredPosition = Vector2.zero;
                    Debug.Log($"Button text set to: {buttonTextStr}");
                }
                else
                {
                    Debug.LogError("Failed to add TMP_Text!");
                }

                // Ustawienie listenera
                passiveButton.OnClick.RemoveAllListeners();
                passiveButton.OnClick.AddListener((UnityEngine.Events.UnityAction)delegate
                {
                    if (hasButton())
                    {
                        Debug.Log("EatButton clicked!");
                        onClickAction();
                    }
                    else
                    {
                        Debug.Log("EatButton click ignored: hasButton returned false");
                    }
                });
                Debug.Log("EatButton listener set");

                buttonObject.SetActive(false);
                Debug.Log($"EatButton created successfully: active={buttonObject.activeSelf}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CustomButton creation error: {e.Message}\nStackTrace: {e.StackTrace}");
            }
        }

        public void SetActive(bool active)
        {
            if (buttonObject != null)
            {
                buttonObject.SetActive(active);
                Debug.Log($"EatButton SetActive: {active}");
            }
            else
            {
                Debug.LogError("Cannot SetActive: buttonObject is null");
            }
        }

        public void SetCoolDown(float current, float max)
        {
            if (buttonObject == null) return;
            var image = buttonObject.GetComponent<Image>();
            if (image != null)
            {
                float fill = max > 0 ? 1 - (current / max) : 1;
                image.fillAmount = Mathf.Clamp01(fill);
                Debug.Log($"EatButton SetCoolDown: current={current}, max={max}, fill={fill}");
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (passiveButton != null)
            {
                passiveButton.enabled = interactable && hasButton();
                Debug.Log($"EatButton SetInteractable: interactable={interactable}, hasButton={hasButton()}");
            }
            else
            {
                Debug.LogError("Cannot SetInteractable: passiveButton is null");
            }
        }
    }
}