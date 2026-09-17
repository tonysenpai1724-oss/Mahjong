using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableDebuger : MonoBehaviour
{
    private void OnDisable()
    {
        Debug.Log($"{name} OnDisable");
    }
}
