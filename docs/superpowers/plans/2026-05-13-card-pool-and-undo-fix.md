# Card Pool And Undo Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix common card pool exhaustion for Attack/Buff cards and make the first undo restore the card to its pre-action hand position.

**Architecture:** Keep the existing client-predicted UI flow and server-authoritative state sync. Increase the common card pool using the real maximum simultaneous common-card UI demand, and move client undo snapshots to the same pre-command timing already used by the server command processor. Add `UNITY_EDITOR` diagnostics only, so release builds stay unchanged.

**Tech Stack:** Unity C#, DOTween, existing `GameApp.MessageCenter`, existing client/server battle protocol, existing server `BattleStateSnapshot` undo response.

---

## File Structure

- Modify `Assets/Scripts/Module/fight/CardMgr/CardPoolManager.cs`
  - Compute common-card pool capacity from hand max, action queue max, and a small temporary animation buffer.
  - Add editor-only pool diagnostics when preloading and when the pool is exhausted.
- Modify `Assets/Scripts/Module/fight/CardMgr/HandCard/HandCardOperator.cs`
  - Capture snapshots before local play/move prediction changes state.
  - Use the operation-before snapshot for undo restore index.
  - Add editor-only undo diagnostics.
- Modify `Assets/Scripts/Module/fight/FightController.cs`
  - When applying server `BattleStateSnapshot` after undo, broadcast `UpdateHandCards` with the existing undo flag (`true`) so the hand UI does not apply new-card stagger timing during rollback.
- Modify `Assets/Scripts/Module/fight/Network/BattleNetworkController.cs`
  - Mark the incoming undo response as undo state sync before broadcasting to `FightController`.
- No new production files.
- No new persistent test files for this fix because the project currently has no business test assembly; verification uses compile checks, editor diagnostics, and Unity play-mode reproduction.

---

### Task 1: Fix Common Card Pool Capacity And Diagnostics

**Files:**
- Modify: `Assets/Scripts/Module/fight/CardMgr/CardPoolManager.cs:20-186`

- [ ] **Step 1: Record the current failing scenario**

In Unity Editor, reproduce the current bug before editing:

1. Enter a battle with 4 heroes.
2. Let the initial 8 normal hand cards appear.
3. Queue 4 normal cards, especially Attack/Buff cards.
4. Trigger draw/refresh/undo paths until the Console logs:

```text
[CardPoolManager] 卡牌对象池耗尽！请检查 Attack 生成配置。
```

or:

```text
[CardPoolManager] 卡牌对象池耗尽！请检查 Buff 生成配置。
```

Expected: the existing Console error proves the scenario before the fix.

- [ ] **Step 2: Add capacity constants and pool diagnostics helpers**

In `CardPoolManager`, replace the field block at the top of the class with this exact block:

```csharp
        private const int COMMON_CARD_TEMP_BUFFER = 4;
        private const int ULTIMATE_CARD_POOL_COUNT = 6;

        private readonly List<UI_CommonCardItem> _commonCardPool;
        private readonly List<UI_UltimateCardItem> _ultimateCardPool;
        
        private int _totalPoolToLoad;
        private int _loadedPoolCount;
        
        private readonly Transform _cardDeckTf;
```

Then add these private methods before `#endregion` of the preload region, after `CheckAllPoolLoaded()`:

```csharp
        // 计算普通卡池容量
        private int getCommonCardPoolCount()
        {
            return GameApp.CardManager.maxHandCardCount +
                   GameApp.CardManager.CardActionQueue.MaxActionCount +
                   COMMON_CARD_TEMP_BUFFER;
        }

#if UNITY_EDITOR
        // 输出对象池诊断信息
        private void logPoolState(CardType type, string reason)
        {
            int commonActive = _commonCardPool.Count(x => x != null && x.gameObject.activeSelf);
            int commonInactive = _commonCardPool.Count - commonActive;
            int ultimateActive = _ultimateCardPool.Count(x => x != null && x.gameObject.activeSelf);
            int ultimateInactive = _ultimateCardPool.Count - ultimateActive;
            Debug.Log($"[{nameof(CardPoolManager)}] {reason} Type={type}, Common={commonActive}/{_commonCardPool.Count} active, CommonIdle={commonInactive}, Ultimate={ultimateActive}/{_ultimateCardPool.Count} active, UltimateIdle={ultimateInactive}");
        }
#endif
```

Add `using System.Linq;` at the top of the file with the existing usings:

```csharp
using System.Collections.Generic;
using System.Linq;
using Common;
```

- [ ] **Step 3: Use the computed common pool capacity**

