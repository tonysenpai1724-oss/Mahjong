using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundButton : MonoBehaviour
{
    Button _button;
    private void OnEnable()
    {
        _button = GetComponent<Button>();
        if (_button != null)
            _button.onClick.AddListener(PlaySoundButton);
    }
    public void PlaySoundButton()
    {
        AudioManager.Instance.PlaySfx(ESfx.ButtonSfx);
    }

}
