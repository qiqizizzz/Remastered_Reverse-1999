/*
* ┌──────────────────────────────────┐
* │  描    述: CardActionQueue 单元测试
* │  类    名: CardActionQueueTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.fight.CardMgr;
using NUnit.Framework;

public class CardActionQueueTests
{
    [Test]
    public void PushAction_ShouldIncreaseCount()
    {
        var queue = new CardActionQueue();
        queue.PushAction(new CardAction { ActionType = CardActionType.PlayCard });

        Assert.That(queue.Count, Is.EqualTo(1));
    }

    [Test]
    public void PushAction_WhenFull_ShouldReturnTrue()
    {
        var queue = new CardActionQueue { MaxActionCount = 2 };
        queue.PushAction(new CardAction { ActionType = CardActionType.PlayCard });
        bool isFull = queue.PushAction(new CardAction { ActionType = CardActionType.PlayCard });

        Assert.That(isFull, Is.True);
    }

    [Test]
    public void PushAction_WhenNotFull_ShouldReturnFalse()
    {
        var queue = new CardActionQueue { MaxActionCount = 3 };

        bool isFull = queue.PushAction(new CardAction { ActionType = CardActionType.PlayCard });

        Assert.That(isFull, Is.False);
    }

    [Test]
    public void CanPlayCard_WhenNotFull_ShouldReturnTrue()
    {
        var queue = new CardActionQueue { MaxActionCount = 4 };

        Assert.That(queue.CanPlayCard(), Is.True);
    }

    [Test]
    public void CanPlayCard_WhenFull_ShouldReturnFalse()
    {
        var queue = new CardActionQueue { MaxActionCount = 1 };
        queue.PushAction(new CardAction { ActionType = CardActionType.PlayCard });

        Assert.That(queue.CanPlayCard(), Is.False);
    }

    [Test]
    public void UndoLastAction_WhenEmpty_ShouldReturnNull()
    {
        var queue = new CardActionQueue();

        var undone = queue.UndoLastAction();

        Assert.That(undone, Is.Null);
        Assert.That(queue.Count, Is.EqualTo(0));
    }

    [Test]
    public void UndoLastAction_ShouldPopAndReturnLastAction()
    {
        var queue = new CardActionQueue();
        var action1 = new CardAction { ActionType = CardActionType.PlayCard };
        var action2 = new CardAction { ActionType = CardActionType.MoveCard };
        queue.PushAction(action1);
        queue.PushAction(action2);

        var undone = queue.UndoLastAction();

        Assert.That(undone, Is.SameAs(action2));
        Assert.That(queue.Count, Is.EqualTo(1));
    }

    [Test]
    public void UndoThenCanPlayCard_ShouldReturnTrueAgain()
    {
        var queue = new CardActionQueue { MaxActionCount = 1 };
        queue.PushAction(new CardAction { ActionType = CardActionType.PlayCard });

        queue.UndoLastAction();

        Assert.That(queue.CanPlayCard(), Is.True);
    }

    [Test]
    public void Clear_ShouldResetCount()
    {
        var queue = new CardActionQueue();
        queue.PushAction(new CardAction());
        queue.PushAction(new CardAction());

        queue.Clear();

        Assert.That(queue.Count, Is.EqualTo(0));
    }

    [Test]
    public void GetAction_ShouldReturnReversedOrder()
    {
        var queue = new CardActionQueue();
        var first = new CardAction { ActionType = CardActionType.PlayCard };
        var second = new CardAction { ActionType = CardActionType.MoveCard };
        queue.PushAction(first);
        queue.PushAction(second);

        var actions = queue.GetAction();

        Assert.That(actions.Length, Is.EqualTo(2));
        Assert.That(actions[0], Is.SameAs(first));
        Assert.That(actions[1], Is.SameAs(second));
    }

    [Test]
    public void GetAllActionsAndClear_ShouldClearAfterReturn()
    {
        var queue = new CardActionQueue();
        queue.PushAction(new CardAction());
        queue.PushAction(new CardAction());

        var actions = queue.GetAllActionsAndClear();

        Assert.That(actions.Count, Is.EqualTo(2));
        Assert.That(queue.Count, Is.EqualTo(0));
    }

    [Test]
    public void DefaultMaxActionCount_ShouldBeFour()
    {
        var queue = new CardActionQueue();

        Assert.That(queue.MaxActionCount, Is.EqualTo(4));
    }
}
