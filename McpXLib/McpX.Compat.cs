using McpXLib.Enums;

namespace McpXLib;

public partial class McpX
{
    public bool ReadBool(Prefix prefix, string address) => Read<bool>(prefix, address);
    public byte ReadByte(Prefix prefix, string address) => Read<byte>(prefix, address);
    public sbyte ReadSByte(Prefix prefix, string address) => Read<sbyte>(prefix, address);
    public short ReadInt16(Prefix prefix, string address) => Read<short>(prefix, address);
    public ushort ReadUInt16(Prefix prefix, string address) => Read<ushort>(prefix, address);
    public int ReadInt32(Prefix prefix, string address) => Read<int>(prefix, address);
    public uint ReadUInt32(Prefix prefix, string address) => Read<uint>(prefix, address);
    public long ReadInt64(Prefix prefix, string address) => Read<long>(prefix, address);
    public ulong ReadUInt64(Prefix prefix, string address) => Read<ulong>(prefix, address);
    public float ReadSingle(Prefix prefix, string address) => Read<float>(prefix, address);
    public double ReadDouble(Prefix prefix, string address) => Read<double>(prefix, address);

    public Task<bool> ReadBoolAsync(Prefix prefix, string address) => ReadAsync<bool>(prefix, address);
    public Task<byte> ReadByteAsync(Prefix prefix, string address) => ReadAsync<byte>(prefix, address);
    public Task<sbyte> ReadSByteAsync(Prefix prefix, string address) => ReadAsync<sbyte>(prefix, address);
    public Task<short> ReadInt16Async(Prefix prefix, string address) => ReadAsync<short>(prefix, address);
    public Task<ushort> ReadUInt16Async(Prefix prefix, string address) => ReadAsync<ushort>(prefix, address);
    public Task<int> ReadInt32Async(Prefix prefix, string address) => ReadAsync<int>(prefix, address);
    public Task<uint> ReadUInt32Async(Prefix prefix, string address) => ReadAsync<uint>(prefix, address);
    public Task<long> ReadInt64Async(Prefix prefix, string address) => ReadAsync<long>(prefix, address);
    public Task<ulong> ReadUInt64Async(Prefix prefix, string address) => ReadAsync<ulong>(prefix, address);
    public Task<float> ReadSingleAsync(Prefix prefix, string address) => ReadAsync<float>(prefix, address);
    public Task<double> ReadDoubleAsync(Prefix prefix, string address) => ReadAsync<double>(prefix, address);

    public bool[] BatchReadBool(Prefix prefix, string address, ushort length) => BatchRead<bool>(prefix, address, length);
    public byte[] BatchReadByte(Prefix prefix, string address, ushort length) => BatchRead<byte>(prefix, address, length);
    public sbyte[] BatchReadSByte(Prefix prefix, string address, ushort length) => BatchRead<sbyte>(prefix, address, length);
    public short[] BatchReadInt16(Prefix prefix, string address, ushort length) => BatchRead<short>(prefix, address, length);
    public ushort[] BatchReadUInt16(Prefix prefix, string address, ushort length) => BatchRead<ushort>(prefix, address, length);
    public int[] BatchReadInt32(Prefix prefix, string address, ushort length) => BatchRead<int>(prefix, address, length);
    public uint[] BatchReadUInt32(Prefix prefix, string address, ushort length) => BatchRead<uint>(prefix, address, length);
    public long[] BatchReadInt64(Prefix prefix, string address, ushort length) => BatchRead<long>(prefix, address, length);
    public ulong[] BatchReadUInt64(Prefix prefix, string address, ushort length) => BatchRead<ulong>(prefix, address, length);
    public float[] BatchReadSingle(Prefix prefix, string address, ushort length) => BatchRead<float>(prefix, address, length);
    public double[] BatchReadDouble(Prefix prefix, string address, ushort length) => BatchRead<double>(prefix, address, length);

