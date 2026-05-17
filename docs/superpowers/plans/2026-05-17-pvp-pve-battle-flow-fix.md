# PVP/PVE Battle Flow Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复 PVP/PVE 战斗中发牌数量、提前结算伤害，以及 PVP 状态污染后续 PVE 的问题，并确保双客户端各自只恢复自己的战斗快照。

**Architecture:** 服务端统一用“存活 Hero 数 × 2，最低保底 4”计算手牌目标和行动队列上限，PVP 的 `EndTurn` 只能在队列填满后结算，避免未出完牌就结算。PVP 开战、回合切换、战斗结束时都通过同一套状态重置入口清理队列、回合标志和会话残留；客户端按 `BattlePack.PlayerId` 选择本地快照恢复目标，避免两个客户端复用同一份本地玩家 ID。

**Tech Stack:** Unity C#、.NET C# 服务端、Google.Protobuf、Unity Test Framework / NUnit、xUnit。

---

## File Structure

- Modify: `AAAGameServer/ConsoleMock_Server/GameServer/Battle/Core/Systems/BattleInitSystem.cs`
  - 负责战前初始化、回合开始补牌、队列上限缩放。
  - 把发牌目标和队列上限统一为同一套规则：`max(4, aliveHeroCount * 2)`。
  - 让 PVP 和 PVE 使用同一个规则入口。
- Modify: `AAAGameServer/ConsoleMock_Server/GameServer/Battle/BattleInstance.cs`
  - 负责回合推进、PVP/PVE 结算、战斗状态切换。
  - 限制 PVP `EndTurn`：行动队列未满时直接返回。
  - 在结算后/切换模式时确保行动队列、轮次和重置流程一致。
- Modify: `AAAGameServer/ConsoleMock_Server/GameServer/Network/Protobuf/ProtocolHandlers/BattleHandler.cs`
  - 负责服务端向双客户端发送战斗快照和事件。
  - PVP 开战和回合结算时按接收玩家发送各自快照，避免串数据。
- Modify: `Assets/Scripts/Module/fight/FightController.cs`
  - 负责客户端接收战斗快照并恢复本地战斗上下文。
  - 只按服务端下发的 `BattlePack.PlayerId` 恢复对应玩家的牌库与行动队列上限。
- Modify: `Assets/Scripts/Module/fight/CardMgr/CardManager.cs`
  - 负责本地卡牌系统、牌库初始化、回合规则缩放。
  - 确认本地 `maxHandCardCount` 与 `CardActionQueue.MaxActionCount` 跟服务端同规则。
- Modify: `Assets/Scripts/Module/fight/CardMgr/ActionQueue/CardActionQueue.cs`
  - 负责行动队列容量与清空行为。
  - 保证在模式切换/战斗重进时上限和内容都能被正确刷新。
- Modify: `Assets/Scripts/Module/fight/EventPlayer/EventPlayer_CharacterShift.cs`
  - 负责回合开始/结束播放、PVP/PVE 输出状态切换。
  - 补充战斗结束与新战斗进入时的局部状态清理。
- Modify: `Assets/Scripts/Module/fight/PlayEventSequence.cs`
  - 负责串行播放服务端事件。
  - 确认回合输出收口逻辑不会把上一局的 pending 状态带进下一局。
- Modify: `Assets/Scripts/Module/Matchmaking/Data/PvpSession.cs`
  - 负责 PVP 会话状态。
  - 确认战斗结束/离开时清空会话字段，避免下一次进入沿用旧玩家 ID。
- Test: `AAAGameServer/ConsoleMock_Server/GameServer.Tests/PvpBattleFlowTests.cs`
  - 负责服务端 PVP 战斗流程回归测试。
  - 增加发牌数量、未满队列不能结算、战斗结束后重进不污染状态的覆盖。
- Test: `Assets/Editor/[test]/FightControllerBattleResetTests.cs`
  - 负责客户端快照恢复和 PVP/PVE 切换回归测试。
  - 验证两个客户端分别恢复自己的玩家快照，且 PVP 状态不会污染后续 PVE。

---

### Task 1: Add failing tests for card count, early resolve, and battle reset

**Files:**
- Modify: `AAAGameServer/ConsoleMock_Server/GameServer.Tests/PvpBattleFlowTests.cs`
- Create: `Assets/Editor/[test]/FightControllerBattleResetTests.cs`

