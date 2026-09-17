using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplashScene : MonoBehaviour
{
    public GameObject logoSpine;
    private void Start()
    {
        StartCoroutine(IESplash());
    }
    IEnumerator IESplash()
    {
        if(logoSpine != null)
            logoSpine.SetActive(true);
        yield return new WaitForSeconds(1);
        yield return new WaitUntil(() => SceneHelper.Instance);
        SceneHelper.Instance.ChangeSceneLoading();
    }
}
