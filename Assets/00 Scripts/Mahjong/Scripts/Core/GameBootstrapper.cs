using System.Collections.Generic;
using MahjongOut3D.Data;
using MahjongOut3D.Gameplay;
using MahjongOut3D.LevelSystem;
using MahjongOut3D.Managers;
using MahjongOut3D.Utilities;
using UnityEngine;

namespace MahjongOut3D.Core
{
    /// <summary>
    /// Builds the runtime context and initializes managers in a deterministic order.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameBootstrapper : MonoBehaviour
    {
        [Header("Runtime Settings")]
        [SerializeField] private MahjongProjectSettings projectSettings;
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Header("Manager References")]
        [SerializeField] private List<ManagerBehaviour> managers = new List<ManagerBehaviour>();

        private readonly List<ManagerBehaviour> runtimeManagers = new List<ManagerBehaviour>();
        private readonly List<VoxelLevelGenerator> levelGenerators = new List<VoxelLevelGenerator>();
        private GameContext context;
        private bool hasBootstrapped;

        /// <summary>
        /// Initializes the runtime context when the scene loads.
        /// </summary>
        private void Awake()
        {
            Bootstrap();
        }

        /// <summary>
        /// Shuts managers down in reverse order when the bootstrapper is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            ShutdownManagers();
        }

        /// <summary>
        /// Builds the runtime context and initializes the configured managers.
        /// </summary>
        public void Bootstrap()
        {
            if (hasBootstrapped)
            {
                return;
            }

            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            ApplyProjectSettings();
            CacheManagers();
            EnsureGameplayManager();

            GameManager gameManager = GameManager.Instance;
            gameManager?.SetState(GameFlowState.Bootstrapping);

            ServiceRegistry serviceRegistry = new ServiceRegistry();
            EventBus eventBus = new EventBus();
            context = new GameContext(projectSettings, serviceRegistry, eventBus);

            for (int index = 0; index < runtimeManagers.Count; index++)
            {
                ManagerBehaviour manager = runtimeManagers[index];
                if (manager == null)
                {
                    continue;
                }

                manager.Initialize(context);
                context.EventBus.Publish(new ManagerInitializedEvent(manager.GetType()));
                MahjongRuntimeLogger.LogVerbose($"Initialized manager: {manager.GetType().Name}");
            }

            MahjongOut3D.Managers.AudioManager persistentAudioManager = FindFirstObjectByType<MahjongOut3D.Managers.AudioManager>(FindObjectsInactive.Exclude);
            if (persistentAudioManager != null && !persistentAudioManager.IsInitialized)
            {
                persistentAudioManager.Initialize(context);
            }

            CacheLevelGenerators();
            for (int index = 0; index < levelGenerators.Count; index++)
            {
                VoxelLevelGenerator generator = levelGenerators[index];
                if (generator != null)
                {
                    generator.Initialize(context);
                }
            }

            if (gameManager != null && projectSettings != null)
            {
                gameManager.SetState(projectSettings.InitialGameState);
            }

            context.EventBus.Publish(new BootstrapCompletedEvent());
            hasBootstrapped = true;
        }

        private void EnsureGameplayManager()
        {
            if (GameplayManager.Instance != null)
            {
                return;
            }

            GameObject gameplayManagerObject = new GameObject("Gameplay Manager");
            gameplayManagerObject.transform.SetParent(transform, false);
            gameplayManagerObject.AddComponent<GameplayManager>();
        }

        /// <summary>
        /// Applies project-wide runtime settings before gameplay begins.
        /// </summary>
        private void ApplyProjectSettings()
        {
            if (projectSettings == null)
            {
                return;
            }

            Application.targetFrameRate = projectSettings.TargetFrameRate;
            QualitySettings.vSyncCount = projectSettings.VSyncCount;
            MahjongRuntimeLogger.Configure(projectSettings.EnableVerboseLogging);
        }

        /// <summary>
        /// Collects and sorts manager references used by the runtime.
        /// </summary>
        private void CacheManagers()
        {
            runtimeManagers.Clear();

            if (managers == null || managers.Count == 0)
            {
                managers = new List<ManagerBehaviour>(GetComponentsInChildren<ManagerBehaviour>(true));
            }

            for (int index = 0; index < managers.Count; index++)
            {
                ManagerBehaviour manager = managers[index];
                if (manager != null && !runtimeManagers.Contains(manager))
                {
                    runtimeManagers.Add(manager);
                }
            }

            runtimeManagers.Sort((left, right) => left.InitializationOrder.CompareTo(right.InitializationOrder));
        }

        /// <summary>
        /// Collects every level generator in the bootstrap hierarchy.
        /// </summary>
        private void CacheLevelGenerators()
        {
            levelGenerators.Clear();
            levelGenerators.AddRange(GetComponentsInChildren<VoxelLevelGenerator>(true));
        }

        /// <summary>
        /// Shuts managers down in reverse initialization order.
        /// </summary>
        private void ShutdownManagers()
        {
            if (!hasBootstrapped)
            {
                return;
            }

            for (int index = runtimeManagers.Count - 1; index >= 0; index--)
            {
                ManagerBehaviour manager = runtimeManagers[index];
                if (manager != null)
                {
                    manager.Shutdown();
                }
            }

            context?.EventBus.Clear();
            runtimeManagers.Clear();
            levelGenerators.Clear();
            context = null;
            hasBootstrapped = false;
        }
    }
}