    public Task<bool[]> BatchReadBoolAsync(Prefix prefix, string address, ushort length) => BatchReadAsync<bool>(prefix, address, length);
    public Task<byte[]> BatchReadByteAsync(Prefix prefix, string address, ushort length) => BatchReadAsync<byte>(prefix, address, length);
    public Task<sbyte[]> BatchReadSByteAsync(Prefix prefix, string address, ushort length) => BatchReadAsync<sbyte>(prefix, address, length);
    public Task<short[]> BatchReadInt16Async(Prefix prefix, string address, ushort length) => BatchReadAsync<short>(prefix, address, length);
    public Task<ushort[]> BatchReadUInt16Async(Prefix prefix, string address, ushort length) => BatchReadAsync<ushort>(prefix, address, length);
    public Task<int[]> BatchReadInt32Async(Prefix prefix, string address, ushort length) => BatchReadAsync<int>(prefix, address, length);
    public Task<uint[]> BatchReadUInt32Async(Prefix prefix, string address, ushort length) => BatchReadAsync<uint>(prefix, address, length);
    public Task<long[]> BatchReadInt64Async(Prefix prefix, string address, ushort length) => BatchReadAsync<long>(prefix, address, length);
    public Task<ulong[]> BatchReadUInt64Async(Prefix prefix, string address, ushort length) => BatchReadAsync<ulong>(prefix, address, length);
    public Task<float[]> BatchReadSingleAsync(Prefix prefix, string address, ushort length) => BatchReadAsync<float>(prefix, address, length);
    public Task<double[]> BatchReadDoubleAsync(Prefix prefix, string address, ushort length) => BatchReadAsync<double>(prefix, address, length);

    public void WriteBool(Prefix prefix, string address, bool value) => Write(prefix, address, value);
    public void WriteByte(Prefix prefix, string address, byte value) => Write(prefix, address, value);
    public void WriteSByte(Prefix prefix, string address, sbyte value) => Write(prefix, address, value);
    public void WriteInt16(Prefix prefix, string address, short value) => Write(prefix, address, value);
    public void WriteUInt16(Prefix prefix, string address, ushort value) => Write(prefix, address, value);
    public void WriteInt32(Prefix prefix, string address, int value) => Write(prefix, address, value);
    public void WriteUInt32(Prefix prefix, string address, uint value) => Write(prefix, address, value);
    public void WriteInt64(Prefix prefix, string address, long value) => Write(prefix, address, value);
    public void WriteUInt64(Prefix prefix, string address, ulong value) => Write(prefix, address, value);
    public void WriteSingle(Prefix prefix, string address, float value) => Write(prefix, address, value);
    public void WriteDouble(Prefix prefix, string address, double value) => Write(prefix, address, value);

    public Task WriteBoolAsync(Prefix prefix, string address, bool value) => WriteAsync(prefix, address, value);
    public Task WriteByteAsync(Prefix prefix, string address, byte value) => WriteAsync(prefix, address, value);
    public Task WriteSByteAsync(Prefix prefix, string address, sbyte value) => WriteAsync(prefix, address, value);
    public Task WriteInt16Async(Prefix prefix, string address, short value) => WriteAsync(prefix, address, value);
    public Task WriteUInt16Async(Prefix prefix, string address, ushort value) => WriteAsync(prefix, address, value);
    public Task WriteInt32Async(Prefix prefix, string address, int value) => WriteAsync(prefix, address, value);
    public Task WriteUInt32Async(Prefix prefix, string address, uint value) => WriteAsync(prefix, address, value);
    public Task WriteInt64Async(Prefix prefix, string address, long value) => WriteAsync(prefix, address, value);
    public Task WriteUInt64Async(Prefix prefix, string address, ulong value) => WriteAsync(prefix, address, value);
    public Task WriteSingleAsync(Prefix prefix, string address, float value) => WriteAsync(prefix, address, value);
    public Task WriteDoubleAsync(Prefix prefix, string address, double value) => WriteAsync(prefix, address, value);

    public bool[] BatchWriteBool(Prefix prefix, string address, bool[] values) => BatchWrite(prefix, address, values);
    public byte[] BatchWriteByte(Prefix prefix, string address, byte[] values) => BatchWrite(prefix, address, values);
    public sbyte[] BatchWriteSByte(Prefix prefix, string address, sbyte[] values) => BatchWrite(prefix, address, values);
    public short[] BatchWriteInt16(Prefix prefix, string address, short[] values) => BatchWrite(prefix, address, values);
    public ushort[] BatchWriteUInt16(Prefix prefix, string address, ushort[] values) => BatchWrite(prefix, address, values);
    public int[] BatchWriteInt32(Prefix prefix, string address, int[] values) => BatchWrite(prefix, address, values);
    public uint[] BatchWriteUInt32(Prefix prefix, string address, uint[] values) => BatchWrite(prefix, address, values);
    public long[] BatchWriteInt64(Prefix prefix, string address, long[] values) => BatchWrite(prefix, address, values);
    public ulong[] BatchWriteUInt64(Prefix prefix, string address, ulong[] values) => BatchWrite(prefix, address, values);
    public float[] BatchWriteSingle(Prefix prefix, string address, float[] values) => BatchWrite(prefix, address, values);
    public double[] BatchWriteDouble(Prefix prefix, string address, double[] values) => BatchWrite(prefix, address, values);

