using McpXLib.Abstructs;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Helpers;
using McpXLib.Utils;

namespace McpXLib.Commands;

public sealed class WordBatchReadCommand<T> : BaseCommand, IPlcCommand<T[]>
    where T : unmanaged
{
    public const ushort MIN_WORD_LENGTH = 1;
    public const ushort MAX_WORD_LENGTH = 960;
    private readonly ushort wordLength;

    public WordBatchReadCommand(Prefix prefix, string address, ushort wordLength) : base()
    {
        this.wordLength = wordLength;

        ValidatePramater();

        var commandContent = new List<byte>();
        commandContent.AddRange(DeviceConverter.ToByteAddress(prefix, address));
        commandContent.AddRange(BitConverter.GetBytes(wordLength));

        content = new ContentPacketHelper(
            command: [0x01, 0x04],
            subCommand: [0x00, 0x00],
            commandContent: commandContent.ToArray(),
            monitoringTimer: [0x00, 0x00]
        );
    }

    public void ValidatePramater()
    {
        if (wordLength < MIN_WORD_LENGTH || wordLength > MAX_WORD_LENGTH)
        {
            throw new ArgumentException($"Word length can be from {MIN_WORD_LENGTH} to {MAX_WORD_LENGTH}.");
        }
    }

    public async Task<T[]> ExecuteAsync(Interfaces.IPlc plc)
    {
        Route = plc.Route;
        return DeviceConverter.ConvertValueArray<T>(
            new ResponsePacketHelper(
                await plc.RequestAsync(ToBytes())
            ).Content
        );
    }

    public T[] Execute(Interfaces.IPlc plc)
    {
        Route = plc.Route;
        return DeviceConverter.ConvertValueArray<T>(
            new ResponsePacketHelper(
                plc.Request(ToBytes())
            ).Content
        );
    }
}