- [ ] **Step 1: Write the failing server-side tests**

Add these tests to `AAAGameServer/ConsoleMock_Server/GameServer.Tests/PvpBattleFlowTests.cs`:

```csharp
        [Fact]
        public void InitDeck_InPvpOneHero_ShouldKeepFourNormalCardsForRound()
        {
            var manager = createBattleManager();
            manager.JoinQueue("alice");
            manager.JoinQueue("bob");
            manager.TryMatch();
            manager.SubmitPvpTeam("alice", new List<int> { 1001, 1002, 1003, 1004 });
            var battle = manager.SubmitPvpTeam("bob", new List<int> { 1001, 1002, 1003, 1004 });
            Assert.NotNull(battle);
            battle.CollectEvents();

            foreach (var entity in battle.AllEntities.Where(e => e.OwnerPlayerId == 2 && e.InstanceId != battle.AllEntities.First(e => e.OwnerPlayerId == 2).InstanceId))
                entity.CurrentHp = 0;

            playCardsUntilQueueFull(battle, 2, 1);
            battle.EndTurn(2);
            battle.CollectEvents();
            playCardsUntilQueueFull(battle, 1, 2);
            battle.EndTurn(1);
            battle.CollectEvents();

            Assert.Equal(4, countNormalHandCards(battle, 2));
        }

        [Fact]
        public void InitDeck_InPvpThreeHeroes_ShouldKeepSixNormalCardsForRound()
        {
            var manager = createBattleManager();
            manager.JoinQueue("alice");
            manager.JoinQueue("bob");
            manager.TryMatch();
            manager.SubmitPvpTeam("alice", new List<int> { 1001, 1002, 1003, 1004 });
            var battle = manager.SubmitPvpTeam("bob", new List<int> { 1001, 1002, 1003, 1004 });
            Assert.NotNull(battle);
            battle.CollectEvents();

            Assert.Equal(6, battle.Context.ActionQueue.MaxQueueSize);
        }

        [Fact]
        public void EndTurn_InPvpBeforeQueueFull_ShouldNotResolveActions()
        {
            var manager = createBattleManager();
            manager.JoinQueue("alice");
            manager.JoinQueue("bob");
            manager.TryMatch();
            manager.SubmitPvpTeam("alice", new List<int> { 1001, 1002, 1003, 1004 });
            var battle = manager.SubmitPvpTeam("bob", new List<int> { 1001, 1002, 1003, 1004 });
            Assert.NotNull(battle);
            battle.CollectEvents();

            var player2Card = battle.Context.PlayerDecks[2].HandCards[0];
            var player1Target = battle.AllEntities.First(e => e.OwnerPlayerId == 1);
            float hpBefore = player1Target.CurrentHp;

            Assert.True(battle.PlayCard(2, player2Card.InstanceId, player1Target.InstanceId, 0));
            battle.CollectEvents();
            battle.EndTurn(2);
            var events = battle.CollectEvents();

            Assert.DoesNotContain(events, e => e.EventType == BattleEventType.DamageTaken);
            Assert.Equal(hpBefore, player1Target.CurrentHp);
            Assert.Equal(BattleState.PlayerTurn, battle.State);
        }
```

Create `Assets/Editor/[test]/FightControllerBattleResetTests.cs` with this content:

```csharp
/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗快照恢复与模式切换回归测试
* │  类    名: FightControllerBattleResetTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using NUnit.Framework;

namespace Tests.Fight
{
    public class FightControllerBattleResetTests
    {
        [Test]
        public void RestoreFromStateSnapshot_ShouldUsePacketPlayerIdForDeckSelection()
        {
            Assert.Pass("待实现后由客户端测试覆盖实际恢复逻辑");
        }

        [Test]
        public void ExitPvp_ShouldClearBattleRuntimeStateBeforeNextPve()
        {
            Assert.Pass("待实现后由客户端测试覆盖状态重置逻辑");
        }
    }
}
```

- [ ] **Step 2: Run the server tests to confirm the new assertions fail first**

Run:

```bash
dotnet test "AAAGameServer/ConsoleMock_Server/GameServer.Tests/GameServer.Tests.csproj" --filter FullyQualifiedName~PvpBattleFlowTests
```

Expected: the new PVP count / queue / resolve tests fail because the current implementation still uses the old scaling and end-turn behavior.

- [ ] **Step 3: Run the client test scaffold to confirm it is not yet meaningful**

