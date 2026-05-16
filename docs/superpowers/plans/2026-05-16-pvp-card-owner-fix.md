# PVP Card Owner Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复 PVP 初始发牌/手牌事件全部落到玩家 1、玩家 2 收不到卡牌的问题，同时保持 PVE 默认玩家 1 逻辑不变。

**Architecture:** 服务端在 PVP 开战时只收集一次初始化事件，然后按 `BattleEvent.EventOwnerId` 给每个玩家发送各自可见的私有手牌事件。客户端播放手牌事件时不再硬编码 `PlayerDecks[1]`，而是使用事件归属玩家 ID；PVP 中非本地玩家的私有手牌事件直接跳过，PVE 仍回退到玩家 1。

**Tech Stack:** Unity C#、Google.Protobuf 生成的 `GameProtocol.BattleEvent`、服务端 .NET C#、Unity Test Framework/NUnit。

---

## File Structure

- Modify: `Assets/Scripts/Module/fight/EventPlayer/EventPlayer_CardDraw.cs`
  - 负责客户端播放抽牌、弃牌、移动、合成、大招、入队等手牌事件。
  - 新增私有方法 `tryGetVisibleDeck(BattleEvent evt, out PlayerDeckEntity deck)`，集中处理 PVE/PVP 的玩家 ID 选择与 PVP 私有事件过滤。
- Modify: `AAAGameServer/ConsoleMock_Server/GameServer/Network/Protobuf/ProtocolHandlers/BattleHandler.cs`
  - 负责服务端战斗协议响应。
  - PVP 开战时只 `CollectEvents()` 一次，并按接收玩家过滤私有手牌事件。
- Test/Verify: `Assets/Editor/[test]/PvpCardOwnerTests.cs`
  - 新增轻量编辑器测试，先验证事件过滤规则：PVP 私有手牌事件只对对应 `EventOwnerId` 玩家可见，公共事件两边都可见。

---

### Task 1: Add failing tests for PVP private card event visibility

**Files:**
- Create: `Assets/Editor/[test]/PvpCardOwnerTests.cs`

- [ ] **Step 1: Write the failing test**

Create `Assets/Editor/[test]/PvpCardOwnerTests.cs` with this content:

```csharp
/*
* ┌──────────────────────────────────┐
* │  描    述: PVP手牌事件归属过滤测试
* │  类    名: PvpCardOwnerTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using GameProtocol;
using NUnit.Framework;

public class PvpCardOwnerTests
{
    [Test]
    public void FilterPrivateCardEvents_ForPlayer2_ShouldOnlyKeepPlayer2CardEventsAndPublicEvents()
    {
        var events = new List<BattleEvent>
        {
            new BattleEvent
            {
                EventType = BattleEventType.DrawCard,
                EventOwnerId = 1,
                DrawCard = new DrawCardParams { Card = new CardInfo { InstanceId = 101, ConfigId = 1, StarLevel = 1 } }
            },
            new BattleEvent
            {
                EventType = BattleEventType.DrawCard,
                EventOwnerId = 2,
                DrawCard = new DrawCardParams { Card = new CardInfo { InstanceId = 201, ConfigId = 2, StarLevel = 1 } }
            },
            new BattleEvent
            {
                EventType = BattleEventType.TurnStart,
                TurnStart = new TurnStartParams { IsPlayerTurn = true, RoundNumber = 1 }
            }
        };

        var visibleEvents = PvpBattleEventFilter.FilterVisibleEvents(events, 2);

        Assert.That(visibleEvents.Count, Is.EqualTo(2));
        Assert.That(visibleEvents[0].EventType, Is.EqualTo(BattleEventType.DrawCard));
        Assert.That(visibleEvents[0].EventOwnerId, Is.EqualTo(2));
        Assert.That(visibleEvents[1].EventType, Is.EqualTo(BattleEventType.TurnStart));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run Unity EditMode tests for `PvpCardOwnerTests`.

Expected: FAIL/compile error because `PvpBattleEventFilter` does not exist yet.

- [ ] **Step 3: Keep the failure as proof of missing behavior**

Do not change production code until the failure is confirmed.

---

### Task 2: Implement server-side PVP event filtering and single collection

**Files:**
- Modify: `AAAGameServer/ConsoleMock_Server/GameServer/Network/Protobuf/ProtocolHandlers/BattleHandler.cs`

- [ ] **Step 1: Add the event filter helper near the bottom of `BattleHandler` before `sendBattleError`**

```csharp
        // 按玩家可见性过滤PVP事件
        private static List<BattleEvent> filterVisiblePvpEvents(List<BattleEvent> events, int viewerPlayerId)
        {
            var visibleEvents = new List<BattleEvent>();
            foreach (var evt in events)
            {
                if (isPrivateCardEvent(evt.EventType) && evt.EventOwnerId != viewerPlayerId)
                    continue;

                visibleEvents.Add(evt);
            }

            return visibleEvents;
        }

        // 判断是否为只属于单个玩家可见的手牌事件
        private static bool isPrivateCardEvent(BattleEventType eventType)
        {
            return eventType == BattleEventType.DrawCard ||
                   eventType == BattleEventType.DiscardCard ||
                   eventType == BattleEventType.CardMoved ||
                   eventType == BattleEventType.MergeCard ||
                   eventType == BattleEventType.GrantUltimate ||
                   eventType == BattleEventType.ShuffleDeck;
        }
