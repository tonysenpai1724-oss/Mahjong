using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TouchController : MonoBehaviour
{
    public GraphicRaycaster graphicRaycaster;
    bool delayAction;

    private void Update()
    {
        if (GameplayManager.Instance.State != EGamePlayState.Running)
            return;
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            DebugCustom.Log($"MouseDown {mousePos}");
            if (!delayAction)
            {
                HandleTouch(mousePos);
            }
        }
        else if (Input.touchCount > 0)
        {
            Vector2 touchPos = Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position);
            DebugCustom.Log($"Touch Pos {touchPos}");
            if (!delayAction)
            {
                HandleTouch(touchPos);
            }
        }
        else
        {
            delayAction = false;
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
    void HandleTouch(Vector3 pos)
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        graphicRaycaster.Raycast(pointerData, results);
        if (results.Count > 0)
            return;
        pos.z = 0;
        DebugCustom.Log($"Touch Pos {pos}");
       
        GameplayManager.Instance.OnClick(pos);
    }
}
