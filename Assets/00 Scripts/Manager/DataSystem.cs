using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataSystem : Singleton<DataSystem>
{
    public DataSprites dataSprites;
    public DataGamePrefabs dataGamePrefabs;
    public DataSoundEffect dataSoundEffect;
    public DataFx dataAnimationEffect;
    public DataShop dataShop;
    public DataIAP dataIAP;
    public DataDailyQuest dataDailyQuest;
    public DataLuckyWheel dataLuckyWheel;
    public DataInviteFriend dataInviteFriend;
    public DataPartnerReferal dataPartnerReferal;
    public DataLeaderboard dataLeaderboard;
}
