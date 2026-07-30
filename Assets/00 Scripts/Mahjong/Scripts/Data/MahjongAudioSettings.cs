using UnityEngine;

namespace MahjongOut3D.Data
{
    /// <summary>
    /// Stores one-shot audio clips used by the Mahjong Out 3D runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Mahjong Out 3D/Data/Audio Settings", fileName = "MahjongAudioSettings")]
    public sealed class MahjongAudioSettings : ScriptableObject
    {
        [field: SerializeField] public AudioClip SelectClip { get; private set; }
        [field: SerializeField] public AudioClip MatchClip { get; private set; }
        [field: SerializeField] public AudioClip MismatchClip { get; private set; }
        [field: SerializeField] public AudioClip WinClip { get; private set; }
        [field: SerializeField] public AudioClip LoseClip { get; private set; }
        [field: SerializeField] public AudioClip HintClip { get; private set; }
        [field: SerializeField] public AudioClip UndoClip { get; private set; }
        [field: SerializeField] public AudioClip ShuffleClip { get; private set; }
        [field: SerializeField] public AudioClip BombClip { get; private set; }
        [field: SerializeField] public AudioClip XRayClip { get; private set; }
    }
}