Run the Unity EditMode test runner for `FightControllerBattleResetTests`.

Expected: the scaffold does not yet verify the real behavior and will need implementation before it can pass.

---

### Task 2: Unify server-side hand size and queue size scaling

**Files:**
- Modify: `AAAGameServer/ConsoleMock_Server/GameServer/Battle/Core/Systems/BattleInitSystem.cs`
- Modify: `AAAGameServer/ConsoleMock_Server/GameServer/Battle/BattleInstance.cs`

- [ ] **Step 1: Replace the hand-size and queue-size switch with one shared rule**

In `BattleInitSystem.cs`, replace the existing scaling logic with this minimal shared rule:

```csharp
        public void UpdateScalingRules()
        {
            int aliveHeroCount = getAliveHeroCount(_env.CurrentPlayerId);
            int targetCount = Math.Max(4, aliveHeroCount * 2);
            _env.Context.ActionQueue.MaxQueueSize = targetCount;
        }

        private int getTargetNormalHandCount(int playerId)
        {
            int aliveHeroCount = getAliveHeroCount(playerId);
            return Math.Max(4, aliveHeroCount * 2);
        }
```

Keep the existing `ProcessRoundStartHandFix()` flow, but make it draw until the new target is reached.

- [ ] **Step 2: Keep PVP `EndTurn` blocked until the queue is full**

In `BattleInstance.cs`, keep the early return on PVP `EndTurn`, but ensure it only checks the queue against the same rule source used by the init system:

```csharp
            if (_env.Mode == GameMode.PvP && _env.Context.ActionQueue.QueuedCards.Count < _env.Context.ActionQueue.MaxQueueSize) return;
```

Do not add any alternate resolve path here.

- [ ] **Step 3: Verify server tests now pass for the new scaling rule**

Run:

```bash
dotnet test "AAAGameServer/ConsoleMock_Server/GameServer.Tests/GameServer.Tests.csproj" --filter FullyQualifiedName~PvpBattleFlowTests.InitDeck_InPvpOneHero_ShouldKeepFourNormalCardsForRound
```

Expected: the one-hero and three-hero assertions pass after the scaling change.

---

### Task 3: Fix server battle state cleanup and PVP finish flow

**Files:**
- Modify: `AAAGameServer/ConsoleMock_Server/GameServer/Battle/BattleInstance.cs`
- Modify: `AAAGameServer/ConsoleMock_Server/GameServer/Network/Protobuf/ProtocolHandlers/BattleHandler.cs`

- [ ] **Step 1: Reset battle runtime state at the mode boundary**

Add or reuse a focused helper in `BattleInstance.cs` to clear runtime state when a battle ends or a new battle starts:

```csharp
        private void resetBattleRuntimeState()
        {
            _env.Context.ActionQueue.QueuedCards.Clear();
            _state = BattleState.PlayerTurn;
        }
```

Call this helper from the PVP end/beginning path where the old battle state should not leak into the next session.

- [ ] **Step 2: Make PVP responses carry the correct per-player snapshot**

Keep the PVP battle response path using `battle.GetStateSnapshot(participant.PlayerId)` for each participant, and make sure `ActionCode.EndTurn` is the only path that refreshes the snapshot on the way out.

```csharp
                if (actionCode == ActionCode.EndTurn)
                    battlePack.StateSnapshot = battle.GetStateSnapshot(participant.PlayerId);
```

- [ ] **Step 3: Verify the cleanup path with the battle flow tests**

Run:

```bash
dotnet test "AAAGameServer/ConsoleMock_Server/GameServer.Tests/GameServer.Tests.csproj" --filter FullyQualifiedName~EntityDied_InPvp_ShouldOnlyRemoveCardsFromDeadOwnerDeck
```

Expected: battle end and cleanup still behave correctly after the new reset helper.

---

### Task 4: Fix client snapshot restore and state isolation between PVP and PVE

**Files:**
- Modify: `Assets/Scripts/Module/fight/FightController.cs`
- Modify: `Assets/Scripts/Module/fight/CardMgr/CardManager.cs`
- Modify: `Assets/Scripts/Module/fight/PlayEventSequence.cs`
- Modify: `Assets/Scripts/Module/fight/EventPlayer/EventPlayer_CharacterShift.cs`
- Modify: `Assets/Scripts/Module/Matchmaking/Data/PvpSession.cs`
- Modify: `Assets/Scripts/Module/fight/CardMgr/ActionQueue/CardActionQueue.cs`

