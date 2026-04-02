/*
* ┌──────────────────────────────────┐
* │  描    述: 游戏数据管理器                      
* │  类    名: GameDataManager.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using UnityEngine;

namespace DefaultNamespace
{
    public class GameDataManager
    {
        public string PlayerName;
        public bool isConnected;// 是否连接服务器
        public bool isServerOnline;// 服务器是否在线
        
        [Header("卡牌相关")]
        public int CharacterCount;// 角色数量

        public GameDataManager()
        {
            PlayerName = "";
            isConnected = false;
            isServerOnline = false;
            CharacterCount = 4;
        }
        
        public void SetPlayerName(string name)
        {
            PlayerName = name;
        }
    }
}