In `PreLoadCardItem()`, replace:

```csharp
            _totalPoolToLoad = GameApp.CardManager.maxHandCardCount + 8;
```

with:

```csharp
            _totalPoolToLoad = getCommonCardPoolCount();
#if UNITY_EDITOR
            Debug.Log($"[{nameof(CardPoolManager)}] 普通卡对象池预加载数量: {_totalPoolToLoad}, 手牌上限: {GameApp.CardManager.maxHandCardCount}, 行动队列上限: {GameApp.CardManager.CardActionQueue.MaxActionCount}, 临时缓冲: {COMMON_CARD_TEMP_BUFFER}");
#endif
```

In `PreLoadUltimateCardItem()`, replace:

```csharp
            int maxUltimateCount = 6;
            _totalPoolToLoad += maxUltimateCount;
            for (int i = 0; i < maxUltimateCount; i++)
```

with:

```csharp
            _totalPoolToLoad += ULTIMATE_CARD_POOL_COUNT;
            for (int i = 0; i < ULTIMATE_CARD_POOL_COUNT; i++)
```

- [ ] **Step 4: Improve exhausted-pool diagnostics**

In `GetCard(CardType type)`, replace the existing editor-only error block:

```csharp
#if UNITY_EDITOR
                Debug.LogError($"[{nameof(CardPoolManager)}] 卡牌对象池耗尽！请检查 {type} 生成配置。");
#endif
```

with:

```csharp
#if UNITY_EDITOR
                logPoolState(type, "卡牌对象池耗尽");
                Debug.LogError($"[{nameof(CardPoolManager)}] 卡牌对象池耗尽！请检查 {type} 生成配置。");
#endif
```

- [ ] **Step 5: Compile-check the changed client code**

Run Unity Editor script compilation by opening/focusing the project in Unity, or run the available project compile command if configured locally.

Expected: no C# compile errors. If a compile error says `Count` is ambiguous or `System.Linq` is missing, re-check Step 2 imports and the `logPoolState` method.

- [ ] **Step 6: Verify the pool no longer exhausts in the original scenario**

In Unity Editor, repeat Step 1.

Expected:

```text
[CardPoolManager] 普通卡对象池预加载数量: 16, 手牌上限: 8, 行动队列上限: 4, 临时缓冲: 4
```

No `卡牌对象池耗尽` error should appear while queueing 4 Attack/Buff normal cards and undoing once.

---

### Task 2: Fix Client Undo Snapshot Timing

**Files:**
- Modify: `Assets/Scripts/Module/fight/CardMgr/HandCard/HandCardOperator.cs:35-303`

- [ ] **Step 1: Record the current failing undo scenario**

In Unity Editor, reproduce the first-undo bug before editing:

1. Enter a battle.
2. Before moving or playing other cards, play one normal hand card from a non-zero index.
3. Click Undo once.

Expected before the fix: the card may return to the wrong hand position instead of the original pre-play position.

- [ ] **Step 2: Save the play snapshot before local UI mutation**

In `playCard(UI_BaseCardItem item, int index)`, replace this block:

```csharp
            var card = item.BattleCardData;
            int dataIndex = GameApp.CardManager.GetHandCards().FindIndex(c => c.InstanceId == card.InstanceId);
            int originalIndex = dataIndex >= 0 ? dataIndex : index;

            _handCardUIManager.RemoveCardAt(index);
            _uiActionStack.Push(item);
            
            var action = new CardAction
            {
                ActionType = CardActionType.PlayCard,
                cardEntity = card,
                OriginalIndex = originalIndex,
                TargetInstanceId = GameApp.CardManager.CurrentSelectedTargetId,
                Snapshot = GameApp.CardManager.TakeSnapshot()
            };
```

with:

```csharp
            var card = item.BattleCardData;
            CardSnapshot beforeSnapshot = GameApp.CardManager.TakeSnapshot();
            int dataIndex = beforeSnapshot.HandCards.FindIndex(c => c.InstanceId == card.InstanceId);
            int originalIndex = dataIndex >= 0 ? dataIndex : index;

            _handCardUIManager.RemoveCardAt(index);
            _uiActionStack.Push(item);
            
            var action = new CardAction
            {
                ActionType = CardActionType.PlayCard,
                cardEntity = card,
                OriginalIndex = originalIndex,
                TargetInstanceId = GameApp.CardManager.CurrentSelectedTargetId,
                Snapshot = beforeSnapshot
            };
```

- [ ] **Step 3: Use the drag-start snapshot for move actions**

In `onCardEndDrag(UI_BaseCardItem item, PointerEventData eventData)`, replace this block:

