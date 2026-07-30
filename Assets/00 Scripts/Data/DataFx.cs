using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "Data Sample", menuName = "Data/Data Fx")]
public class DataFx : SerializedScriptableObject
{
    public Dictionary<EAnimationEffectType, ParticleSystem> dicEffect;
}