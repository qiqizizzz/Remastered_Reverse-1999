/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗管理器，管理所有进行中的战斗实例
* │  类    名: BattleManager.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using GameServer.Battle.Data;

namespace GameServer.Battle
{
    internal class BattleManager
    {
        private readonly Dictionary<string, BattleInstance> _battles;
        private readonly ConfigManager _configManager;

        public BattleManager(ConfigManager configManager)
        {
            _configManager = configManager;
            _battles = new Dictionary<string, BattleInstance>();
        }

        // 为指定用户创建新战斗
        public BattleInstance CreateBattle(string username, int levelId, List<int> heroConfigIds = null)
        {
            RemoveBattle(username);

            var levelConfig = _configManager.Levels.Find(l => l.Id == levelId);
            if (levelConfig == null) return null;

            if (heroConfigIds == null || heroConfigIds.Count == 0)
                heroConfigIds = _configManager.Heroes.Select(h => h.Id).ToList();

            var battle = new BattleInstance(levelId, heroConfigIds, levelConfig.MonsterSpawns, _configManager, _configManager);
            _battles[username] = battle;
            return battle;
        }

        // 获取指定用户的战斗实例
        public BattleInstance GetBattle(string username)
        {
            _battles.TryGetValue(username, out var battle);
            return battle;
        }

        // 移除指定用户的战斗实例
        public void RemoveBattle(string username)
        {
            _battles.Remove(username);
        }
    }
}