```csharp
                var moveAction = new CardAction
                {
                    ActionType = CardActionType.MoveCard,
                    MoveFromIndex = safeStartIndex,
                    MoveToIndex = index,
                    Snapshot = GameApp.CardManager.TakeSnapshot()
                };
```

with:

```csharp
                var moveAction = new CardAction
                {
                    ActionType = CardActionType.MoveCard,
                    MoveFromIndex = safeStartIndex,
                    MoveToIndex = index,
                    Snapshot = _tempSnapshot
                };
```

- [ ] **Step 4: Add editor-only undo diagnostics**

In `UndoLastPlayCard()`, replace this block:

```csharp
                    int restoreIndex = undoneAction.OriginalIndex;
                    if (undoneAction.Snapshot?.HandCards != null)
                    {
                        int snapshotIndex = undoneAction.Snapshot.HandCards.FindIndex(
                            c => c.InstanceId == undoItem.BattleCardData.InstanceId);
                        if (snapshotIndex >= 0)
                            restoreIndex = snapshotIndex;
                    }
                    _handCardUIManager.AnimateUndoCard(undoItem, restoreIndex);
```

with:

```csharp
                    int restoreIndex = undoneAction.OriginalIndex;
                    int snapshotIndex = -1;
                    if (undoneAction.Snapshot?.HandCards != null)
                    {
                        snapshotIndex = undoneAction.Snapshot.HandCards.FindIndex(
                            c => c.InstanceId == undoItem.BattleCardData.InstanceId);
                        if (snapshotIndex >= 0)
                            restoreIndex = snapshotIndex;
                    }
#if UNITY_EDITOR
                    Debug.Log($"[{nameof(HandCardOperator)}] 撤销出牌 InstanceId={undoItem.BattleCardData.InstanceId}, OriginalIndex={undoneAction.OriginalIndex}, SnapshotIndex={snapshotIndex}, RestoreIndex={restoreIndex}");
#endif
                    _handCardUIManager.AnimateUndoCard(undoItem, restoreIndex);
```

- [ ] **Step 5: Compile-check the changed client code**

Run Unity Editor script compilation by opening/focusing the project in Unity, or run the available project compile command if configured locally.

Expected: no C# compile errors. If `CardSnapshot` is not resolved, confirm `HandCardOperator.cs` still has `using Module.fight.CardMgr;` through its namespace and uses the existing `CardSnapshot` type from `CardActionQueue.cs`.

- [ ] **Step 6: Verify first undo returns to the original position**

In Unity Editor, repeat Step 1.

Expected: Console logs a line like:

```text
[HandCardOperator] 撤销出牌 InstanceId=1003, OriginalIndex=3, SnapshotIndex=3, RestoreIndex=3
```

The card should return to the same index it had before the first play action.

---

### Task 3: Mark Server Undo Snapshot Sync As Undo UI Refresh

**Files:**
- Modify: `Assets/Scripts/Module/fight/Network/BattleNetworkController.cs:155-160`
- Modify: `Assets/Scripts/Module/fight/FightController.cs:113-148`

- [ ] **Step 1: Record current server undo sync behavior**

Before editing, perform a play action then undo with the server connected.

Expected before the fix: the server `StateSnapshot` refreshes hand data, but `FightController.restoreFromStateSnapshot()` posts `UpdateHandCards` without the undo flag:

```csharp
GameApp.MessageCenter.PostEvent(EventDefines.UpdateHandCards, deck.HandCards);
```

That means `FightingView.onUpdateHandCards()` sees `isUndo == false`.

- [ ] **Step 2: Broadcast undo responses with an undo marker**

In `BattleNetworkController.onUndoResponse(MainPack pack)`, replace:

```csharp
            broadcastEvents(pack);
```

with:

```csharp
            if (pack.BattlePack != null)
                GameApp.MessageCenter.PostEvent(EventDefines.OnBattleServerResponse, new object[] { pack.BattlePack, true });
```

- [ ] **Step 3: Read the undo marker in FightController**

In `FightController.onBattleServerResponse(object args)`, replace the method body:

```csharp
            var battlePack = args as BattlePack;
            if (battlePack == null) return;
            
            if (battlePack.StateSnapshot != null)
                restoreFromStateSnapshot(battlePack.StateSnapshot);
            
            PlayEventSequence.Play(battlePack.Events);
```

with:

