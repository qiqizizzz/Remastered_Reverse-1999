/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗快照恢复与模式切换回归测试
* │  类    名: FightControllerBattleResetTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.fight.CardMgr;
using NUnit.Framework;

namespace Tests.Fight
{
    public class FightControllerBattleResetTests
    {
        [Test]
        public void DefaultActionQueue_ShouldBeFour()
        {
            var queue = new CardActionQueue();

            Assert.That(queue.MaxActionCount, Is.EqualTo(4));
        }

        [Test]
        public void Clear_ShouldRemoveQueuedCardsWithoutChangingDefaultLimit()
        {
            var queue = new CardActionQueue { MaxActionCount = 6 };
            queue.PushAction(new CardAction());
            queue.Clear();

            Assert.That(queue.Count, Is.EqualTo(0));
            Assert.That(queue.MaxActionCount, Is.EqualTo(6));
        }

        [Test]
        public void CanPlayCard_WhenFourActionsQueued_ShouldReturnFalse()
        {
            var queue = new CardActionQueue { MaxActionCount = 4 };
            for (int i = 0; i < 4; i++)
                queue.PushAction(new CardAction());

            Assert.That(queue.CanPlayCard(), Is.False);
        }
    }
}
