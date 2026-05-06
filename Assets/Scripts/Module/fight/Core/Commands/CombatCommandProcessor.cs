/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗命令处理器                      
* │  类    名: CombatCommandProcessor.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Module.fight.CardMgr;

namespace Module.fight.Core.Commands
{
    public class CombatCommandProcessor
    {
        private readonly Stack<BaseCommand> _history = new();
        private readonly CardManager _cardManager;

        public bool Execute(BaseCommand command)
        {
            command.BeforeSnapshot = _cardManager.TakeSnapshot();

            bool success = command.Execute(_cardManager.BattleContext);
            if(success) _history.Push(command);
            
            return success;
        }

        public void Undo()
        {
            if(_history.Count == 0) return;
            var cmd = _history.Pop();
            cmd.Undo(_cardManager.BattleContext);
        }
        
        /*
         private readonly CardManager _cardManager;
          private readonly Stack<BaseCommand> _history;
          private readonly int _maxActionCount;

          public int ActionCount => _history.Count;
          public bool CanExecute => _history.Count < _maxActionCount;

          public CombatCommandProcessor(CardManager cardManager, int maxActionCount = 4)
          {
              _cardManager = cardManager;
              _history = new Stack<BaseCommand>();
              _maxActionCount = maxActionCount;
          }

          public bool Execute(BaseCommand command)
          {
              if (!CanExecute) return false;

              // 执行前拍快照
              command.BeforeSnapshot = _cardManager.TakeSnapshot();

              bool success = command.Execute(_cardManager.BattleContext);
              if (success)
              {
                  _history.Push(command);
              }

              return success;
          }

          public void Undo()
          {
              if (_history.Count == 0) return;

              var command = _history.Pop();

              // 优先用快照恢复（最保险）
              if (command.BeforeSnapshot != null)
              {
                  _cardManager.RestoreSnapshot(command.BeforeSnapshot);
              }
              else
              {
                  command.Undo(_cardManager.BattleContext);
              }
          }

          public List<BaseCommand> GetHistoryAndClear()
          {
              var list = new List<BaseCommand>(_history);
              list.Reverse();
              _history.Clear();
              return list;
          }

          public void Clear()
          {
              _history.Clear();
          }
         */
    }
}