    public Task<bool[]> BatchWriteBoolAsync(Prefix prefix, string address, bool[] values) => BatchWriteAsync(prefix, address, values);
    public Task<byte[]> BatchWriteByteAsync(Prefix prefix, string address, byte[] values) => BatchWriteAsync(prefix, address, values);
    public Task<sbyte[]> BatchWriteSByteAsync(Prefix prefix, string address, sbyte[] values) => BatchWriteAsync(prefix, address, values);
    public Task<short[]> BatchWriteInt16Async(Prefix prefix, string address, short[] values) => BatchWriteAsync(prefix, address, values);
    public Task<ushort[]> BatchWriteUInt16Async(Prefix prefix, string address, ushort[] values) => BatchWriteAsync(prefix, address, values);
    public Task<int[]> BatchWriteInt32Async(Prefix prefix, string address, int[] values) => BatchWriteAsync(prefix, address, values);
    public Task<uint[]> BatchWriteUInt32Async(Prefix prefix, string address, uint[] values) => BatchWriteAsync(prefix, address, values);
    public Task<long[]> BatchWriteInt64Async(Prefix prefix, string address, long[] values) => BatchWriteAsync(prefix, address, values);
    public Task<ulong[]> BatchWriteUInt64Async(Prefix prefix, string address, ulong[] values) => BatchWriteAsync(prefix, address, values);
    public Task<float[]> BatchWriteSingleAsync(Prefix prefix, string address, float[] values) => BatchWriteAsync(prefix, address, values);
    public Task<double[]> BatchWriteDoubleAsync(Prefix prefix, string address, double[] values) => BatchWriteAsync(prefix, address, values);

    public (short[] wordValues, int[] doubleValues)
        RandomReadInt16Int32((Prefix prefix, string address)[] wordAddresses,
                                (Prefix prefix, string address)[] doubleWordAddresses)
            => RandomRead<short, int>(wordAddresses, doubleWordAddresses);

    public (short[] wordValues, uint[] doubleValues)
        RandomReadInt16UInt32((Prefix prefix, string address)[] wordAddresses,
                                (Prefix prefix, string address)[] doubleWordAddresses)
            => RandomRead<short, uint>(wordAddresses, doubleWordAddresses);

    public (short[] wordValues, float[] doubleValues)
        RandomReadInt16Single((Prefix prefix, string address)[] wordAddresses,
                                (Prefix prefix, string address)[] doubleWordAddresses)
            => RandomRead<short, float>(wordAddresses, doubleWordAddresses);

    public (ushort[] wordValues, int[] doubleValues)
        RandomReadUInt16Int32((Prefix prefix, string address)[] wordAddresses,
                                (Prefix prefix, string address)[] doubleWordAddresses)
            => RandomRead<ushort, int>(wordAddresses, doubleWordAddresses);

    public (ushort[] wordValues, uint[] doubleValues)
        RandomReadUInt16UInt32((Prefix prefix, string address)[] wordAddresses,
                                (Prefix prefix, string address)[] doubleWordAddresses)
            => RandomRead<ushort, uint>(wordAddresses, doubleWordAddresses);

    public (ushort[] wordValues, float[] doubleValues)
        RandomReadUInt16Single((Prefix prefix, string address)[] wordAddresses,
                                (Prefix prefix, string address)[] doubleWordAddresses)
            => RandomRead<ushort, float>(wordAddresses, doubleWordAddresses);

    public Task<(short[] wordValues, int[] doubleValues)>
        RandomReadInt16Int32Async((Prefix prefix, string address)[] wordAddresses,
                                    (Prefix prefix, string address)[] doubleWordAddresses)
            => RandomReadAsync<short, int>(wordAddresses, doubleWordAddresses);

    public Task<(short[] wordValues, uint[] doubleValues)>
        RandomReadInt16UInt32Async((Prefix prefix, string address)[] wordAddresses,
                                    (Prefix prefix, string address)[] doubleWordAddresses)
            => RandomReadAsync<short, uint>(wordAddresses, doubleWordAddresses);