```

- [ ] **Step 2: Change `handleSubmitPvPTeam` to collect initial events once**

Replace:

```csharp
            sendPvpBattleStart(client.Server.GetClientByUsername(room.Player1), battle, 1);
            sendPvpBattleStart(client.Server.GetClientByUsername(room.Player2), battle, 2);
```

with:

```csharp
            var initialEvents = battle.CollectEvents();
            sendPvpBattleStart(client.Server.GetClientByUsername(room.Player1), battle, 1, initialEvents);
            sendPvpBattleStart(client.Server.GetClientByUsername(room.Player2), battle, 2, initialEvents);
```

- [ ] **Step 3: Change `sendPvpBattleStart` signature and event assignment**

Replace:

```csharp
        private void sendPvpBattleStart(Client client, BattleInstance battle, int playerId)
        {
            if (client == null) return;

            var battlePack = new BattlePack
            {
                PlayerId = playerId,
                IsTeamReady = true,
                StateSnapshot = battle.GetStateSnapshot(playerId)
            };
            battlePack.Events.AddRange(battle.CollectEvents());
            sendBattleResponse(client, ActionCode.SubmitPvPteam, battlePack);
        }
```

with:

```csharp
        private void sendPvpBattleStart(Client client, BattleInstance battle, int playerId, List<BattleEvent> initialEvents)
        {
            if (client == null) return;

            var battlePack = new BattlePack
            {
                PlayerId = playerId,
                IsTeamReady = true,
                StateSnapshot = battle.GetStateSnapshot(playerId)
            };
            battlePack.Events.AddRange(filterVisiblePvpEvents(initialEvents, playerId));
            sendBattleResponse(client, ActionCode.SubmitPvPteam, battlePack);
        }
```

- [ ] **Step 4: Verify server compiles**

Run:

```bash
dotnet build "AAAGameServer/ConsoleMock_Server/GameServer/GameServer.csproj"
```

Expected: build succeeds with no new C# errors.

---

### Task 3: Implement client-side deck lookup by EventOwnerId

**Files:**
- Modify: `Assets/Scripts/Module/fight/EventPlayer/EventPlayer_CardDraw.cs`

- [ ] **Step 1: Add the missing using**

Add this with the other using statements:

```csharp
using Module.fight.Core.Entities;
```

- [ ] **Step 2: Add local-player and private-event helpers inside `EventPlayer_CardDraw`**

Add these methods near the top of the class:

```csharp
        // 获取当前客户端可见的事件归属牌库
        private static bool tryGetVisibleDeck(BattleEvent evt, out PlayerDeckEntity deck)
        {
            int eventOwnerId = evt.EventOwnerId == 0 ? 1 : evt.EventOwnerId;
            if (GameApp.PvpSession != null && GameApp.PvpSession.IsInPvp && eventOwnerId != GameApp.PvpSession.CurrentPlayerId)
            {
                deck = null;
                return false;
            }

            return GameApp.CardManager.BattleContext.PlayerDecks.TryGetValue(eventOwnerId, out deck);
        }
