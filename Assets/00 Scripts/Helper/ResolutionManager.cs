using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Cinemachine;
using DG.Tweening;
using UnityEngine.UIElements;

public class ResolutionManager : MonoBehaviour
{
    #region Field
    public CinemachineVirtualCamera cinemachine;
    public Camera mainCam;
    [SerializeField]
    private Transform canvasScreenSize;
    [SerializeField]
    private float perUnitSize = 100;
    [SerializeField]
    private Vector2 renderCanvasSize, tempRenderCanvasSize;
    [SerializeField, ReadOnly]
    private float screenWidth = 0f;
    [SerializeField, ReadOnly]
    private float screenHeight = 0f;
    [SerializeField, ReadOnly]
    private float canvasWidth = 0f;
    [SerializeField, ReadOnly]
    private float canvasHeight = 0f;
    private float zoomTime = 3f;
    private static ResolutionManager instance;
    #endregion
    #region Property
    public static ResolutionManager Instance { get { return instance; } }

    [SerializeField]
    private float screenLeftEdge = 0f;
    [SerializeField]
    private float screenRightEdge = 0f;
    [SerializeField]
    private float screenTopEdge = 0f;
    [SerializeField]
    private float screenBottomEdge = 0f;

    public float ScreenLeftEdge => screenLeftEdge;
    public float ScreenRightEdge => screenRightEdge;
    public float ScreenBottomEdge => screenBottomEdge;
    public float ScreenTopEdge => screenTopEdge;
    private Vector3 currentPosition, previousePosition;
    //Vector2 lastScreenSize = new Vector2(Screen.width, Screen.height);
    public float DefaultCamSize => defaultCameraSize;
    private float defaultCameraSize = 0f;


    public float WorldWidth => worldWidth;
    [SerializeField, ReadOnly]
    float worldWidth;



    public float WorldHigh => worldHigh;
    [SerializeField, ReadOnly]
    float worldHigh;

    private GameObject currentRainObj;

    public Transform _top, _bottom, _left, _right, _center;
    public Vector2 Left => left;
    public Vector2 Right => right;
    public Vector2 Top => top;
    public Vector2 Bottom => bottom;
    public Vector2 Center => center;
    [SerializeField, ReadOnly]
    Vector2 left, right, top, bottom, center;
    public bool IsInitilized { get; private set; }
    Tween zoomTween, offsetTween;
    public bool Zooming { get; private set; }
    public System.Action<float> _OnChangeScreenSize;
    float maxOrthographicSize;
    private void GetEdge()
    {
        left = mainCam.ScreenToWorldPoint(_left.position);
        right = mainCam.ScreenToWorldPoint(_right.position);
        top = mainCam.ScreenToWorldPoint(_top.position);
        bottom = mainCam.ScreenToWorldPoint(_bottom.position);
        center = mainCam.ScreenToWorldPoint(_center.position);
        //DebugCustom.Log(center);
        screenLeftEdge = left.x;
        screenBottomEdge = left.y;

        screenRightEdge = bottom.x;
        screenTopEdge = bottom.y;

    }

    private void Start()
    {
        defaultCameraSize = mainCam.orthographicSize;
        currentPosition = previousePosition = transform.position;
        tempRenderCanvasSize = renderCanvasSize;
        RefreshResolution();
        IsInitilized = true;
    }
    public void SetColliderCam(PolygonCollider2D colliderCam)
    {
        CinemachineConfiner2D confinder = cinemachine.GetComponent<CinemachineConfiner2D>();
        confinder.enabled = false;
        float width = colliderCam.points[1].x - colliderCam.points[0].x;
        float height = colliderCam.points[2].y - colliderCam.points[0].y;
        float aspectRatio = (float)Screen.width / (float)Screen.height;
        maxOrthographicSize = Mathf.Min(width / (2 * aspectRatio), height / 2) * 0.95f;
        confinder.m_MaxWindowSize = maxOrthographicSize;
        confinder.m_BoundingShape2D = colliderCam;
        confinder.enabled = true;
        confinder.InvalidateCache();
    }
    private void OnChangeScreenSize()
    {
        cinemachine.GetComponent<CinemachineConfiner2D>().InvalidateCache();
        renderCanvasSize = tempRenderCanvasSize;
        Canvas.ForceUpdateCanvases();
        RefreshResolution();
        _OnChangeScreenSize?.Invoke(mainCam.orthographicSize);
    }
    public void RefreshResolution()
    {
        float aspect = (float)Screen.width / Screen.height;

        worldHigh = mainCam.orthographicSize * 2;

        worldWidth = worldHigh * aspect;
        if (instance == null)
            instance = this;
        screenWidth = Screen.width;
        screenHeight = Screen.height;
        renderCanvasSize = new Vector2(renderCanvasSize.x / perUnitSize, renderCanvasSize.y / perUnitSize);
        canvasWidth = renderCanvasSize.x;
        canvasHeight = renderCanvasSize.y;
        GetEdge();
    }

