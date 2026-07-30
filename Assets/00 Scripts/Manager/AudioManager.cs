using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Sirenix.OdinInspector;
using Random = UnityEngine.Random;

public class AudioManager : Singleton<AudioManager>
{
    public AudioSource musicSource;
    public SfxAudioSource sfxSourcePrefabs;

    public AudioClip homeMusic;
    public List<AudioClip> lstBattleMusic;
    CompositeDisposable musicDispose;
    List<int> lstPlayingSource;
    bool initilized;
    public List<GameObject> laserActiving;
    public void GameInit()
    {
        lstPlayingSource = new List<int>();
        laserActiving = new List<GameObject>();
        TigerForge.EventManager.StartListening(Constant.EVENT_ON_GAME_SETTING_CHANGE, OnGameSettingChange);
        musicSource.volume = IGameSettingController.Instance.GetSetting(EGameSetting.Music) ? 1 : 0;
        initilized = true;

    }

    public void OnGameSettingChange()
    {
        musicSource.volume = IGameSettingController.Instance.GetSetting(EGameSetting.Music) ? 1 : 0;
    }

    private void Update()
    {
        if (!initilized)
            return;
        lstPlayingSource.Clear();
    }

    public void PlayMusicHome()
    {
        musicSource.Stop();
        if (homeMusic != null)
            PlayMusic(homeMusic);
    }

    public void PlayMusicBattle()
    {
        laserActiving.Clear();
        musicSource.Stop();
        if (lstBattleMusic != null)
            if (lstBattleMusic.Count > 0)
                PlayMusic(lstBattleMusic.GetRandom());
    }

    public void PlayMusic()
    {
        if (GameManager.Instance.GameState == EGameState.Home)
            PlayMusicHome();
        else if (GameManager.Instance.GameState == EGameState.Gameplay)
            PlayMusicBattle();
    }

    public void PlayMusic(AudioClip music, float volume = 1f)
    {
        musicSource.clip = music;
        musicSource.volume = volume;
        musicSource.Play();
        float lenght = music.length;
        if (musicDispose != null)
            musicDispose.Dispose();
        Observable.Timer(TimeSpan.FromSeconds(lenght)).Subscribe(_ => { PlayMusic(); });
    }

    public SfxAudioSource PlaySfx(AudioClip sfx, float volume = 1f, float timePlay = 0)
    {
        if (GameManager.Instance.GameState != EGameState.Home && GameManager.Instance.GameState != EGameState.Gameplay)
            return null;
        if (!IGameSettingController.Instance.GetSetting(EGameSetting.Sound) || lstPlayingSource.Contains(sfx.GetEntityId().GetHashCode()))
            return null;
        //DebugCustom.Log("Play Sound", sfx);
        lstPlayingSource.Add(sfx.GetEntityId().GetHashCode());
        SfxAudioSource source = ObjectPooler.Spawn(sfxSourcePrefabs, transform.position);
        source.PlaySfx(sfx, volume, timePlay);
        return source;
    }

    public SfxAudioSource PlaySfx(List<AudioClip> sfx, float volume = 1f, float timePlay = 0)
    {
        if (GameManager.Instance.GameState != EGameState.Home && GameManager.Instance.GameState != EGameState.Gameplay)
            return null;
        int random = Random.Range(0, sfx.Count);
        if (!IGameSettingController.Instance.GetSetting(EGameSetting.Sound) || lstPlayingSource.Contains(sfx[random].GetEntityId().GetHashCode()))
            return null;
        DebugCustom.Log("Play Sound", sfx[random]);
        lstPlayingSource.Add(sfx[random].GetEntityId().GetHashCode());
        SfxAudioSource source = ObjectPooler.Spawn(sfxSourcePrefabs, transform.position);
        source.PlaySfx(sfx[random], volume, timePlay);
        return source;
    }

    public SfxAudioSource PlaySfx(AudioReference audioRef, float timePlay = 0)
    {
        if (GameManager.Instance.GameState != EGameState.Home && GameManager.Instance.GameState != EGameState.Gameplay)
            return null;
        if (!IGameSettingController.Instance.GetSetting(EGameSetting.Sound) ||
            lstPlayingSource.Contains(audioRef.audioClip.GetEntityId().GetHashCode()))
            return null;
        DebugCustom.Log("Play Sound", audioRef.audioClip.name);
        lstPlayingSource.Add(audioRef.audioClip.GetEntityId().GetHashCode());
        SfxAudioSource source = ObjectPooler.Spawn(sfxSourcePrefabs, transform.position);
        source.PlaySfx(audioRef.audioClip, audioRef.volume, timePlay);
        return source;
    }

    public SfxAudioSource PlaySfx(List<AudioReference> audioRef, float timePlay = 0)
    {
        if (GameManager.Instance.GameState != EGameState.Home && GameManager.Instance.GameState != EGameState.Gameplay)
            return null;
        int random = Random.Range(0, audioRef.Count);
        if (!IGameSettingController.Instance.GetSetting(EGameSetting.Sound) ||
            lstPlayingSource.Contains(audioRef[random].audioClip.GetEntityId().GetHashCode()))
            return null;
        DebugCustom.Log("Play Sound", audioRef[random].audioClip.name);
        lstPlayingSource.Add(audioRef[random].audioClip.GetEntityId().GetHashCode());
        SfxAudioSource source = ObjectPooler.Spawn(sfxSourcePrefabs, transform.position);
        source.PlaySfx(audioRef[random].audioClip, audioRef[random].volume, timePlay);
        return source;
    }

    public SfxAudioSource PlaySfx(ESfx sfx)
    {
        return PlaySfx(DataSystem.Instance.dataSoundEffect.dicSfx[sfx]);
    }
    public void AddActiveLaser(GameObject laser, bool active)
    {
        if (active)
            if (!laserActiving.Contains(laser))
                laserActiving.Add(laser);
        if (!active)
            if (laserActiving.Contains(laser))
                laserActiving.Remove(laser);
    }
}

[Serializable]
public class AudioReference
{
    public AudioClip audioClip;
    public float volume = 1;
}