- [ ] **Step 1: Restore the client deck from the packet player ID instead of the local session fallback**

In `FightController.cs`, change snapshot restore to use the packet-provided player ID when available:

```csharp
            int playerId = snapshot.PlayerId;
            var deck = GameApp.CardManager.BattleContext.PlayerDecks[playerId];
```

If `BattleStateSnapshot` does not currently expose `PlayerId`, add it to the snapshot model and wire it through the restore path.

- [ ] **Step 2: Reset queue size and local runtime state when entering or leaving battle**

In `CardManager.cs`, add a clear runtime reset path that resets both the action queue contents and its size back to the current mode rule before loading a new battle:

```csharp
        public void ResetBattleRuntime()
        {
            CardActionQueue.Clear();
            CardActionQueue.MaxActionCount = _maxHandCardCount switch
            {
                8 => 4,
                6 => 3,
                4 => 2,
                _ => 4
            };
        }
```

Adjust the exact mapping so it matches the shared `max(4, aliveHeroCount * 2)` rule and does not carry an old PVP queue size into PVE.

- [ ] **Step 3: Clear pending playback flags when battle output finishes**

In `PlayEventSequence.cs` and `EventPlayer_CharacterShift.cs`, make sure the following flags are reset at battle end / new battle start:

```csharp
PlayEventSequence.IsPlayerTurnResolving = false;
PlayEventSequence.IsEnemyTurnResolving = false;
PlayEventSequence.PendingPlayerPlayCardCount = 0;
PlayEventSequence.PendingPlayerCasterOwnerIds.Clear();
```

Also clear any queue contents used for replaying move/attack animation so the next PVE session starts with empty UI slots.

- [ ] **Step 4: Clear PVP session state when leaving battle**

In `PvpSession.cs`, confirm `Clear()` resets match ID, current player ID, and `IsInPvp`. Make sure the battle exit path actually calls it when leaving PVP.

- [ ] **Step 5: Verify client-side recovery after PVP and before PVE**

Run the Unity EditMode tests and manually open one PVP battle followed by one PVE battle.

Expected:
- action queue UI returns to the correct full placeholder count
- the next PVE battle uses local player 1 again
- no stale PVP queue size remains
- the two clients show different decks when restoring snapshots

---

### Task 5: Verify full regression coverage and tidy tests

**Files:**
- Modify: `AAAGameServer/ConsoleMock_Server/GameServer.Tests/PvpBattleFlowTests.cs`
- Modify: `Assets/Editor/[test]/FightControllerBattleResetTests.cs`

- [ ] **Step 1: Replace placeholder client tests with real assertions once the restore path is wired**

Update the client test file so it asserts the actual restored deck owner and reset queue size instead of `Assert.Pass`.

- [ ] **Step 2: Run the full targeted regression suite**

Run:

```bash
dotnet test "AAAGameServer/ConsoleMock_Server/GameServer.Tests/GameServer.Tests.csproj" --filter FullyQualifiedName~PvpBattleFlowTests
```

Then run the Unity EditMode tests for the battle reset coverage.

Expected: all targeted tests pass and no new failures appear in unrelated battle tests.

- [ ] **Step 3: Commit the completed fix set**

Commit the server and client fixes together once the regression suite is green.

```bash
git add AAAGameServer/ConsoleMock_Server/GameServer.Tests/PvpBattleFlowTests.cs \
        AAAGameServer/ConsoleMock_Server/GameServer/Battle/BattleInstance.cs \
        AAAGameServer/ConsoleMock_Server/GameServer/Battle/Core/Systems/BattleInitSystem.cs \
        AAAGameServer/ConsoleMock_Server/GameServer/Network/Protobuf/ProtocolHandlers/BattleHandler.cs \
        Assets/Scripts/Module/fight/FightController.cs \
        Assets/Scripts/Module/fight/CardMgr/CardManager.cs \
        Assets/Scripts/Module/fight/PlayEventSequence.cs \
        Assets/Scripts/Module/fight/EventPlayer/EventPlayer_CharacterShift.cs \
        Assets/Scripts/Module/Matchmaking/Data/PvpSession.cs \
        Assets/Scripts/Module/fight/CardMgr/ActionQueue/CardActionQueue.cs

git commit -m "fix: unify battle flow state and queue scaling"
```
