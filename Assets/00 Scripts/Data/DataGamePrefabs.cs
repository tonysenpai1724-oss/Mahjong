using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
[CreateAssetMenu(fileName = "Data Prefabs", menuName = "Data/Data Prefabs")]
public class DataGamePrefabs : SerializedScriptableObject
{
    [FoldoutGroup("Home")]
    public UIFlyingObject uIFlyingObject;
    [FoldoutGroup("UI")]
    public UiResourceItem uiResourceItem;
}
