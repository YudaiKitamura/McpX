using McpXLib.Enums;
using McpXLib.Exceptions;

namespace McpXLib;

/// <summary>
/// モニタセッション。<see cref="McpX.MonitorRegist(Action{MonitorBuilder})"/> で登録したデバイスを、
/// <see cref="Read"/> / <see cref="ReadAsync"/> により何度でも読み出せます。
/// </summary>
/// <remarks>
/// モニタ(コマンド: 0802)は登録済みのデバイスを読み出すだけの軽量なコマンドのため、
/// 登録は1回だけ行い、本セッションで繰り返し読み出すのが効率的です。<br/>
/// PLCの再立上げなどで登録内容が消去された場合は、再度 <see cref="McpX.MonitorRegist(Action{MonitorBuilder})"/> で登録し直してください。
/// </remarks>
public sealed class MonitorSession
{
    private readonly McpX mcpX;
    private readonly (Prefix prefix, string address)[] wordAddresses;
    private readonly (Prefix prefix, string address)[] doubleWordAddresses;
    private readonly Action<ushort>[] wordApplies;
    private readonly Action<uint>[] doubleWordApplies;

    // このセッションを作ったモニタ登録の世代。別の登録が行われると無効になる。
    private readonly int generation;

    internal MonitorSession(McpX mcpX, MonitorBuilder builder, int generation)
    {
        this.mcpX = mcpX;
        this.generation = generation;
        wordAddresses = builder.wordEntries.Select(e => (e.prefix, e.address)).ToArray();
        doubleWordAddresses = builder.doubleWordEntries.Select(e => (e.prefix, e.address)).ToArray();
        wordApplies = builder.wordEntries.Select(e => e.apply).ToArray();
        doubleWordApplies = builder.doubleWordEntries.Select(e => e.apply).ToArray();
    }

    /// <summary>
    /// 登録済みのデバイスの値をPLCから読み出し(コマンド: 0802)、各コールバックへ変換済みの値を渡します。
    /// </summary>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="InvalidOperationException">このセッションの作成後に別のモニタ登録（MonitorRegist）が行われ、PLC の登録が置き換わっている場合にスローします。</exception>
    public void Read()
    {
        var (rawWords, rawDoubleWords) = mcpX.MonitorRegistered(generation, wordAddresses, doubleWordAddresses);
        Dispatch(rawWords, rawDoubleWords);
    }

    /// <summary>
    /// 登録済みのデバイスの値を非同期でPLCから読み出し(コマンド: 0802)、各コールバックへ変換済みの値を渡します。
    /// </summary>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <exception cref="InvalidOperationException">このセッションの作成後に別のモニタ登録（MonitorRegist）が行われ、PLC の登録が置き換わっている場合にスローします。</exception>
    public async Task ReadAsync()
    {
        var (rawWords, rawDoubleWords) = await mcpX.MonitorRegisteredAsync(generation, wordAddresses, doubleWordAddresses);
        Dispatch(rawWords, rawDoubleWords);
    }

    private void Dispatch(ushort[] rawWords, uint[] rawDoubleWords)
    {
        for (int i = 0; i < wordApplies.Length; i++)
        {
            wordApplies[i](rawWords[i]);
        }

        for (int j = 0; j < doubleWordApplies.Length; j++)
        {
            doubleWordApplies[j](rawDoubleWords[j]);
        }
    }
}
