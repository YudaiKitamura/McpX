using McpXLib.Abstructs;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Helpers;
using McpXLib.Utils;

namespace McpXLib.Commands;

public sealed class WordBatchWriteAsciiCommand<T> : BaseAsciiCommand, IPlcCommand<bool>
    where T : unmanaged
{
    public const ushort MIN_WORD_LENGTH = 1;
    public const ushort MAX_WORD_LENGTH = 960;
    private readonly ushort wordLength;

    public WordBatchWriteAsciiCommand(Prefix prefix, string address, T[] values) : base()
    {
        wordLength = (ushort)(values.Length * DeviceConverter.GetWordLength<T>());

        ValidatePramater();

        var hexValueStr = string.Concat(
            DeviceConverter.ReverseByTwoBytes(
                DeviceConverter.ConvertByteValueArray(values)
            ).Select(x => x.ToString("X2"))
        );

        content = new ContentAsciiPacketHelper(
            command: "1401",
            subCommand: "0000",
            commandContent: $"{ DeviceConverter.ToASCIIAddress(prefix, address) }{ wordLength.ToString("X").PadLeft(4, '0') }{ hexValueStr }",
            monitoringTimer: "0000"
        );
    }

    public void ValidatePramater()
    {
        if (wordLength < MIN_WORD_LENGTH || wordLength > MAX_WORD_LENGTH)
        {
            throw new ArgumentException($"Word length can be from {MIN_WORD_LENGTH} to {MAX_WORD_LENGTH}.");
        }
    }

    public async Task<bool> ExecuteAsync(IPlc plc)
    {
        Route = plc.Route;
        return new ResponseAsciiPacketHelper(
            await plc.RequestAsync(ToBytes())
        ).errCode == 0;
    }

    public bool Execute(IPlc plc)
    {
        Route = plc.Route;
        return new ResponseAsciiPacketHelper(
            plc.Request(ToBytes())
        ).errCode == 0;
    }
}
