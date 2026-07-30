using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;
[CreateAssetMenu(fileName = "BuildVersion", menuName = "Build/Version")]
public class BuildVersion : ScriptableObject
{
    public int version = 1;
    public string userId = "";
    [EnumToggleButtons]
    public EBuildType buildType;
    [EnumToggleButtons]
    public EPlatform platform;

    public void CorrectPlatform()
    {
#if UNITY_ANDROID
        platform = EPlatform.Android;
#endif
    }

}