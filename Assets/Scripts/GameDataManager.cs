/*
* ┌──────────────────────────────────┐
* │  描    述: 游戏数据管理器                      
* │  类    名: GameDataManager.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace DefaultNamespace
{
    public class GameDataManager
    {
        public string PlayerName;

        public GameDataManager()
        {
            PlayerName = "";
        }
        
        public void SetPlayerName(string name)
        {
            PlayerName = name;
        }
    }
}