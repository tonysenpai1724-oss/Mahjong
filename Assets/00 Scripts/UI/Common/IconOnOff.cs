using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IconOnOff : MonoBehaviour
{
    public GameObject activeIcon;
    public bool activeIsOn = true;
    public void SetOn(bool on)
    {
        if(on)
            activeIcon.SetActive(activeIsOn);
        else
            activeIcon.SetActive(!activeIsOn);
    }
}
