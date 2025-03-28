using McpXLib.Abstructs;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Helpers;
using McpXLib.Utils;

namespace McpXLib.Commands;

public sealed class WordBatchReadAsciiCommand<T> : BaseAsciiCommand, IPlcCommand<T[]>
    where T : unmanaged
{
    public const ushort MIN_WORD_LENGTH = 1;
    public const ushort MAX_WORD_LENGTH = 960;
    private readonly ushort wordLength;

    public WordBatchReadAsciiCommand(Prefix prefix, string address, ushort wordLength) : base()
    {
        this.wordLength = wordLength;

        ValidatePramater();

        content = new ContentAsciiPacketHelper(
            command: "0401",
            subCommand: "0000",
            commandContent: $"{ DeviceConverter.ToASCIIAddress(prefix, address) }{ wordLength.ToString().PadLeft(4, '0') }",
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

    public async Task<T[]> ExecuteAsync(IPlc plc)
    {
        Route = plc.Route;
        return DeviceConverter.ConvertValueArray<T>(
            new ResponseAsciiPacketHelper(
                await plc.RequestAsync(ToBytes())
            ).Content
        );
    }

    public T[] Execute(IPlc plc)
    {
        Route = plc.Route;
        return DeviceConverter.ConvertValueArray<T>(
            new ResponseAsciiPacketHelper(
                plc.Request(ToBytes())
            ).Content
        );
    }
}
