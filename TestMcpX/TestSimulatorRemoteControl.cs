using McpXLib;
using McpXLib.Enums;

namespace TestMcpX;

/// <summary>
/// リモート操作（RUN / STOP / PAUSE / ラッチクリア / RESET）の結合テストの共通実装。
/// </summary>
/// <remarks>
/// PLCの動作状態を変更し、テスト後は RUN に戻します。ラッチクリアにより、PLCのラッチデバイスがクリアされます。<br/>
/// RESET はPLCとの接続が切断されるため、環境変数 <see cref="ResetEnableVar"/> に 1 を指定した場合のみ実行します。
/// </remarks>
public abstract class RemoteControlTestBase
{
    // SD203 の b0〜b3: CPUの動作状態（0: RUN、2: STOP、3: PAUSE）
    private const int Run = 0;
    private const int Stop = 2;
    private const int Pause = 3;

    /// <summary>
    /// 接続先PLCへ接続する。実行条件を満たさない場合は <see cref="Assert.Inconclusive(string)"/> を呼ぶ。
    /// </summary>
    protected abstract McpX Connect();

    /// <summary>
    /// RESET のテストを有効にする環境変数名。
    /// </summary>
    protected abstract string ResetEnableVar { get; }

    private static int GetState(McpX mcpx) => mcpx.Read<ushort>(Prefix.SD, "203") & 0x000F;

    [TestMethod]
    public async Task TestRunStopPause()
    {
        using var mcpx = Connect();
        try
        {
            mcpx.RemoteStop();
            Assert.AreEqual(Stop, GetState(mcpx));

            mcpx.RemoteRun();
            Assert.AreEqual(Run, GetState(mcpx));

            await mcpx.RemotePauseAsync();
            Assert.AreEqual(Pause, GetState(mcpx));

            await mcpx.RemoteRunAsync(force: true);
            Assert.AreEqual(Run, GetState(mcpx));

            await mcpx.RemoteStopAsync();
            Assert.AreEqual(Stop, GetState(mcpx));

            mcpx.RemotePause(force: true);  // STOP 中の PAUSE は正常完了するが PAUSE にはならない
            mcpx.RemoteRun(clearMode: RemoteRunClearMode.None);
            Assert.AreEqual(Run, GetState(mcpx));
        }
        finally
        {
            mcpx.RemoteRun(force: true);
        }
    }

    [TestMethod]
    public async Task TestLatchClear()
    {
        using var mcpx = Connect();
        try
        {
            mcpx.RemoteStop();
            mcpx.Write(Prefix.L, "0", true);
            Assert.IsTrue(mcpx.Read<bool>(Prefix.L, "0"));

            mcpx.RemoteLatchClear();
            Assert.IsFalse(mcpx.Read<bool>(Prefix.L, "0"));

            mcpx.Write(Prefix.L, "1", true);
            await mcpx.RemoteLatchClearAsync();
            Assert.IsFalse(mcpx.Read<bool>(Prefix.L, "1"));
        }
        finally
        {
            mcpx.RemoteRun(force: true);
        }
    }

    [TestMethod]
    public async Task TestReset()
    {
        if (Environment.GetEnvironmentVariable(ResetEnableVar) != "1")
        {
            Assert.Inconclusive($"RESET のテストは {ResetEnableVar}=1 のときのみ実行します。");
        }

        using var mcpx = Connect();

        try
        {
            // リセットにより接続が切断されても例外にならず、同じインスタンスで続けて読み書きできること
            mcpx.RemoteStop();
            mcpx.RemoteReset();
            Assert.AreEqual(Run, GetState(mcpx));   // スイッチの状態（RUN）に戻る
            mcpx.Write(Prefix.D, "9000", (short)4321);
            Assert.AreEqual((short)4321, mcpx.Read<short>(Prefix.D, "9000"));

            await mcpx.RemoteStopAsync();
            await mcpx.RemoteResetAsync();
            Assert.AreEqual(Run, GetState(mcpx));
        }
        finally
        {
            // RESET が拒否された場合（リモートリセットを許可していない実機など）も STOP のまま残さない
            mcpx.RemoteRun(force: true);
        }
    }
}

/// <summary>
/// GX Simulator3 のリモート操作の結合テスト。
/// </summary>
/// <remarks>
/// RESET は環境変数 <c>MCPX_SIM_RESET=1</c> を指定した場合のみ実行します。<br/>
/// 実行条件・接続先は <see cref="SimulatorConnection"/> を参照してください。
/// </remarks>
[TestClass]
[TestCategory("SimulatorPlc")]
public sealed class TestSimulatorRemoteControl : RemoteControlTestBase
{
    protected override McpX Connect() => SimulatorConnection.Connect();

    protected override string ResetEnableVar => "MCPX_SIM_RESET";
}
