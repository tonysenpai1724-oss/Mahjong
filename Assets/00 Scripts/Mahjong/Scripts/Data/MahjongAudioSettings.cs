using UnityEngine;

namespace MahjongOut3D.Data
{
    /// <summary>
    /// Stores one-shot audio clips used by the Mahjong Out 3D runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Mahjong Out 3D/Data/Audio Settings", fileName = "MahjongAudioSettings")]
    public sealed class MahjongAudioSettings : ScriptableObject
    {
        [field: Header("Mahjong SFX")]
        [field: SerializeField] public AudioClip MahjongTapClip { get; private set; }
        [field: SerializeField] public AudioClip FlipTileClip { get; private set; }
        [field: SerializeField] public AudioClip TileMatchClip { get; private set; }
        [field: SerializeField] public AudioClip TileSmashClip { get; private set; }

        [field: Header("Combo SFX")]
        [field: SerializeField] public AudioClip GoodClip { get; private set; }
        [field: SerializeField] public AudioClip GreatClip { get; private set; }
        [field: SerializeField] public AudioClip ExcellentClip { get; private set; }
        [field: SerializeField] public AudioClip WellDoneClip { get; private set; }
        [field: SerializeField] public AudioClip UnbelievableClip { get; private set; }
        [field: SerializeField] public AudioClip BrilliantClip { get; private set; }
        [field: SerializeField] public AudioClip LegendaryClip { get; private set; }

        [field: Header("Level SFX")]
        [field: SerializeField] public AudioClip LevelCompletedClip { get; private set; }
        [field: SerializeField] public AudioClip LevelFailedClip { get; private set; }

        [field: Header("Music")]
        [field: SerializeField] public AudioClip MenuMusicClip { get; private set; }
        [field: SerializeField] public AudioClip[] GameplayMusicClips { get; private set; }
        [field: SerializeField] public AudioClip NatureClip { get; private set; }

        [field: Header("Power-up SFX")]
        [field: SerializeField] public AudioClip HintClip { get; private set; }
        [field: SerializeField] public AudioClip UndoClip { get; private set; }
        [field: SerializeField] public AudioClip ShuffleClip { get; private set; }
        [field: SerializeField] public AudioClip BombClip { get; private set; }
        [field: SerializeField] public AudioClip XRayClip { get; private set; }

        [field: Header("Legacy aliases")]
        [field: SerializeField] public AudioClip SelectClip { get; private set; }
        [field: SerializeField] public AudioClip MatchClip { get; private set; }
        [field: SerializeField] public AudioClip MismatchClip { get; private set; }
        [field: SerializeField] public AudioClip WinClip { get; private set; }
        [field: SerializeField] public AudioClip LoseClip { get; private set; }
    }
}
