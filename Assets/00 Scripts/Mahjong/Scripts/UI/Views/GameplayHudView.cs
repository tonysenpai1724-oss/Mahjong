using System;
using System.Collections.Generic;
using TMPro;
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
        [SerializeField] private TMP_Dropdown pieceMaterialDropdown;

        private Action<int> pieceMaterialChangedCallback;
        private bool suppressPieceMaterialDropdownCallback;

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
            BindPieceMaterialDropdown();
        }

        /// <summary>
        /// Rebuilds the runtime piece-material dropdown options.
        /// </summary>
        public void SetPieceMaterialOptions(IReadOnlyList<string> optionLabels, int selectedIndex)
        {
            TMP_Dropdown dropdown = EnsurePieceMaterialDropdown();
            if (dropdown == null)
            {
                return;
            }

            bool hasOptions = optionLabels != null && optionLabels.Count > 0;
            dropdown.gameObject.SetActive(hasOptions);
            if (!hasOptions)
            {
                return;
            }

            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>(optionLabels.Count);
            for (int index = 0; index < optionLabels.Count; index++)
            {
                options.Add(new TMP_Dropdown.OptionData(optionLabels[index]));
            }

            suppressPieceMaterialDropdownCallback = true;
            dropdown.ClearOptions();
            dropdown.AddOptions(options);
            dropdown.SetValueWithoutNotify(Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, options.Count - 1)));
            dropdown.RefreshShownValue();
            suppressPieceMaterialDropdownCallback = false;
        }

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

        private void BindPieceMaterialDropdown()
        {
            TMP_Dropdown dropdown = EnsurePieceMaterialDropdown();
            if (dropdown == null)
            {
                return;
            }

            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener(HandlePieceMaterialDropdownValueChanged);
        }

        private void HandlePieceMaterialDropdownValueChanged(int selectedIndex)
        {
            if (suppressPieceMaterialDropdownCallback)
            {
                return;
            }

            pieceMaterialChangedCallback?.Invoke(selectedIndex);
        }

        private TMP_Dropdown EnsurePieceMaterialDropdown()
        {
            if (pieceMaterialDropdown != null)
            {
                return pieceMaterialDropdown;
            }

            RectTransform root = transform as RectTransform;
            if (root == null)
            {
                return null;
            }

            GameObject dropdownObject = TMP_DefaultControls.CreateDropdown(BuildDropdownResources());
            dropdownObject.name = "PieceMaterialDropdown";

            RectTransform dropdownRect = dropdownObject.GetComponent<RectTransform>();
            dropdownRect.SetParent(root, false);
            dropdownRect.anchorMin = new Vector2(1f, 1f);
            dropdownRect.anchorMax = new Vector2(1f, 1f);
            dropdownRect.pivot = new Vector2(1f, 1f);
            dropdownRect.sizeDelta = new Vector2(280f, 36f);
            dropdownRect.anchoredPosition = new Vector2(-24f, -120f);

            pieceMaterialDropdown = dropdownObject.GetComponent<TMP_Dropdown>();
            if (pieceMaterialDropdown?.captionText != null)
            {
                pieceMaterialDropdown.captionText.text = "Piece Material";
                pieceMaterialDropdown.captionText.fontSize = 20f;
            }

            if (pieceMaterialDropdown?.itemText != null)
            {
                pieceMaterialDropdown.itemText.fontSize = 18f;
            }

            dropdownObject.SetActive(false);
            return pieceMaterialDropdown;
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
    }
}