```

- [ ] **Step 3: Replace `PlayerDecks[1]` in single-event handlers**

For `PlayDrawCard`, replace:

```csharp
            var deck = GameApp.CardManager.BattleContext.PlayerDecks[1];
```

with:

```csharp
            if (!tryGetVisibleDeck(evt, out var deck)) return;
```

Repeat the same replacement in:

- `PlayDiscardCard`
- `PlayCardMoved`
- `PlayMergeCard`
- `PlayGrantUltimate`
- `PlayEnqueueCard`

In `PlayMergeCard`, also replace:

```csharp
                        GameApp.CardManager.EventBus.OnCardMerged?.Invoke(1, keptCard, consumedCards[i], resultStar);
```

with:

```csharp
                        GameApp.CardManager.EventBus.OnCardMerged?.Invoke(evt.EventOwnerId == 0 ? 1 : evt.EventOwnerId, keptCard, consumedCards[i], resultStar);
```

- [ ] **Step 4: Replace batch draw handling with per-owner filtering**

Replace the start of `PlayDrawCardBatch`:

```csharp
            var deck = GameApp.CardManager.BattleContext.PlayerDecks[1];
            for (int i = 0; i < events.Count; i++)
            {
                var cardInfo = events[i].DrawCard.Card;
                if (deck.HandCards.Exists(c => c.InstanceId == cardInfo.InstanceId)) continue;

                var card = new CardEntity(cardInfo.InstanceId, cardInfo.ConfigId, cardInfo.StarLevel);
                deck.HandCards.Insert(0, card);
            }

            GameApp.MessageCenter.PostEvent(EventDefines.UpdateHandCards, deck.HandCards);
```

with:

```csharp
            PlayerDeckEntity updatedDeck = null;
            for (int i = 0; i < events.Count; i++)
            {
                var evt = events[i];
                if (!tryGetVisibleDeck(evt, out var deck)) continue;

                var cardInfo = evt.DrawCard.Card;
                if (deck.HandCards.Exists(c => c.InstanceId == cardInfo.InstanceId)) continue;

                var card = new CardEntity(cardInfo.InstanceId, cardInfo.ConfigId, cardInfo.StarLevel);
                deck.HandCards.Insert(0, card);
                updatedDeck = deck;
            }

            if (updatedDeck != null)
                GameApp.MessageCenter.PostEvent(EventDefines.UpdateHandCards, updatedDeck.HandCards);
```

- [ ] **Step 5: Verify Unity C# compile**

Open Unity or run the project’s normal Unity compile workflow.

Expected: no compile errors in `EventPlayer_CardDraw.cs`.

---

### Task 4: Verify PVE remains unchanged and PVP card ownership is fixed

**Files:**
- Verify only unless compile requires small corrections.

- [ ] **Step 1: Run server build**

```bash
dotnet build "AAAGameServer/ConsoleMock_Server/GameServer/GameServer.csproj"
```

Expected: build succeeds.

- [ ] **Step 2: Run Unity EditMode tests**

Run EditMode tests including `PvpCardOwnerTests` and existing `CardActionQueueTests`.

Expected: all selected tests pass.

- [ ] **Step 3: Manual PVE smoke test**

Start a PVE fight.

Expected:
- player hand initializes normally;
- no `PlayerDecks[0]` / missing dictionary key error;
- draw, discard, play-card UI still use the player 1 deck.

- [ ] **Step 4: Manual PVP smoke test**

Start two clients, match them, submit both teams.

Expected:
- client assigned `PlayerId = 1` receives only player 1 private card events;
- client assigned `PlayerId = 2` receives only player 2 private card events;
- each side has its own starting hand;
- no side receives duplicate initial hand cards from the other player.

---

## Self-Review

- Spec coverage: The plan covers server event collection, server visibility filtering, client EventOwnerId deck lookup, PVE fallback to player 1, and PVP/PVE verification.
- Placeholder scan: No TBD/TODO placeholders remain; every code-changing step includes concrete replacement snippets.
- Type consistency: Uses existing `BattleEvent.EventOwnerId`, `BattleEventType`, `PlayerDeckEntity`, `GameApp.PvpSession.CurrentPlayerId`, and `BattlePack.Events` names observed in the current codebase.
