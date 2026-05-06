/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗命令处理器                      
* │  类    名: CombatCommandProcessor.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Module.fight.CardMgr;
using Module.fight.Core.Entities;

namespace Module.fight.Core.Commands
{
    public class CombatCommandProcessor
    {
        private readonly CombatContext _ctx;
        private readonly Stack<BaseCommand> _history;
        private readonly int _maxActionCount;
        
        private readonly Func<CardSnapshot> _takeSnapshot;
        private readonly Action<CardSnapshot> _restoreSnapshot;

        public int ActionCount => _history.Count;
        public bool CanExecute => _history.Count < _maxActionCount;

        public CombatCommandProcessor(
            CombatContext ctx,
            Func<CardSnapshot> takeSnapshot,
            Action<CardSnapshot> restoreSnapshot,
            int maxActionCount = 4)
        {
            _ctx = ctx;
            _takeSnapshot = takeSnapshot;
            _restoreSnapshot = restoreSnapshot;
            _history = new Stack<BaseCommand>();
            _maxActionCount = maxActionCount;
        }

        public bool Execute(BaseCommand command)
        {
            if (!CanExecute)
                return false;

            command.BeforeSnapshot = _takeSnapshot();
            bool success = command.Execute(_ctx);

            if (success)
                _history.Push(command);

            return success;
        }

        public void Undo()
        {
            if (_history.Count == 0)
                return;

            var command = _history.Pop();

            if (command.BeforeSnapshot != null)
                _restoreSnapshot(command.BeforeSnapshot);
            else
                command.Undo(_ctx);
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
    }
}