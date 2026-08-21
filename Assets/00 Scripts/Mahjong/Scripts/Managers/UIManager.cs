using MahjongOut3D.Core;
using MahjongOut3D.Gameplay;
using MahjongOut3D.LevelSystem;
using MahjongOut3D.UI;
using System.Collections.Generic;
using TigerForge;
using UnityEngine;

namespace MahjongOut3D.Managers
{
    /// <summary>
    /// Owns runtime screen visibility and wires UI actions to game systems.
    /// </summary>
    public sealed class UIManager : ManagerBehaviour
    {
        [SerializeField] private List<UIScreenView> screens = new List<UIScreenView>();
        [SerializeField] private MainMenuView mainMenuView;
        [SerializeField] private LevelSelectView levelSelectView;
        [SerializeField] private GameplayHudView gameplayHudView;
        [SerializeField] private TraySlotAnchorProvider traySlotAnchorProvider;
        [SerializeField] private PauseMenuView pauseMenuView;
        [SerializeField] private ResultScreenView resultScreenView;

        private readonly List<Texture2D> availablePieceTextures = new List<Texture2D>();
        private Texture2D selectedPiecePreviewTexture;

        /// <summary>
        /// Gets the currently active UI screen.
        /// </summary>
        public UIScreenType CurrentScreen { get; private set; } = UIScreenType.None;

        public GameplayHudView GameplayHudView => gameplayHudView;
        public TraySlotAnchorProvider TraySlotAnchorProvider => traySlotAnchorProvider;

        /// <summary>
        /// Gets the bootstrap order for the UI manager.
        /// </summary>
        public override int InitializationOrder => 50;

        /// <summary>
        /// Initializes screen references and subscribes to gameplay events.
        /// </summary>
        protected override void OnInitialize()
        {
            CacheScreenReferences();
            BindViews();

            Context.EventBus.Subscribe<GameplayProgressChangedEvent>(HandleProgressChanged);
            Context.EventBus.Subscribe<LevelGeneratedEvent>(HandleLevelGenerated);

            GameManager gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.GameFlowStateChanged += HandleGameFlowStateChanged;
            }

            EventManager.StartListening(Constant.ON_GAME_STATE_CHANGE, HandleGameplayStateChanged);
            EventManager.StartListening(Constant.ON_PLAYER_RESOURCE_UPDATE, HandlePlayerResourceChanged);
            RefreshScreenState();
            RefreshCoinDisplays();
            RefreshBoosterDisplays();
            UpdateLevelSelectInfo();
            RefreshPieceMaterialDropdown();
        }

        /// <summary>
        /// Clears UI event subscriptions during shutdown.
        /// </summary>
        protected override void OnShutdown()
        {
            Context.EventBus.Unsubscribe<GameplayProgressChangedEvent>(HandleProgressChanged);
            Context.EventBus.Unsubscribe<LevelGeneratedEvent>(HandleLevelGenerated);

            GameManager gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.GameFlowStateChanged -= HandleGameFlowStateChanged;
            }

