using UnityEngine;
using UniRx;

public class SfxAudioSource : PoolingObject
{
    public AudioSource audioSource;
    CompositeDisposable _disposables;

    public void PlaySfx(AudioClip clip, float volume = 1f, float timePlay = 0)
    {
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.Play();
        float lenght = clip.length;
        if (timePlay != 0)
        {
            lenght = timePlay;
        }

        if (_disposables != null)
            _disposables.Dispose();
        _disposables = new CompositeDisposable();
        Observable.Timer(System.TimeSpan.FromSeconds(lenght), Scheduler.MainThreadIgnoreTimeScale).Subscribe(_ => { Despawn(); }).AddTo(_disposables);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (_disposables != null)
            _disposables.Dispose();
    }
}