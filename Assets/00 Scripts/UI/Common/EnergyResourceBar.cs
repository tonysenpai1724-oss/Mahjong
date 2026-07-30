using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
public class EnergyResourceBar : MonoBehaviour
{
    public TextMeshProUGUI txtValue, txtCooldown;
    long currentEnergy, maxEnergy;
    private void Start()
    {
        TigerForge.EventManager.StartListening(Constant.ON_PLAYER_RESOURCE_UPDATE, OnResourceChanged);
        TigerForge.EventManager.StartListening(Constant.EVENT_TIMER_TICK, OnTick);
        OnResourceChanged();
        OnTick();
    }
    void OnResourceChanged()
    {
        currentEnergy = IPlayerResource.Instance.GetCommonResource(ECommonResource.Energy);
        maxEnergy = IPlayerResource.Instance.GetMaxEnergy();
        txtValue.text = $"{currentEnergy}/{maxEnergy}";
    }
    void OnTick()
    {
        txtCooldown.text = currentEnergy < maxEnergy ? Helper.TimeToString(ITimerController.Instance.GetNextFreeEnergy() - DateTime.UtcNow) : "";
    }
}
