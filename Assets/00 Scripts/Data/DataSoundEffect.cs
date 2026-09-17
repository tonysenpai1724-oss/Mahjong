using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
[CreateAssetMenu(fileName = "Data Sound Effect", menuName = "Data/Data Sound Effect")]
public class DataSoundEffect : SerializedScriptableObject
{
    public Dictionary<ESfx, AudioClip> dicSfx;
}
