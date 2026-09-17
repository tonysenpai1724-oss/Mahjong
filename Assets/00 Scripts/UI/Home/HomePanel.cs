using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class HomePanel : MonoBehaviour
{
    public Canvas canvas;
    public virtual void SetActive(bool active)
    {
        canvas.enabled = active;
    }
    public abstract void InitFirstTime();
}
