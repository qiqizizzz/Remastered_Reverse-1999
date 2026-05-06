/*
* ┌──────────────────────────────────┐
* │  描    述: 命令执行上下文,封装命令执行所需的全部核心服务
* │  类    名: CommandExecutionContext.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.fight.Core.Entities;
using Module.fight.Core.EventBus;
using Module.fight.Core.Systems;

namespace Module.fight.Core.Commands
{
    public class CommandExecutionContext
    {
        public CombatContext CombatContext { get; }
        public CardCombatSystem CombatSystem { get; }
        public CombatEventBus EventBus { get; }

        public CommandExecutionContext(CombatContext combatContext, CardCombatSystem combatSystem,
            CombatEventBus eventBus)
        {
            CombatContext = combatContext;
            CombatSystem = combatSystem;
            EventBus = eventBus;
        }
        
    }
}