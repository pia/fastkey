using System;
using WindowSnapHotkeys;

internal static class HotkeyStateMachineTests
{
    private static int _failures;

    private static int Main()
    {
        Run("queues left snap on first Alt+A keydown", QueuesLeftSnapOnFirstAltAKeydown);
        Run("does not queue duplicate snap before keyup", DoesNotQueueDuplicateSnapBeforeKeyUp);
        Run("queues both sides in order after releasing keys", QueuesBothSidesInOrderAfterReleasingKeys);
        Run("alt keyup clears handled state without consuming the event", AltKeyUpClearsHandledStateWithoutConsuming);

        if (_failures == 0)
        {
            Console.WriteLine("PASS");
            return 0;
        }

        Console.WriteLine("FAILURES: " + _failures);
        return 1;
    }

    private static void QueuesLeftSnapOnFirstAltAKeydown()
    {
        var stateMachine = new HotkeyStateMachine();

        var result = stateMachine.ProcessKeyEvent(isKeyDown: true, isKeyUp: false, vkCode: 0x41, isAltPressed: true);

        AssertTrue(result.ShouldConsumeEvent, "Alt+A keydown should be consumed.");
        AssertTrue(result.TriggeredSide.HasValue, "Alt+A keydown should produce a snap action.");
        AssertEqual(SnapSide.Left, result.TriggeredSide.Value, "Expected the triggered action to be Left.");
    }

    private static void DoesNotQueueDuplicateSnapBeforeKeyUp()
    {
        var stateMachine = new HotkeyStateMachine();

        var first = stateMachine.ProcessKeyEvent(isKeyDown: true, isKeyUp: false, vkCode: 0x41, isAltPressed: true);
        var second = stateMachine.ProcessKeyEvent(isKeyDown: true, isKeyUp: false, vkCode: 0x41, isAltPressed: true);

        AssertTrue(first.TriggeredSide.HasValue, "First Alt+A keydown should produce an action.");
        AssertTrue(second.ShouldConsumeEvent, "Repeated Alt+A keydown should still be consumed.");
        AssertFalse(second.TriggeredSide.HasValue, "Repeated Alt+A keydown should not produce another action.");
    }

    private static void QueuesBothSidesInOrderAfterReleasingKeys()
    {
        var stateMachine = new HotkeyStateMachine();

        var left = stateMachine.ProcessKeyEvent(isKeyDown: true, isKeyUp: false, vkCode: 0x41, isAltPressed: true);
        stateMachine.ProcessKeyEvent(isKeyDown: false, isKeyUp: true, vkCode: 0x41, isAltPressed: false);
        var right = stateMachine.ProcessKeyEvent(isKeyDown: true, isKeyUp: false, vkCode: 0x44, isAltPressed: true);

        AssertTrue(left.TriggeredSide.HasValue, "Alt+A should produce a left snap.");
        AssertTrue(right.TriggeredSide.HasValue, "Alt+D after releasing A should produce a right snap.");
        AssertEqual(SnapSide.Left, left.TriggeredSide.Value, "Expected the first triggered action to be Left.");
        AssertEqual(SnapSide.Right, right.TriggeredSide.Value, "Expected the second triggered action to be Right.");
    }

    private static void AltKeyUpClearsHandledStateWithoutConsuming()
    {
        var stateMachine = new HotkeyStateMachine();

        stateMachine.ProcessKeyEvent(isKeyDown: true, isKeyUp: false, vkCode: 0x41, isAltPressed: true);
        var altUp = stateMachine.ProcessKeyEvent(isKeyDown: false, isKeyUp: true, vkCode: 0x12, isAltPressed: false);
        var next = stateMachine.ProcessKeyEvent(isKeyDown: true, isKeyUp: false, vkCode: 0x41, isAltPressed: true);

        AssertFalse(altUp.ShouldConsumeEvent, "Alt keyup itself should not be consumed.");
        AssertFalse(altUp.TriggeredSide.HasValue, "Alt keyup should not produce a snap.");
        AssertTrue(next.TriggeredSide.HasValue, "Alt keyup should clear handled state so Alt+A can queue again.");
    }

    private static void Run(string name, Action test)
    {
        try
        {
            test();
            Console.WriteLine("PASS " + name);
        }
        catch (Exception ex)
        {
            _failures++;
            Console.WriteLine("FAIL " + name + ": " + ex.Message);
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertFalse(bool condition, string message)
    {
        if (condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!Equals(expected, actual))
        {
            throw new InvalidOperationException(message + " Expected: " + expected + ", Actual: " + actual);
        }
    }
}