```csharp
            bool isUndo = false;
            BattlePack battlePack = null;
            if (args is object[] arr)
            {
                battlePack = arr.Length > 0 ? arr[0] as BattlePack : null;
                isUndo = arr.Length > 1 && arr[1] is true;
            }
            else
            {
                battlePack = args as BattlePack;
            }
            if (battlePack == null) return;
            
            if (battlePack.StateSnapshot != null)
                restoreFromStateSnapshot(battlePack.StateSnapshot, isUndo);
            
            PlayEventSequence.Play(battlePack.Events);
```

- [ ] **Step 4: Pass the undo flag into hand refresh**

Change the method signature in `FightController` from:

```csharp
        private void restoreFromStateSnapshot(BattleStateSnapshot snapshot)
```

to:

```csharp
        private void restoreFromStateSnapshot(BattleStateSnapshot snapshot, bool isUndo)
```

Then replace the final hand update line in that method:

```csharp
            GameApp.MessageCenter.PostEvent(EventDefines.UpdateHandCards, deck.HandCards);
```

with:

```csharp
            GameApp.MessageCenter.PostEvent(EventDefines.UpdateHandCards, new object[] { deck.HandCards, isUndo });
```

This uses the existing `FightingView` handler path:

```csharp
m_onUpdateHandCardsHandler = args => onUpdateHandCards(new object[] { args });
```

and existing undo parsing:

```csharp
bool isUndo = args.Length > 1 && args[1] is true;
```

- [ ] **Step 5: Compile-check the changed client code**

Run Unity Editor script compilation by opening/focusing the project in Unity, or run the available project compile command if configured locally.

Expected: no C# compile errors. If hand refresh does not update, inspect the runtime argument shape in `FightingView.onUpdateHandCards(params object[] args)` and ensure the posted argument is exactly `new object[] { deck.HandCards, isUndo }`.

- [ ] **Step 6: Verify undo server sync does not replay draw-style stagger**

In Unity Editor with the server connected:

1. Enter battle.
2. Play a card.
3. Undo.
4. Watch hand cards during the server snapshot refresh.

Expected: the local card returns immediately to the restored hand order, and the server snapshot refresh does not stagger it like a newly drawn card.

---

### Task 4: Final Verification

**Files:**
- Verify: `Assets/Scripts/Module/fight/CardMgr/CardPoolManager.cs`
- Verify: `Assets/Scripts/Module/fight/CardMgr/HandCard/HandCardOperator.cs`
- Verify: `Assets/Scripts/Module/fight/Network/BattleNetworkController.cs`
- Verify: `Assets/Scripts/Module/fight/FightController.cs`

- [ ] **Step 1: Run script compilation**

Run Unity Editor script compilation by opening/focusing the project in Unity, or run the available project compile command if configured locally.

Expected: no compile errors and no release-build logging changes because new diagnostics are under `#if UNITY_EDITOR`.

- [ ] **Step 2: Verify object-pool diagnostics and no exhaustion**

In Unity Editor:

1. Enter battle.
2. Queue 4 Attack/Buff normal cards.
3. Undo one queued card.
4. Queue another Attack/Buff normal card.

Expected:

```text
[CardPoolManager] 普通卡对象池预加载数量: 16, 手牌上限: 8, 行动队列上限: 4, 临时缓冲: 4
```

No `卡牌对象池耗尽` error appears.

- [ ] **Step 3: Verify first undo position**

In Unity Editor:

1. Enter battle.
2. Play the card at index 2 or higher.
3. Click Undo once.

Expected: the card returns to the exact original index. The diagnostic log should show matching `SnapshotIndex` and `RestoreIndex`.

- [ ] **Step 4: Verify move undo position**

In Unity Editor:

1. Enter battle.
2. Drag a normal card from index 1 to index 3.
3. Click Undo once.

Expected: after server snapshot sync, the hand order returns to the pre-drag order without draw-style stagger.

- [ ] **Step 5: Verify normal battle flow still works**

In Unity Editor:

1. Enter battle.
2. Queue cards until the queue is full.
3. Let the turn resolve.
4. Wait for the next player turn.

Expected: queued cards execute, return to the pool after output, new hand cards display, and no new Console errors appear.

---

## Self-Review

- Spec coverage: Task 1 covers common pool capacity and diagnostics; Task 2 covers local pre-action snapshots and undo diagnostics; Task 3 covers server undo snapshot refresh as undo UI update; Task 4 covers compile and manual regression verification.
- Placeholder scan: no placeholder implementation steps remain; each code change includes exact replacement blocks.
- Type consistency: `CardSnapshot`, `CardAction`, `BattlePack`, `BattleStateSnapshot`, `EventDefines.UpdateHandCards`, and `EventDefines.OnBattleServerResponse` match existing code names observed in the repository.
