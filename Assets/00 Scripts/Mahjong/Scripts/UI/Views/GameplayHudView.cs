using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using MahjongOut3D.Gameplay;
using MahjongOut3D.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongOut3D.UI
{
    /// <summary>
    /// Displays gameplay progress, currency and power-up actions.
    /// </summary>
    public sealed class GameplayHudView : UIScreenView
    {
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text coinText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Image progressSlider;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button hintButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button shuffleButton;
        [SerializeField] private Button bombButton;
        [SerializeField] private Button xrayButton;
        [SerializeField] private BoosterHudBinding hintBooster;
        [SerializeField] private BoosterHudBinding undoBooster;
        [SerializeField] private BoosterHudBinding shuffleBooster;
        [SerializeField] private BoosterHudBinding bombBooster;
        [SerializeField] private BoosterHudBinding xrayBooster;
      //  [SerializeField] private TMP_Dropdown pieceMaterialDropdown;
        [SerializeField] private TMP_Text comboText;
        [SerializeField] private Image comboImage;

        private Action<int> pieceMaterialChangedCallback;
       // private bool suppressPieceMaterialDropdownCallback;
        private Sequence comboPopupSequence;
        private Sequence comboImageSequence;

        /// <summary>
        /// Binds HUD buttons to runtime callbacks.
        /// </summary>
        public void Bind(Action onPause, Action onHint, Action onUndo, Action onShuffle, Action onBomb, Action onXRay, Action<int> onPieceMaterialChanged)
        {
            BindButton(pauseButton, onPause);
            BindButton(hintButton, onHint);
            BindButton(undoButton, onUndo);
            BindButton(shuffleButton, onShuffle);
            BindButton(bombButton, onBomb);
            BindButton(xrayButton, onXRay);

            pieceMaterialChangedCallback = onPieceMaterialChanged;
          //  BindPieceMaterialDropdown();
        }

        /// <summary>
        /// Rebuilds the runtime piece-material dropdown options.
        /// </summary>
        // public void SetPieceMaterialOptions(IReadOnlyList<string> optionLabels, int selectedIndex)
        // {
        //     TMP_Dropdown dropdown = EnsurePieceMaterialDropdown();
        //     if (dropdown == null)
        //     {
        //         return;
        //     }

        //     bool hasOptions = optionLabels != null && optionLabels.Count > 0;
        //     dropdown.gameObject.SetActive(hasOptions);
        //     if (!hasOptions)
        //     {
        //         return;
        //     }

        //     List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>(optionLabels.Count);
        //     for (int index = 0; index < optionLabels.Count; index++)
        //     {
        //         options.Add(new TMP_Dropdown.OptionData(optionLabels[index]));
        //     }

        //   //  suppressPieceMaterialDropdownCallback = true;
        //     dropdown.ClearOptions();
        //     dropdown.AddOptions(options);
        //     dropdown.SetValueWithoutNotify(Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, options.Count - 1)));
        //     dropdown.RefreshShownValue();
        //    // suppressPieceMaterialDropdownCallback = false;
        // }

        /// <summary>
        /// Updates the visible coin counter.
        /// </summary>
        public void SetCoins(int coins)
        {
            if (coinText != null)
            {
                coinText.text = coins.ToString();
            }
        }

        /// <summary>
        /// Updates the visible gameplay level label.
        /// </summary>
        public void SetLevel(int levelIndex)
        {
            if (levelText != null)
            {
                levelText.text = $"Level {Mathf.Max(1, levelIndex + 1)}";
            }
        }

        /// <summary>
        /// Updates the visible progress indicator.
        /// </summary>
        public void SetProgress(int remainingTiles, int totalTiles, float completionRatio)
        {
            if (progressText != null)
            {
                progressText.text = $"{totalTiles - remainingTiles}/{totalTiles}";
            }

            if (progressSlider != null)
            {
                progressSlider.fillAmount = Mathf.Clamp01(completionRatio);
            }
        }

        /// <summary>
        /// Updates the quantity and icon state for one gameplay booster.
        /// </summary>
        public void SetBoosterCount(PowerUpType powerUpType, int count)
        {
            GetBoosterBinding(powerUpType).SetCount(count);
            UpdateBoosterButtonInteractable(powerUpType, count);
        }

        /// <summary>
        /// Updates every gameplay booster quantity shown in the HUD.
        /// </summary>
        public void SetBoosterCounts(int hintCount, int undoCount, int shuffleCount, int bombCount, int xrayCount)
        {
            hintBooster.SetCount(hintCount);
            undoBooster.SetCount(undoCount);
            shuffleBooster.SetCount(shuffleCount);
            bombBooster.SetCount(bombCount);
            xrayBooster.SetCount(xrayCount);

            UpdateBoosterButtonInteractable(PowerUpType.Hint, hintCount);
            UpdateBoosterButtonInteractable(PowerUpType.Undo, undoCount);
            UpdateBoosterButtonInteractable(PowerUpType.Shuffle, shuffleCount);
            UpdateBoosterButtonInteractable(PowerUpType.Bomb, bombCount);
            UpdateBoosterButtonInteractable(PowerUpType.XRay, xrayCount);
        }

        public void AddTestBooster()
        {
            SaveManager saveManager = UnityEngine.Object.FindAnyObjectByType<SaveManager>();
            if (saveManager != null)
            {
                saveManager.SetPowerUpCount(PowerUpType.Hint, 10);
                saveManager.SetPowerUpCount(PowerUpType.Undo, 10);
                saveManager.SetPowerUpCount(PowerUpType.Shuffle, 10);
                saveManager.SetPowerUpCount(PowerUpType.Bomb, 10);
                saveManager.SetPowerUpCount(PowerUpType.XRay, 10);
            }

            SetBoosterCounts(10, 10, 10, 10, 10);
        }

        /// <summary>
        /// Shows a floating combo popup. If no combo text is assigned, it creates one automatically.
        /// </summary>
        public void ShowComboText(int combo)
        {
            TMP_Text targetText = EnsureComboText();
            if (targetText == null)
            {
                return;
            }

            if (combo <= 1)
            {
                targetText.gameObject.SetActive(false);
                HideComboBurstImage();
                return;
            }

            if (comboPopupSequence != null)
            {
                comboPopupSequence.Kill(true);
                comboPopupSequence = null;
            }

            targetText.text = $"COMBO x{combo}";
            targetText.color = new Color(targetText.color.r, targetText.color.g, targetText.color.b, 1f);
            targetText.rectTransform.localScale = Vector3.one * 0.7f;
            targetText.rectTransform.anchoredPosition = new Vector2(0f, 300f);
            targetText.gameObject.SetActive(true);

            comboPopupSequence = DOTween.Sequence();
            comboPopupSequence.Append(targetText.rectTransform.DOAnchorPosY(500f, 0.42f).SetEase(Ease.OutCubic));
            comboPopupSequence.Join(targetText.rectTransform.DOScale(Vector3.one * 1.65f, 0.42f).SetEase(Ease.OutBack));
            comboPopupSequence.Append(targetText.rectTransform.DOScale(Vector3.one * 1.15f, 0.18f).SetEase(Ease.Linear));
            comboPopupSequence.Join(DOTween.To(() => targetText.color.a, alpha =>
            {
                Color color = targetText.color;
                color.a = alpha;
                targetText.color = color;
            }, 0f, 0.65f).SetDelay(0.18f));
            comboPopupSequence.OnComplete(() =>
            {
                targetText.gameObject.SetActive(false);
                Color hideColor = targetText.color;
                hideColor.a = 1f;
                targetText.color = hideColor;
                targetText.rectTransform.localScale = Vector3.one;
                comboPopupSequence = null;
            });

            if (combo % 5 == 0)
            {
                ShowComboBurstImage();
            }
            else
            {
                HideComboBurstImage();
            }
        }

private void ShowComboBurstImage()
{
    if (comboImage == null)
    {
        return;
    }

    if (comboImageSequence != null)
    {
        comboImageSequence.Kill(true);
        comboImageSequence = null;
    }

    comboImage.gameObject.SetActive(true);

    // Reset alpha
    Color color = comboImage.color;
    color.a = 0f;
    comboImage.color = color;

    comboImageSequence = DOTween.Sequence();

    // =========================
    // FADE IN - 0.15s
    // =========================

    comboImageSequence.Append(
        comboImage
            .DOFade(1f, 0.15f)
            .SetEase(Ease.OutQuad)
    );

    // =========================
    // GIỮ - 1.1s
    // =========================

    comboImageSequence.AppendInterval(1.1f);

    // =========================
    // FADE OUT - 0.75s
    // =========================

    comboImageSequence.Append(
        comboImage
            .DOFade(0f, 0.75f)
            .SetEase(Ease.InOutQuad)
    );

    // =========================
    // TẮT
    // =========================

    comboImageSequence.OnComplete(() =>
    {
        comboImage.gameObject.SetActive(false);

        Color resetColor = comboImage.color;
        resetColor.a = 1f;
        comboImage.color = resetColor;

        comboImageSequence = null;
    });
}
        private void HideComboBurstImage()
        {
            if (comboImageSequence != null)
            {
                comboImageSequence.Kill(true);
                comboImageSequence = null;
            }

            if (comboImage != null)
            {
                comboImage.gameObject.SetActive(false);
                Color color = comboImage.color;
                color.a = 1f;
                comboImage.color = color;
                comboImage.rectTransform.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// Binds a UI button when the reference exists.
        /// </summary>
        private static void BindButton(Button button, Action callback)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => callback?.Invoke());
        }

        // private void BindPieceMaterialDropdown()
        // {
        //     TMP_Dropdown dropdown = EnsurePieceMaterialDropdown();
        //     if (dropdown == null)
        //     {
        //         return;
        //     }

        //     dropdown.onValueChanged.RemoveAllListeners();
        //     dropdown.onValueChanged.AddListener(HandlePieceMaterialDropdownValueChanged);
        // }

        // private void HandlePieceMaterialDropdownValueChanged(int selectedIndex)
        // {
        //     if (suppressPieceMaterialDropdownCallback)
        //     {
        //         return;
        //     }

        //     pieceMaterialChangedCallback?.Invoke(selectedIndex);
        // }

        private BoosterHudBinding GetBoosterBinding(PowerUpType powerUpType)
        {
            switch (powerUpType)
            {
                case PowerUpType.Hint:
                    return hintBooster;
                case PowerUpType.Undo:
                    return undoBooster;
                case PowerUpType.Shuffle:
                    return shuffleBooster;
                case PowerUpType.Bomb:
                    return bombBooster;
                case PowerUpType.XRay:
                    return xrayBooster;
                default:
                    return default;
            }
        }

        private void UpdateBoosterButtonInteractable(PowerUpType powerUpType, int count)
        {
            Button boosterButton = GetBoosterButton(powerUpType);
            if (boosterButton != null)
            {
                boosterButton.interactable = count > 0;
            }
        }

        private Button GetBoosterButton(PowerUpType powerUpType)
        {
            switch (powerUpType)
            {
                case PowerUpType.Hint:
                    return hintButton;
                case PowerUpType.Undo:
                    return undoButton;
                case PowerUpType.Shuffle:
                    return shuffleButton;
                case PowerUpType.Bomb:
                    return bombButton;
                case PowerUpType.XRay:
                    return xrayButton;
                default:
                    return null;
            }
        }

        // private TMP_Dropdown EnsurePieceMaterialDropdown()
        // {
        //     if (pieceMaterialDropdown != null)
        //     {
        //         return pieceMaterialDropdown;
        //     }

        //     RectTransform root = transform as RectTransform;
        //     if (root == null)
        //     {
        //         return null;
        //     }

        //     GameObject dropdownObject = TMP_DefaultControls.CreateDropdown(BuildDropdownResources());
        //     dropdownObject.name = "PieceMaterialDropdown";

        //     RectTransform dropdownRect = dropdownObject.GetComponent<RectTransform>();
        //     dropdownRect.SetParent(root, false);
        //     dropdownRect.anchorMin = new Vector2(1f, 1f);
        //     dropdownRect.anchorMax = new Vector2(1f, 1f);
        //     dropdownRect.pivot = new Vector2(1f, 1f);
        //     dropdownRect.sizeDelta = new Vector2(280f, 36f);
        //     dropdownRect.anchoredPosition = new Vector2(-24f, -120f);

        //     pieceMaterialDropdown = dropdownObject.GetComponent<TMP_Dropdown>();
        //     if (pieceMaterialDropdown?.captionText != null)
        //     {
        //         pieceMaterialDropdown.captionText.text = "Piece Material";
        //         pieceMaterialDropdown.captionText.fontSize = 20f;
        //     }

        //     if (pieceMaterialDropdown?.itemText != null)
        //     {
        //         pieceMaterialDropdown.itemText.fontSize = 18f;
        //     }

        //     dropdownObject.SetActive(false);
        //     return pieceMaterialDropdown;
        // }

        private TMP_Text EnsureComboText()
        {
            if (comboText != null)
            {
                return comboText;
            }

            RectTransform root = transform as RectTransform;
            if (root == null)
            {
                return null;
            }

            GameObject textObject = new GameObject("ComboText", typeof(RectTransform));
            textObject.transform.SetParent(root, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(260f, 80f);
            textRect.anchoredPosition = new Vector2(0f, 180f);

            comboText = textObject.AddComponent<TextMeshProUGUI>();
            comboText.text = "COMBO x1";
            comboText.fontSize = 54;
            comboText.fontStyle = FontStyles.Bold;
            comboText.alignment = TextAlignmentOptions.Center;
            comboText.raycastTarget = false;
            comboText.color = new Color(1f, 0.7f, 0.15f, 0f);
            comboText.enableAutoSizing = false;
            comboText.outlineColor = Color.black;
            comboText.outlineWidth = 0.35f;
            comboText.gameObject.SetActive(false);
            return comboText;
        }

        private static TMP_DefaultControls.Resources BuildDropdownResources()
        {
            return new TMP_DefaultControls.Resources
            {
                standard = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd"),
                background = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd"),
                inputField = Resources.GetBuiltinResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
                knob = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd"),
                checkmark = Resources.GetBuiltinResource<Sprite>("UI/Skin/Checkmark.psd"),
                dropdown = Resources.GetBuiltinResource<Sprite>("UI/Skin/DropdownArrow.psd"),
                mask = Resources.GetBuiltinResource<Sprite>("UI/Skin/UIMask.psd"),
            };
        }

        [Serializable]
        private struct BoosterHudBinding
        {
            [SerializeField] private Image iconImage;
            [SerializeField] private TMP_Text quantityText;
            [SerializeField] private Sprite availableSprite;
            [SerializeField] private Sprite emptySprite;

            public void SetCount(int count)
            {
                int safeCount = Mathf.Max(0, count);
                bool hasBooster = safeCount > 0;

                if (quantityText != null)
                {
                    quantityText.text = count == int.MaxValue ? "∞" : safeCount.ToString();
                }

                if (iconImage != null)
                {
                    Sprite targetSprite = hasBooster ? availableSprite : emptySprite;
                    if (targetSprite != null)
                    {
                        iconImage.sprite = targetSprite;
                    }
                }
            }
        }
    }
}