            EventManager.StopListening(Constant.ON_GAME_STATE_CHANGE, HandleGameplayStateChanged);
            EventManager.StopListening(Constant.ON_PLAYER_RESOURCE_UPDATE, HandlePlayerResourceChanged);
        }

        /// <summary>
        /// Displays the specified screen type.
        /// </summary>
        /// <param name="screenType">Screen to display.</param>
        public void ShowScreen(UIScreenType screenType)
        {
            CurrentScreen = screenType;

            for (int index = 0; index < screens.Count; index++)
            {
                UIScreenView screen = screens[index];
                if (screen != null)
                {
                    bool isVisible = screen.ScreenType == screenType;
                    if (screen == resultScreenView)
                    {
                        isVisible = screenType == UIScreenType.Result;
                    }

                    screen.SetVisible(isVisible);
                }
            }
        }

        /// <summary>
        /// Caches missing screen references from the current hierarchy.
        /// </summary>
        private void CacheScreenReferences()
        {
            if (screens == null || screens.Count == 0)
            {
                screens = new List<UIScreenView>(GetComponentsInChildren<UIScreenView>(true));
            }

            if (mainMenuView == null)
            {
                mainMenuView = GetComponentInChildren<MainMenuView>(true);
            }

            if (levelSelectView == null)
            {
                levelSelectView = GetComponentInChildren<LevelSelectView>(true);
            }

            if (gameplayHudView == null)
            {
                gameplayHudView = GetComponentInChildren<GameplayHudView>(true);
            }

            if (traySlotAnchorProvider == null)
            {
                traySlotAnchorProvider = GetComponentInChildren<TraySlotAnchorProvider>(true);
            }

            if (traySlotAnchorProvider == null)
            {
                traySlotAnchorProvider = FindFirstObjectByType<TraySlotAnchorProvider>(FindObjectsInactive.Include);
            }

            if (pauseMenuView == null)
            {
                pauseMenuView = GetComponentInChildren<PauseMenuView>(true);
            }

            if (resultScreenView == null)
            {
                resultScreenView = GetComponentInChildren<ResultScreenView>(true);
            }
        }

        /// <summary>
        /// Binds UI callbacks to the active managers.
        /// </summary>
        private void BindViews()
        {
            if (mainMenuView != null)
            {
                mainMenuView.Bind(HandlePlayPressed, HandleLevelSelectPressed);
            }

            if (levelSelectView != null)
            {
                levelSelectView.Bind(HandlePreviousLevelPressed, HandleNextLevelPressed, HandleLoadSelectedLevelPressed);
            }

            if (gameplayHudView != null)
            {
                gameplayHudView.Bind(
                    HandlePausePressed,
                    () => Context.Services.Get<MatchManager>().UseHint(),
                    () => Context.Services.Get<MatchManager>().UseUndo(),
                    () => Context.Services.Get<MatchManager>().UseShuffle(),
                    () => Context.Services.Get<MatchManager>().UseBomb(),
                    () => Context.Services.Get<MatchManager>().UseXRay(),
                    HandlePieceMaterialDropdownChanged);
            }

            if (pauseMenuView != null)
            {
                pauseMenuView.Bind(HandleResumePressed, HandleRetryPressed, HandleHomePressed);
            }

            if (resultScreenView != null)
            {
                resultScreenView.Bind(HandleResultPrimaryPressed, HandleHomePressed);
            }
        }

        /// <summary>
        /// Applies screen visibility for the current game flow state.
        /// </summary>
        private void ApplyGameFlowState(GameFlowState state)
        {
            switch (state)
            {
                case GameFlowState.MainMenu:
                    ShowScreen(UIScreenType.MainMenu);
                    break;
                case GameFlowState.LevelSelect:
                    UpdateLevelSelectInfo();
                    ShowScreen(UIScreenType.LevelSelect);
                    break;
                case GameFlowState.Gameplay:
                    UpdateGameplayHudInfo();
                    ShowScreen(UIScreenType.GameplayHud);
                    break;
                case GameFlowState.Paused:
                    ShowScreen(UIScreenType.Pause);
                    break;
            }
        }

        private void RefreshScreenState()
        {
            if (GameplayManager.Instance != null && GameplayManager.Instance.State == EGamePlayState.GameOver)
            {
                if (resultScreenView != null)
                {
                    resultScreenView.SetResult(GameplayManager.Instance.winGame);
                }

                ShowScreen(UIScreenType.Result);
                return;
            }

            GameManager gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                ApplyGameFlowState(gameManager.CurrentFlowState);
            }
        }

        /// <summary>
        /// Updates coin labels when save data changes.
        /// </summary>
        private void UpdateCoinDisplays(int coins)
        {
            gameplayHudView?.SetCoins(coins);
        }

        private void RefreshCoinDisplays()
        {
            long coins = IPlayerResource.Instance != null
                ? IPlayerResource.Instance.GetCommonResource(ECommonResource.Coin)
                : 0;

            long clampedCoins = coins < 0L ? 0L : coins;
            UpdateCoinDisplays(clampedCoins > int.MaxValue ? int.MaxValue : (int)clampedCoins);
        }

        private void RefreshBoosterDisplays()
        {
            if (gameplayHudView == null || !Context.Services.TryGet(out MatchManager matchManager))
            {
                return;
            }

            gameplayHudView.SetBoosterCounts(
                matchManager.GetPowerUpAvailableCount(PowerUpType.Hint),
                matchManager.GetPowerUpAvailableCount(PowerUpType.Undo),
                matchManager.GetPowerUpAvailableCount(PowerUpType.Shuffle),
                matchManager.GetPowerUpAvailableCount(PowerUpType.Bomb),
                matchManager.GetPowerUpAvailableCount(PowerUpType.XRay));
        }

        private void UpdateGameplayHudInfo()
        {
            if (gameplayHudView == null || !Context.Services.TryGet(out LevelManager levelManager))
            {
                return;
            }

            gameplayHudView.SetLevel(levelManager.CurrentLevelIndex);
            RefreshBoosterDisplays();
            RefreshPieceMaterialDropdown();
        }

        /// <summary>
        /// Updates level-select labels from the current level and save data.
        /// </summary>
        private void UpdateLevelSelectInfo()
        {
            if (levelSelectView == null || !Context.Services.TryGet(out LevelManager levelManager))
            {
                return;
            }

            levelSelectView.SetLevelInfo(levelManager.CurrentLevelIndex, GetHighestUnlockedLevel());
        }

        private int GetHighestUnlockedLevel()
        {
            return Mathf.Max(1, IPlayerInfoController.Instance.CurrentLevel());
        }

        private void HandleGameFlowStateChanged(GameFlowStateChangedEvent eventData) => RefreshScreenState();

        private void HandleGameplayStateChanged() => RefreshScreenState();

        private void HandlePlayerResourceChanged()
        {
            RefreshCoinDisplays();
            RefreshBoosterDisplays();
        }

        private void HandleProgressChanged(GameplayProgressChangedEvent eventData)
        {
            gameplayHudView?.SetProgress(eventData.RemainingTiles, eventData.TotalTiles, eventData.CompletionRatio);
            UpdateGameplayHudInfo();
            RefreshCoinDisplays();
            RefreshBoosterDisplays();
        }

        private void HandleLevelGenerated(LevelGeneratedEvent eventData)
        {
            UpdateGameplayHudInfo();
            RefreshPieceMaterialDropdown();
        }

        private void RefreshPieceMaterialDropdown()
        {
            if (gameplayHudView == null || !Context.Services.TryGet(out VoxelLevelGenerator levelGenerator))
            {
                return;
            }

            availablePieceTextures.Clear();
            IReadOnlyList<Texture2D> runtimeTextures = levelGenerator.GetAvailablePieceTextures();
            for (int index = 0; index < runtimeTextures.Count; index++)
            {
                Texture2D texture = runtimeTextures[index];
                if (texture != null)
                {
                    availablePieceTextures.Add(texture);
                }
            }

            if (selectedPiecePreviewTexture != null)
            {
                Texture2D matchingTexture = FindMatchingPieceTexture(selectedPiecePreviewTexture);
                if (matchingTexture != null)
                {
                    selectedPiecePreviewTexture = matchingTexture;
                    if (levelGenerator.CurrentPieceTexture != matchingTexture)
                    {
                        levelGenerator.ApplyPieceTexture(matchingTexture);
                    }
                }
                else
                {
                    selectedPiecePreviewTexture = null;
                }
            }

            List<string> optionLabels = new List<string>(availablePieceTextures.Count);
            int selectedIndex = 0;
            Texture2D currentTexture = selectedPiecePreviewTexture != null ? selectedPiecePreviewTexture : levelGenerator.CurrentPieceTexture;
            for (int index = 0; index < availablePieceTextures.Count; index++)
            {
                Texture2D texture = availablePieceTextures[index];
                optionLabels.Add($"Piece: {texture.name}");
                if (texture == currentTexture)
                {
                    selectedIndex = index;
                }
            }

            gameplayHudView.SetPieceMaterialOptions(optionLabels, selectedIndex);
        }

        private void HandlePieceMaterialDropdownChanged(int selectedIndex)
        {
            if (!Context.Services.TryGet(out VoxelLevelGenerator levelGenerator))
            {
                return;
            }

            if (selectedIndex < 0 || selectedIndex >= availablePieceTextures.Count)
            {
                return;
            }

            selectedPiecePreviewTexture = availablePieceTextures[selectedIndex];
            levelGenerator.ApplyPieceTexture(selectedPiecePreviewTexture);
            RefreshPieceMaterialDropdown();
        }

        private Texture2D FindMatchingPieceTexture(Texture2D sourceTexture)
        {
            if (sourceTexture == null)
            {
                return null;
            }

            for (int index = 0; index < availablePieceTextures.Count; index++)
            {
                Texture2D candidate = availablePieceTextures[index];
                if (candidate == sourceTexture)
                {
                    return candidate;
                }
            }

            return null;
        }

        private void HandlePlayPressed()
        {
            if (!Context.Services.Get<LevelManager>().LoadCurrentLevel())
            {
                GameManager.Instance.SetState(GameFlowState.LevelSelect);
            }
        }

        private void HandleLevelSelectPressed()
        {
            GameManager.Instance.SetState(GameFlowState.LevelSelect);
        }

        private void HandlePreviousLevelPressed()
        {
            LevelManager levelManager = Context.Services.Get<LevelManager>();
            int previousIndex = Mathf.Max(0, levelManager.CurrentLevelIndex - 1);
            levelManager.SetCurrentLevel(previousIndex);
            UpdateLevelSelectInfo();
        }

        private void HandleNextLevelPressed()
        {
            LevelManager levelManager = Context.Services.Get<LevelManager>();
            int maxAllowedIndex = Mathf.Max(0, GetHighestUnlockedLevel() - 1);
            int nextIndex = Mathf.Min(maxAllowedIndex, levelManager.CurrentLevelIndex + 1);
            levelManager.SetCurrentLevel(nextIndex);
            UpdateLevelSelectInfo();
        }

        private void HandleLoadSelectedLevelPressed()
        {
            Context.Services.Get<LevelManager>().LoadCurrentLevel();
        }

        private void HandlePausePressed()
        {
            GameManager.Instance.PauseGameplay();
        }

        private void HandleResumePressed()
        {
            GameManager.Instance.ResumeGameplay();
        }

        private void HandleRetryPressed()
        {
            Context.Services.Get<LevelManager>().ReloadCurrentLevel();
        }

        private void HandleHomePressed()
        {
            GameManager.Instance.SetState(GameFlowState.MainMenu);
        }

        private void HandleResultPrimaryPressed()
        {
            LevelManager levelManager = Context.Services.Get<LevelManager>();

            if (GameplayManager.Instance != null && GameplayManager.Instance.winGame && levelManager.LoadNextLevel())
            {
                return;
            }

            levelManager.ReloadCurrentLevel();
        }
    }
}
