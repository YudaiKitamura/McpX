using System.Runtime.CompilerServices;
using McpXLib.Utils;

namespace TestMcpX;

[TestClass]
public sealed class TestTaskExtensions
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateFaultedTask(string message, bool observe)
    {
        var tcs = new TaskCompletionSource<bool>();
        if (observe)
        {
            tcs.Task.ObserveException();
        }
        tcs.SetException(new InvalidOperationException(message));
    }

    private static bool IsUnobservedRaised(bool observe)
    {
        var message = Guid.NewGuid().ToString();
        var raised = false;

        EventHandler<UnobservedTaskExceptionEventArgs> handler = (s, e) =>
        {
            if (e.Exception.InnerExceptions.Any(ex => ex.Message == message))
            {
                raised = true;
                e.SetObserved();
            }
        };

        TaskScheduler.UnobservedTaskException += handler;
        try
        {
            CreateFaultedTask(message, observe);

            for (var i = 0; i < 5; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= handler;
        }

        return raised;
    }

    [TestMethod]
    public void TestObserveException()
    {
        Assert.IsFalse(IsUnobservedRaised(observe: true));
    }

    [TestMethod]
    public void TestWithoutObserveExceptionRaisesEvent()
    {
        // 検証方法そのものが有効であることを確認する
        Assert.IsTrue(IsUnobservedRaised(observe: false));
    }
}
