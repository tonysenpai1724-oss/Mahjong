using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SafeArea : MonoBehaviour
{
    public Canvas canvas;
    void Start()
    {
        UIManager.Instance.uISafeZone = this;    
    }

}
