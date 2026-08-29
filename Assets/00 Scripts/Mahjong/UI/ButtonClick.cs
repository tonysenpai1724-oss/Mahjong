
using UnityEngine;
using UnityEngine.UI;
using MahjongOut3D.Managers;
using MahjongOut3D.Gameplay;
using MahjongOut3D.LevelSystem;
using MahjongOut3D.TileSystem;
using MahjongOut3D.Utilities;
public class ButtonClick : MonoBehaviour
{
    public Button button;
    void Start()
    {
        if (button == null)
            button = GetComponent<Button>();
        button.onClick.AddListener(PlayUISfx);
    }
    public void PlayUISfx()
    {  MahjongOut3D.Managers.AudioManager audioManager =
    FindFirstObjectByType<MahjongOut3D.Managers.AudioManager>();
        if(audioManager!=null)
        {
            audioManager.PlayUISfx();
        }
       
    }
}