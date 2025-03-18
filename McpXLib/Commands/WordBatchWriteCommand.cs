using McpXLib.Abstructs;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Helpers;
using McpXLib.Utils;

namespace McpXLib.Commands;

public sealed class WordBatchWriteCommand<T> : BaseCommand, IPlcCommand<bool>
    where T : unmanaged
{
    public const ushort MIN_WORD_LENGTH = 1;
    public const ushort MAX_WORD_LENGTH = 960;
    private readonly ushort wordLength;

    public WordBatchWriteCommand(Prefix prefix, string address, T[] values) : base()
    {
        wordLength = (ushort)(values.Length * DeviceConverter.GetWordLength<T>());

        ValidatePramater();

        var commandContent = new List<byte>();
        commandContent.AddRange(DeviceConverter.ToByteAddress(prefix, address));
        commandContent.AddRange(BitConverter.GetBytes(wordLength));
        commandContent.AddRange(DeviceConverter.ConvertByteValueArray(values));

        content = new ContentPacketHelper(
            command: [0x01, 0x14],
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

    public async Task<bool> ExecuteAsync(Interfaces.IPlc plc)
    {
        Route = plc.Route;
        return new ResponsePacketHelper(
            await plc.RequestAsync(ToBytes())
        ).errCode == 0;
    }

    public bool Execute(Interfaces.IPlc plc)
    {
        Route = plc.Route;
        return new ResponsePacketHelper(
            plc.Request(ToBytes())
        ).errCode == 0;
    }
}
