using System;
using Cinemachine;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using static Cinemachine.DocumentationSortingAttribute;
using System.Linq;

public class GameplayManager : Singleton<GameplayManager>
{
    public EGamePlayState State => state;
    [SerializeField, ReadOnly] protected EGamePlayState state;
    public EGamePlayState LastState { get; private set; }
    public bool winGame { get; private set; }
    public int CurrentLevel { get; set; }
    public int LevelTime { get; private set; }
    public int Score { get; private set; }
    public PackageResource PackReward { get; private set; }
    public IEnumerator IEInit()
    {
        DebugCustom.LogColor("Init Level");
        SetState(EGamePlayState.Cinematic);

        IAchievementController.Instance.UpdateAchievementProgress(EAchievementType.LevelPlay);

        LevelTime = 180;
        CurrentLevel = IPlayerInfoController.Instance.CurrentLevel();
        yield return new WaitUntil(() => ResolutionManager.Instance);
        yield return new WaitUntil(() => ResolutionManager.Instance.IsInitilized);
        yield return new WaitUntil(() => UIManager.Instance.uIGameplay);
        if (GameManager.Instance.GameType == EGameType.Endless)
            IAchievementController.Instance.UpdateAchievementProgress(EAchievementType.EndlessPlay);
        UIManager.Instance.uIGameplay.Initialize();
        StartGame();
        TigerForge.EventManager.StartListening(Constant.EVENT_TIMER_TICK, OnTick);
        TigerForge.EventManager.EmitEvent(Constant.EVENT_LEVEL_INITED);
    }
    public void StartGame()
    {
        SetState(EGamePlayState.Running);
    }
    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.X))
        {
            for (int i = 0; i < 10; i++)
            {
                OnTick();
            }
        }
#endif
    }
    void OnTick()
    {
        if (state == EGamePlayState.Running && LevelTime > 0)
        {
            if (LevelTime <= 0)
                EndGame(false);
        }

    }
    public void SetState(EGamePlayState _state)
    {
        state = _state;
        DebugCustom.LogColor("GamePlayState", State);
        if (state != EGamePlayState.Pause)
        {
            LastState = state;
            Time.timeScale = 1;
        }
        else
            Time.timeScale = 0;

        TigerForge.EventManager.EmitEvent(Constant.ON_GAME_STATE_CHANGE);
    }

    public void EndGame(bool win)
    {
        DebugCustom.LogColor("End Game");
        winGame = win;
        if (winGame)
        {
            IPlayerInfoController.Instance.WinLevel();
            IAchievementController.Instance.UpdateAchievementProgress(EAchievementType.LevelWin);
            if (GameManager.Instance.GameType == EGameType.Endless)
                IAchievementController.Instance.UpdateAchievementProgress(EAchievementType.WinEndlessStage);
            IAchievementController.Instance.UpdateAchievementProgress(EAchievementType.LevelWin);

            PackReward = new PackageResource();
            PackReward.AddResource(new CommonResource(ECommonResource.Coin, 15));
            PackReward.AddResource(new CommonResource(ECommonResource.Gem, 10));
            PackReward.AddResource(new CommonResource(ECommonResource.ActivePoint, 1));

            PackReward.ReceiveResource(EResourceFrom.ReviveIngame);
            UIManager.Instance.ShowPopupEndGame();
        }
        else
        {
            if (GameManager.Instance.GameType == EGameType.Endless)
            {
                PackReward = new PackageResource();
                PackReward.AddResource(new CommonResource(ECommonResource.Coin, Score));
                PackReward.AddResource(new CommonResource(ECommonResource.ActivePoint, Score / 10));
            }
            UIManager.Instance.ShowPopupEndGame();
        }
    }
    public void OnClick(Vector3 pos)
    {
        DebugCustom.LogColor("OnClick", pos);
    }
}