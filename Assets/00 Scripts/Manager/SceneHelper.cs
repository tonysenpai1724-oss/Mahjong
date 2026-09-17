using System.Collections;
using System.Collections.Generic;
using MahjongOut3D.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;
public class SceneHelper : Singleton<SceneHelper>
{
    protected override void OnAwake()
    {
        base.OnAwake();
        Application.targetFrameRate = 60;
    }
    public void ChangeSceneLoading()
    {
        StartCoroutine(IEChangeSceneLoading());
    }
    public IEnumerator IEChangeSceneLoading()
    {
        Debug.Log("ChangeScene Loading");
        if(UIManager.Instance != null)
            UIManager.Instance.Initialized = false;
        var async = SceneManager.LoadSceneAsync(Constant.SCENE_LOADING);
        while (!async.isDone)
        {
            //totalProgress = async.progress;
            yield return null;
        }
        Debug.Log("ChangeScene Loading Done");
    }
    public IEnumerator IEReturnHome()
    {
        yield return StartCoroutine(LoadingPanel.Instance.IEStartTransiton());
        if (SceneManager.GetActiveScene() != SceneManager.GetSceneByName(Constant.SCENE_LOADING))
            yield return StartCoroutine(IEChangeSceneLoading());
        yield return StartCoroutine(IEChangeSceneHome());
        yield return StartCoroutine(UIManager.Instance.IEHomeInit());
        yield return StartCoroutine(LoadingPanel.Instance.IEEndTransition());
        GameManager.Instance.SetState(EGameState.Home);
        UIManager.Instance.HomeInit();
    }
    IEnumerator IEChangeSceneHome()
    {
        Debug.Log("ChangeScene Home");
        var async = SceneManager.LoadSceneAsync(Constant.SCENE_MAIN_UI);
        while (!async.isDone)
        {
            //totalProgress = async.progress;
            yield return null;
        }
        Debug.Log("ChangeScene Home Done");
    }
    public IEnumerator IEGoGameplay()
    {
        yield return StartCoroutine(LoadingPanel.Instance.IEStartTransiton());
        yield return StartCoroutine(IEChangeSceneLoading());
        yield return StartCoroutine(IEChangeSceneGameplay());
        yield return StartCoroutine(IELoadMap());
        yield return StartCoroutine(UIManager.Instance.IEGamgeInit());
        yield return StartCoroutine(IELoadMahjongLevelAfterSceneReady());
        yield return StartCoroutine(LoadingPanel.Instance.IEEndTransition());
        GameManager.Instance.SetState(EGameState.Gameplay);
    }
    IEnumerator IEChangeSceneGameplay()
    {
        Debug.Log("ChangeScene Gameplay");
        var async = SceneManager.LoadSceneAsync(Constant.SCENE_GAME_PLAY);
        while (!async.isDone)
        {
            //totalProgress = async.progress;
            yield return null;
        }
         yield return StartCoroutine(LoadingPanel.Instance.IEEndTransition());
        Debug.Log("ChangeScene Gameplay Done");
    }

    public IEnumerator IELoadMap()
    {
        yield return new WaitUntil(() => GameplayManager.Instance);
        yield return StartCoroutine(GameplayManager.Instance.IEInit());
    }

    IEnumerator IELoadMahjongLevelAfterSceneReady()
    {
        if (GameManager.Instance.GameType != EGameType.Campaign)
            yield break;

        Debug.Log("[Mahjong] SceneHelper waiting to load campaign level.");

        int retryCount = 20;
        while (retryCount-- > 0)
        {
            LevelManager levelManager = FindObjectOfType<LevelManager>();
            if (levelManager != null)
            {
                Debug.Log($"[Mahjong] SceneHelper load attempt {20 - retryCount}/20 with level index {levelManager.CurrentLevelIndex}.");
                if (levelManager.LoadCurrentLevel())
                {
                    Debug.Log($"[Mahjong] SceneHelper loaded campaign level {levelManager.CurrentLevelIndex}.");
                    yield break;
                }
            }

            yield return null;
        }

        Debug.LogWarning("[Mahjong] SceneHelper failed to load campaign level after scene ready.");
    }
}
