# Hit VFX Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Clean up obsolete client execution code, add `sourceCardConfigId` to the protocol, and play the target VFX configured in `CardDataSO` during damage/heal events.

**Architecture:** We will modify `GamesProtocol.proto` to include `sourceCardConfigId` in `BattleEvent`. The client's `PlayEventSequence.cs` will read this ID, lookup the `CardDataSO`, and instantiate `CardDataSO.CardEffectPrefab` at the target entity's position when it receives a `DamageTaken` (or `HealTaken`) event. We will also remove obsolete client files like `CardSkillExecutor.cs`.

**Tech Stack:** Unity C#, Protobuf.

---

### Task 1: Update Protobuf Protocol

**Files:**
- Modify: `Assets/Scripts/[test]/GamesProtocol.proto`
- Note: We will use a script to regenerate the C# files or you can ask the user to regenerate if there's no script, but we can just manually update the C# file `Assets/Scripts/Network/Protocol/GamesProtocol.cs` and the server's equivalent since Protobuf in Unity sometimes requires an external compiler. Let's just modify the `.proto` and the generated C# files directly to match it for safety. Wait, modifying generated C# directly is safer if we don't have the `protoc` compiler in the environment.

**Task Steps:**

- [ ] **Step 1: Add `sourceCardConfigId` to `GamesProtocol.proto`**
Modify `Assets/Scripts/[test]/GamesProtocol.proto`. Inside `message BattleEvent`, add `int32 sourceCardConfigId = 4;`.

- [ ] **Step 2: Update Client's generated `GamesProtocol.cs`**
Modify `Assets/Scripts/Network/Protocol/GamesProtocol.cs`. Find `class BattleEvent` and add `public int SourceCardConfigId { get; set; }` and modify `WriteTo` and `CalculateSize` and `MergeFrom` to include the new integer field (tag 4).

- [ ] **Step 3: Update Server's generated `GamesProtocol.cs`**
Modify `AAAGameServer/ConsoleMock_Server/GameServer/Network/Protobuf/GamesProtocol.cs` (or similar path if it's there). Mirror the exact same changes to `BattleEvent` class (add `SourceCardConfigId`).

- [ ] **Step 4: Commit**
```bash
git add "Assets/Scripts/[test]/GamesProtocol.proto" "Assets/Scripts/Network/Protocol/GamesProtocol.cs" "AAAGameServer/ConsoleMock_Server/GameServer/Network/Protobuf/GamesProtocol.cs"
git commit -m "feat: add sourceCardConfigId to BattleEvent protocol"
```

### Task 2: Server Implementation

**Files:**
- Modify: Server's battle logic where `DamageTaken` events are created. (Likely in `AAAGameServer/ConsoleMock_Server/GameServer/Combat/SkillExecutor.cs` or similar - the agent must locate this file first using glob/grep).

- [ ] **Step 1: Locate the Server's Event Generation Code**
Find where `BattleEvent` objects are created on the server (especially for `DamageTaken`). Use `grep "BattleEventType.DamageTaken" AAAGameServer/ConsoleMock_Server/GameServer`.

- [ ] **Step 2: Inject `SourceCardConfigId` into the Event**
Update the server code so that when creating a `BattleEvent` for a skill execution, it sets `SourceCardConfigId = skillConfigId` (or the card's config ID).

- [ ] **Step 3: Commit**
```bash
git add AAAGameServer/
git commit -m "feat: server populates SourceCardConfigId for BattleEvents"
```

### Task 3: Client Obsolete Code Cleanup

**Files:**
- Delete: `Assets/Scripts/Module/fight/Skill/CardSkillExecutor.cs`
- Delete: `Assets/Scripts/Module/fight/Core/Commands/CombatCommandProcessor.cs` (and other unused commands if verified they are totally unused).

- [ ] **Step 1: Delete `CardSkillExecutor.cs`**
Remove `Assets/Scripts/Module/fight/Skill/CardSkillExecutor.cs` using `rm`.

- [ ] **Step 2: Verify and Delete Unused `Commands`**
Verify that `CombatCommandProcessor.cs` and other files in `Assets/Scripts/Module/fight/Core/Commands` are unreferenced in the client. If so, delete the `Commands` folder.

- [ ] **Step 3: Commit**
```bash
git add -A Assets/Scripts/Module/fight
git commit -m "refactor: remove obsolete CardSkillExecutor and Core/Commands"
```

### Task 4: Client Hit VFX Implementation

**Files:**
- Modify: `Assets/Scripts/Module/fight/PlayEventSequence.cs`

- [ ] **Step 1: Implement `PlayVfx` helper method**
In `PlayEventSequence.cs`, add a helper method to instantiate the VFX:
```csharp
private static void tryPlayCardVfx(int configId, BaseCharacter target)
{
    if (configId <= 0 || target == null) return;
    
    // 获取 CardDataSO
    var cardData = GameApp.ConfigManager.Card.GetCardById(configId);
    if (cardData != null && cardData.CardEffectPrefab != null)
    {
        // 实例化特效到目标位置
        GameObject vfx = UnityEngine.Object.Instantiate(cardData.CardEffectPrefab, target.transform.position, Quaternion.identity);
        // 可选：添加自动销毁逻辑，如果预制体自带可以不加，这里为了安全可以加一个定时销毁
        UnityEngine.Object.Destroy(vfx, 3f); 
    }
}
```

- [ ] **Step 2: Trigger VFX in `playDamageTaken` and `playDamageTakenBatch`**
In `PlayEventSequence.cs`:
Update `playDamageTaken(BattleEvent evt)` to call `tryPlayCardVfx(evt.SourceCardConfigId, target);` right after `target.TakeDamage(...)`.
Update `playDamageTakenBatch` to loop through events and call `tryPlayCardVfx(evt.SourceCardConfigId, target);`.

- [ ] **Step 3: Trigger VFX in `playHealTaken`**
Update `playHealTaken(BattleEvent evt)` similarly, calling `tryPlayCardVfx(evt.SourceCardConfigId, target);` right after the target's HP is updated.

- [ ] **Step 4: Commit**
```bash
git add Assets/Scripts/Module/fight/PlayEventSequence.cs
git commit -m "feat: client plays CardEffectPrefab based on SourceCardConfigId"
```