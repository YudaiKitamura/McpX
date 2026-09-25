namespace McpXLib.Utils;

internal static class TaskExtensions
{
    /// <summary>
    /// 待機を打ち切ったタスクの例外を観測済みにし、UnobservedTaskException の発生を防ぐ。
    /// </summary>
    internal static void ObserveException(this Task task)
    {
        _ = task.ContinueWith(
            t => _ = t.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }
}
