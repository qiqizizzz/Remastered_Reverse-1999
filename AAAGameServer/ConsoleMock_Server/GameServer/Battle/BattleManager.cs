/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗管理器，管理所有进行中的战斗实例（PvE + PvP）
* │  类    名: BattleManager.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using System.Linq;
using GameServer.Battle.Core;
using GameServer.Battle.Data;
using Network.Matchmaking;

namespace GameServer.Battle
{
    internal class BattleManager
    {
        private readonly Dictionary<string, BattleInstance> _battles;
        private readonly ConfigManager _configManager;
        private readonly MatchmakingQueue _matchQueue = new();

        public BattleManager(ConfigManager configManager)
        {
            _configManager = configManager;
            _battles = new Dictionary<string, BattleInstance>();
        }

        #region 战斗类型
        // 为指定用户创建 PvE 战斗
        public BattleInstance CreateBattle(string username, int levelId, List<int>? heroConfigIds = null)
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

        // 为两个玩家创建 PvP 战斗
        public BattleInstance CreatePvPBattle(string player1, string player2,
            List<int>? heroIdsP1 = null, List<int>? heroIdsP2 = null)
        {
            RemoveBattle(player1);
            RemoveBattle(player2);

            if (heroIdsP1 == null || heroIdsP1.Count == 0)
                heroIdsP1 = _configManager.Heroes.Select(h => h.Id).ToList();
            if (heroIdsP2 == null || heroIdsP2.Count == 0)
                heroIdsP2 = _configManager.Heroes.Select(h => h.Id).ToList();

            var battle = new BattleInstance(
                levelId: 0,
                heroIdsP1: heroIdsP1,
                heroIdsP2: heroIdsP2,
                monsterSpawns: null,
                configManager: _configManager,
                cardCatalog: _configManager,
                gameMode: GameMode.PvP);

            _battles[player1] = battle;
            _battles[player2] = battle;
            return battle;
        }
        #endregion

        #region 战斗实例
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
        #endregion

        #region 匹配系统
        public void JoinQueue(string username) => _matchQueue.Enqueue(username);

        public void LeaveQueue(string username) => _matchQueue.Dequeue(username);

        public (string p1, string p2, BattleInstance)? TryMatch()
        {
            if (!_matchQueue.CanMatch) return null;

            var (p1, p2) = _matchQueue.PopMatch();
            var battle = CreatePvPBattle(p1, p2);
            _battles[p1] = battle;
            _battles[p2] = battle;
            return (p1, p2, battle);
        }
        #endregion
    }
}