    public Task<(short[] wordValues, float[] doubleValues)>
        RandomReadInt16SingleAsync((Prefix prefix, string address)[] wordAddresses,
                                    (Prefix prefix, string address)[] doubleWordAddresses)
            => RandomReadAsync<short, float>(wordAddresses, doubleWordAddresses);

    public Task<(ushort[] wordValues, int[] doubleValues)>
        RandomReadUInt16Int32Async((Prefix prefix, string address)[] wordAddresses,
                                    (Prefix prefix, string address)[] doubleWordAddresses)
            => RandomReadAsync<ushort, int>(wordAddresses, doubleWordAddresses);

    public Task<(ushort[] wordValues, uint[] doubleValues)>
        RandomReadUInt16UInt32Async((Prefix prefix, string address)[] wordAddresses,
                                    (Prefix prefix, string address)[] doubleWordAddresses)
            => RandomReadAsync<ushort, uint>(wordAddresses, doubleWordAddresses);

    public Task<(ushort[] wordValues, float[] doubleValues)>
        RandomReadUInt16SingleAsync((Prefix prefix, string address)[] wordAddresses,
                                    (Prefix prefix, string address)[] doubleWordAddresses)
            => RandomReadAsync<ushort, float>(wordAddresses, doubleWordAddresses);

    public void RandomWriteInt16Int32(
        (Prefix prefix, string address, short value)[] wordDevices,
        (Prefix prefix, string address, int value)[] doubleWordDevices)
            => RandomWrite<short, int>(wordDevices, doubleWordDevices);

    public void RandomWriteInt16UInt32(
        (Prefix prefix, string address, short value)[] wordDevices,
        (Prefix prefix, string address, uint value)[] doubleWordDevices)
            => RandomWrite<short, uint>(wordDevices, doubleWordDevices);

    public void RandomWriteInt16Single(
        (Prefix prefix, string address, short value)[] wordDevices,
        (Prefix prefix, string address, float value)[] doubleWordDevices)
            => RandomWrite<short, float>(wordDevices, doubleWordDevices);

    public void RandomWriteUInt16Int32(
        (Prefix prefix, string address, ushort value)[] wordDevices,
        (Prefix prefix, string address, int value)[] doubleWordDevices)
            => RandomWrite<ushort, int>(wordDevices, doubleWordDevices);

    public void RandomWriteUInt16UInt32(
        (Prefix prefix, string address, ushort value)[] wordDevices,
        (Prefix prefix, string address, uint value)[] doubleWordDevices)
            => RandomWrite<ushort, uint>(wordDevices, doubleWordDevices);

    public void RandomWriteUInt16Single(
        (Prefix prefix, string address, ushort value)[] wordDevices,
        (Prefix prefix, string address, float value)[] doubleWordDevices)
            => RandomWrite<ushort, float>(wordDevices, doubleWordDevices);

    public Task RandomWriteInt16Int32Async(
        (Prefix prefix, string address, short value)[] wordDevices,
        (Prefix prefix, string address, int value)[] doubleWordDevices)
            => RandomWriteAsync<short, int>(wordDevices, doubleWordDevices);

    public Task RandomWriteInt16UInt32Async(
        (Prefix prefix, string address, short value)[] wordDevices,
        (Prefix prefix, string address, uint value)[] doubleWordDevices)
            => RandomWriteAsync<short, uint>(wordDevices, doubleWordDevices);

    public Task RandomWriteInt16SingleAsync(
        (Prefix prefix, string address, short value)[] wordDevices,
        (Prefix prefix, string address, float value)[] doubleWordDevices)
            => RandomWriteAsync<short, float>(wordDevices, doubleWordDevices);

    public Task RandomWriteUInt16Int32Async(
        (Prefix prefix, string address, ushort value)[] wordDevices,
        (Prefix prefix, string address, int value)[] doubleWordDevices)
            => RandomWriteAsync<ushort, int>(wordDevices, doubleWordDevices);

    public Task RandomWriteUInt16UInt32Async(
        (Prefix prefix, string address, ushort value)[] wordDevices,
        (Prefix prefix, string address, uint value)[] doubleWordDevices)
            => RandomWriteAsync<ushort, uint>(wordDevices, doubleWordDevices);

    public Task RandomWriteUInt16SingleAsync(
        (Prefix prefix, string address, ushort value)[] wordDevices,
        (Prefix prefix, string address, float value)[] doubleWordDevices)
            => RandomWriteAsync<ushort, float>(wordDevices, doubleWordDevices);
}
