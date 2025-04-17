using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Utils;
using McpXLib.Builders;

namespace McpXLib.Commands;

internal sealed class BitBatchReadCommand : IPlcCommand<bool[]>
{
    internal const ushort MIN_BIT_LENGTH = 1;
    internal const ushort MAX_BIT_LENGTH = 7168;
    private readonly ushort bitLength;
    private readonly CommandPacketBuilder commandPacketBuilder;

    internal BitBatchReadCommand(Prefix prefix, string address, ushort bitLength) : base()
    {
        this.bitLength = bitLength;

        ValidatePramater();

        commandPacketBuilder = new CommandPacketBuilder(
            command: [0x01, 0x04],
            subCommand: [0x01, 0x00],
            payloadBuilder: new DevicePayloadBuilder(prefix, address, bitLength),
            monitoringTimer: [0x00, 0x00]
        );
    }

    internal void ValidatePramater()
    {
        if (bitLength < MIN_BIT_LENGTH || bitLength > MAX_BIT_LENGTH)
        {
            throw new ArgumentException($"Bit length can be from {MIN_BIT_LENGTH} to {MAX_BIT_LENGTH}.");
        }
    }

    public async Task<bool[]> ExecuteAsync(IPlc plc)
    {
        var requestFrameSelector = new RequestFrameSelector(plc, commandPacketBuilder);
        var responseFrameSelector = new ResponseFrameSelector(
            plc,
            requestFrameSelector.GetSerialNumber(),
            DeviceAccessMode.Bit
        );

        var responseContent = responseFrameSelector.ParsePacket(
            await plc.RequestAsync(requestFrameSelector.GetRequestPacket(), responseFrameSelector)
        );

        // MEMO:
        //  読出しビット数が奇数の場合、余分な4ビットを含む1バイトをクリア
        if (bitLength % 2 != 0) 
        {
            var responseContentList = responseContent.ToList();
            responseContentList.RemoveAt(responseContent.Count() - 1);
            responseContent = responseContentList.ToArray();
        }

        return DeviceConverter.ConvertValueArray<bool>(responseContent);
    }

    public bool[] Execute(IPlc plc)
    {
        var requestFrameSelector = new RequestFrameSelector(plc, commandPacketBuilder);
        var responseFrameSelector = new ResponseFrameSelector(
            plc,
            requestFrameSelector.GetSerialNumber(),
            DeviceAccessMode.Bit
        );

        var responseContent = responseFrameSelector.ParsePacket(
            plc.Request(requestFrameSelector.GetRequestPacket(), responseFrameSelector)
        );

        // MEMO:
        //  読出しビット数が奇数の場合、余分な4ビットを含む1バイトをクリア
        if (bitLength % 2 != 0) 
        {
            var responseContentList = responseContent.ToList();
            responseContentList.RemoveAt(responseContent.Count() - 1);
            responseContent = responseContentList.ToArray();
        }

        return DeviceConverter.ConvertValueArray<bool>(responseContent);
    }

    public byte[] ToBinaryBytes()
    {
        return commandPacketBuilder.ToBinaryBytes();
    }

    public byte[] ToAsciiBytes()
    {
        return commandPacketBuilder.ToAsciiBytes();
    }
}
