using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using DG.Tweening;
public class PopupLuckyWheel : UIBase
{
    public List<UiResourceItem> lstResourceItems;
    public GameObject selectedObj, btnFree, btnGem;
    public TextMeshProUGUI txtCost, txtCountDown;
    public AnimationCurve rollCurve;
    public int rollOffset = 5;
    public float timeRoll;
    Dictionary<GameResource, float> dicRate;
    int currentIndex;
    public override void Show()
    {
        base.Show();
        if (dicRate == null)
        {
            dicRate = new Dictionary<GameResource, float>();
            int id = 0;
            foreach (var item in DataSystem.Instance.dataLuckyWheel.dicRewards)
            {
                GameResource gameResource = GameResource.GetResource(item.Key);
                dicRate.Add(gameResource, item.Value);
                lstResourceItems[id].InitResouce(gameResource);
                id++;
            }
        }
        selectedObj.transform.position = lstResourceItems[currentIndex % lstResourceItems.Count].transform.position;
        TigerForge.EventManager.StartListening(Constant.EVENT_TIMER_TICK, OnTick);
        OnTick();
    }
    public override void OnDisable()
    {
        base.OnDisable();
        TigerForge.EventManager.StopListening(Constant.EVENT_TIMER_TICK, OnTick);
    }
    void OnTick()
    {
        btnFree.SetActive(ILuckyWheelController.Instance.CanSpinFree());
        btnGem.SetActive(!ILuckyWheelController.Instance.CanSpinFree());
        txtCountDown.gameObject.SetActive(!ILuckyWheelController.Instance.CanSpinFree());
        txtCost.text = ILuckyWheelController.Instance.GetSpinCost().ToString();
        txtCountDown.text = ILuckyWheelController.Instance.GetNextFreeCountdown();
    }
    public void OnClickSpin()
    {
        ILuckyWheelController.Instance.SpinWheel(() =>
        {
            GameResource reward = Helper.GetRandomByPercent(dicRate);
            PackageResource pack = new PackageResource();
            pack.AddResource(reward);
            pack.ReceiveResource(EResourceFrom.LuckySpin);
            int index = dicRate.Keys.ToList().IndexOf(reward);
            StartCoroutine(IESpinWheel(index, pack));
        });
    }
    IEnumerator IESpinWheel(int targetIndex, PackageResource pack)
    {
        blockPanel.gameObject.SetActive(true);
        int _targetIndex = currentIndex + (lstResourceItems.Count - currentIndex % lstResourceItems.Count) + lstResourceItems.Count * rollOffset + targetIndex;
        bool tweening = true;
        DOTween.To(() => currentIndex, x =>
        {
            currentIndex = x;
        }, _targetIndex, timeRoll).SetEase(rollCurve).OnUpdate(() =>
        {
            selectedObj.transform.position = lstResourceItems[currentIndex % lstResourceItems.Count].transform.position;
        }).OnComplete(() =>
        {
            selectedObj.transform.position = lstResourceItems[currentIndex % lstResourceItems.Count].transform.position;
            tweening = false;
        });
        yield return new WaitUntil(() => !tweening);
        UIManager.Instance.ShowPopupReward(pack);
        blockPanel.gameObject.SetActive(false);
        OnTick();
    }
}