    private void LateUpdate()
    {
        currentPosition = transform.position;
        if (currentPosition != previousePosition)
        {
            previousePosition = currentPosition;
            GetEdge();
        }
    }
    public void ZoomVirtualCamByScale(float scale, float timeAnim = -1)
    {
        ZoomVirtualCam(defaultCameraSize * scale, timeAnim);
    }
    public void ZoomVirtualCam(float value, float timeAnim = -1)
    {
        value = Mathf.Clamp(value, 1, maxOrthographicSize);
        timeAnim = timeAnim < 0 ? zoomTime : timeAnim;
        if (zoomTween != null)
            zoomTween.Kill();
        Zooming = true;
        zoomTween = DOTween.To(() => cinemachine.m_Lens.OrthographicSize, x => cinemachine.m_Lens.OrthographicSize = x, value, timeAnim)
            .SetEase(Ease.Linear).OnUpdate(() =>
        {
            OnChangeScreenSize();
        }).OnComplete(() =>
        {
            cinemachine.m_Lens.OrthographicSize = value;
            OnChangeScreenSize();
            Zooming = false;
        });
    }
    public void ResetZoom(float timeAnim = -1)
    {
        ZoomVirtualCamByScale(1, timeAnim);
    }
    bool PointIsVisibleToCamera(Vector2 point, Camera cam)
    {
        if (cam.WorldToViewportPoint(point).x < 0 || cam.WorldToViewportPoint(point).x > 1 || cam.WorldToViewportPoint(point).y > 1 || cam.WorldToViewportPoint(point).y < 0)
            return false;

        return true;
    }

    public bool IsPointInScreen(Vector2 point, float offsetX = 0, float offsetY = 0)
    {
        if (point.x > screenRightEdge + offsetX || point.x < screenLeftEdge - offsetX || point.y > screenTopEdge + offsetY || point.y < screenBottomEdge - offsetY)
            return false;

        return true;
    }

    public Vector2 ScreenCentre
    {
        // returns the x-coordinate that is the centre of the screen on the x axis regardless of where the camera is
        get
        {
            Vector2 zeroZero = new Vector2(0.5f, 0.5f);

            Vector2 zeroZeroToWorld = mainCam.ViewportToWorldPoint(zeroZero);


            return zeroZeroToWorld;
        }

    }
    public void UpdateFollow(Transform trans)
    {
        cinemachine.m_Follow = trans;
    }
    public void UpdateDeadZone(Vector2 deadZone)
    {
        CinemachineFramingTransposer framingTransposer = cinemachine.GetCinemachineComponent<CinemachineFramingTransposer>();

        if (framingTransposer != null)
        {
            framingTransposer.m_DeadZoneWidth = deadZone.x;
            framingTransposer.m_DeadZoneHeight = deadZone.y;
        }
    }
    public void UpdateOffset(Vector3 offset, float timer = -1)
    {
        CinemachineFramingTransposer framingTransposer = cinemachine.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (offsetTween != null)
            offsetTween.Kill();
        Vector3 _startOffset = framingTransposer.m_TrackedObjectOffset;
        if (timer <= 0)
        {
            framingTransposer.m_TrackedObjectOffset = offset;
        }
        else
        {
            float evulate = 0;
            offsetTween = DOTween.To(() => evulate, x => evulate = x, 1, timer).OnUpdate(() =>
           {

               framingTransposer.m_TrackedObjectOffset = Vector3.Lerp(framingTransposer.m_TrackedObjectOffset, offset, evulate);
               OnChangeScreenSize();

           }).OnComplete(() =>
           {
               framingTransposer.m_TrackedObjectOffset = offset;
               OnChangeScreenSize();
           });
        }
    }
    public void UpdateOffset(Vector3 offset)
    {
        CinemachineFramingTransposer framingTransposer = cinemachine.GetCinemachineComponent<CinemachineFramingTransposer>();
        framingTransposer.m_TrackedObjectOffset = offset;
    }
    #endregion
    #region Method

    public void SetCameraOthographicSize(Camera _cam)
    {
        _cam.orthographicSize = mainCam.orthographicSize;
    }
    #endregion


}