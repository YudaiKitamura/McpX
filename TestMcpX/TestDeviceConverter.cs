using McpXLib.Utils;
using Bogus;
using McpXLib.Enums;
using McpXLib.Exceptions;

namespace TestMcpX;

[TestClass]
public sealed class TestDeviceConverter
{
    private readonly Faker faker = new Faker();

    [TestMethod]
    public void ConvertByteValueArray_ShouldConvertVariousTypesToByteArray()
    {
        TestConvertByteValueArray((short)faker.Random.Short());
        TestConvertByteValueArray((ushort)faker.Random.UShort());
        TestConvertByteValueArray(faker.Random.Int());
        TestConvertByteValueArray(faker.Random.UInt());
        TestConvertByteValueArray(faker.Random.Long());
        TestConvertByteValueArray(faker.Random.ULong());
        TestConvertByteValueArray(faker.Random.Float());
        TestConvertByteValueArray(faker.Random.Double());
        TestConvertByteValueArray(faker.Random.Bool());

        TestConvertByteValueArray<short>(0, short.MinValue, short.MaxValue);
        TestConvertByteValueArray<ushort>(0, ushort.MinValue, ushort.MaxValue);
        TestConvertByteValueArray<int>(0, int.MinValue, int.MaxValue);
        TestConvertByteValueArray<uint>(0, uint.MinValue, uint.MaxValue);
        TestConvertByteValueArray<long>(0, long.MinValue, long.MaxValue);
        TestConvertByteValueArray<ulong>(0, ulong.MinValue, ulong.MaxValue);
        TestConvertByteValueArray<float>(0, float.MinValue, float.MaxValue);
        TestConvertByteValueArray<double>(0, double.MinValue, double.MaxValue);
        TestConvertByteValueArray<bool>(true, false);
        TestConvertByteValueArray<byte>(0, byte.MinValue, byte.MaxValue);
        TestConvertByteValueArray<sbyte>(0, sbyte.MinValue, sbyte.MaxValue);
    }

    private void TestConvertByteValueArray<T>(params T[] values) where T : unmanaged
    {
        byte[] expected = values.SelectMany<T,byte>(v =>
        {
            if (typeof(T) == typeof(bool))
            {
                return [(bool)(object)v ? (byte)0x01 : (byte)0x00];
            }
            else if (typeof(T) == typeof(byte))
            {
                // 1要素=1ワード（下位バイト）
                return [(byte)(object)v, (byte)0x00];
            }
            else if (typeof(T) == typeof(sbyte))
            {
                // 1要素=1ワード（符号拡張）
                return BitConverter.GetBytes((short)(sbyte)(object)v);
            }
            return BitConverter.GetBytes((dynamic)v);
        }).ToArray();

        byte[] actual = DeviceConverter.ConvertByteValueArray(values);

        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    [ExpectedException(typeof(NotSupportedException))]
    public void ConvertByteValueArray_ShouldThrowExceptionForUnsupportedType()
    {
        DeviceConverter.ConvertByteValueArray<char>(faker.Random.Chars());
    }

    [TestMethod]
    public void ConvertValueArray_ShouldConvertVariousTypes()
    {
        TestConvertValueArray((short)faker.Random.Short());
        TestConvertValueArray((ushort)faker.Random.UShort());
        TestConvertValueArray(faker.Random.Int());
        TestConvertValueArray(faker.Random.UInt());
        TestConvertValueArray(faker.Random.Long());
        TestConvertValueArray(faker.Random.ULong());
        TestConvertValueArray(faker.Random.Float());
        TestConvertValueArray(faker.Random.Double());
        TestConvertValueArray(faker.Random.Bool());
        TestConvertValueArray<byte>(faker.Random.Byte());
        TestConvertValueArray<bool>(faker.Random.Bool());

        TestConvertValueArray<short>(0, short.MinValue, short.MaxValue);
        TestConvertValueArray<ushort>(0, ushort.MinValue, ushort.MaxValue);
        TestConvertValueArray<int>(0, int.MinValue, int.MaxValue);
        TestConvertValueArray<uint>(0, uint.MinValue, uint.MaxValue);
        TestConvertValueArray<long>(0, long.MinValue, long.MaxValue);
        TestConvertValueArray<ulong>(0, ulong.MinValue, ulong.MaxValue);
        TestConvertValueArray<float>(0f, float.MinValue, float.MaxValue);
        TestConvertValueArray<double>(0.0, double.MinValue, double.MaxValue);
        TestConvertValueArray<byte>(0, byte.MinValue, byte.MaxValue);
        TestConvertValueArray<sbyte>(0, sbyte.MinValue, sbyte.MaxValue);
        TestConvertValueArray<bool>(true, false);
    }

    private void TestConvertValueArray<T>(params T[] values) where T : unmanaged
    {
        byte[] input = values.SelectMany<T,byte>(v =>
        {
            if (typeof(T) == typeof(bool))
            {
                return [(bool)(object)v ? (byte)0x01 : (byte)0x00];
            }
            else if (typeof(T) == typeof(byte))
            {
                // 1要素=1ワード（下位バイト）
                return [(byte)(object)v, (byte)0x00];
            } 
            else if (typeof(T) == typeof(sbyte))
            {
                return BitConverter.GetBytes((short)(sbyte)(object)v);
            }
            return BitConverter.GetBytes((dynamic)v);
        }).ToArray();

        
        T[] expected = values;
        T[] actual = DeviceConverter.ConvertValueArray<T>(input);

        CollectionAssert.AreEqual(values, actual);
    }

    [TestMethod]
    [ExpectedException(typeof(NotSupportedException))]
    public void ConvertValueArray_ShouldThrowExceptionForUnsupportedType()
    {
        DeviceConverter.ConvertValueArray<char>(faker.Random.Bytes(2));
    }

    [TestMethod]
    public void ConvertValueArray_ShouldHandleEmptyByteArray()
    {
        int[] result = DeviceConverter.ConvertValueArray<int>(new byte[0]);
        Assert.AreEqual(0, result.Length);
    }

    [TestMethod]
    public void ConvertValueArray_ShouldHandleNonMultipleOfTypeSize()
    {
        var ex = Assert.ThrowsException<ArgumentException>(() => {
            DeviceConverter.ConvertValueArray<short>(faker.Random.Bytes(3));
        });

        Assert.IsInstanceOfType<ArgumentException>(ex);
        Assert.AreEqual("Byte array is not the correct length.", ex.Message);


        ex = Assert.ThrowsException<ArgumentException>(() => {
            DeviceConverter.ConvertValueArray<ushort>(faker.Random.Bytes(3));
        });

        Assert.IsInstanceOfType<ArgumentException>(ex);
        Assert.AreEqual("Byte array is not the correct length.", ex.Message);


        ex = Assert.ThrowsException<ArgumentException>(() => {
            DeviceConverter.ConvertValueArray<int>(faker.Random.Bytes(3));
        });

        Assert.IsInstanceOfType<ArgumentException>(ex);
        Assert.AreEqual("Byte array is not the correct length.", ex.Message);
        

        ex = Assert.ThrowsException<ArgumentException>(() => {
            DeviceConverter.ConvertValueArray<uint>(faker.Random.Bytes(3));
        });

        Assert.IsInstanceOfType<ArgumentException>(ex);
        Assert.AreEqual("Byte array is not the correct length.", ex.Message);
        

        ex = Assert.ThrowsException<ArgumentException>(() => {
            DeviceConverter.ConvertValueArray<float>(faker.Random.Bytes(3));
        });

        Assert.IsInstanceOfType<ArgumentException>(ex);
        Assert.AreEqual("Byte array is not the correct length.", ex.Message);
        

        ex = Assert.ThrowsException<ArgumentException>(() => {
            DeviceConverter.ConvertValueArray<double>(faker.Random.Bytes(3));
        });

        Assert.IsInstanceOfType<ArgumentException>(ex);
        Assert.AreEqual("Byte array is not the correct length.", ex.Message);
        

        ex = Assert.ThrowsException<ArgumentException>(() => {
            DeviceConverter.ConvertValueArray<long>(faker.Random.Bytes(3));
        });

        Assert.IsInstanceOfType<ArgumentException>(ex);
        Assert.AreEqual("Byte array is not the correct length.", ex.Message);
        

        ex = Assert.ThrowsException<ArgumentException>(() => {
            DeviceConverter.ConvertValueArray<ulong>(faker.Random.Bytes(3));
        });

        Assert.IsInstanceOfType<ArgumentException>(ex);
        Assert.AreEqual("Byte array is not the correct length.", ex.Message);
    }

    [TestMethod]
    public void GetWordLength_ShouldGetVariousTypesToWordLength()
    {
        int length = DeviceConverter.GetWordLength<bool>();
        Assert.AreEqual(1, length);

        length = DeviceConverter.GetWordLength<short>();
        Assert.AreEqual(1, length);

        length = DeviceConverter.GetWordLength<ushort>();
        Assert.AreEqual(1, length);

        length = DeviceConverter.GetWordLength<int>();
        Assert.AreEqual(2, length);

        length = DeviceConverter.GetWordLength<uint>();
        Assert.AreEqual(2, length);

        length = DeviceConverter.GetWordLength<float>();
        Assert.AreEqual(2, length);

        length = DeviceConverter.GetWordLength<double>();
        Assert.AreEqual(4, length);

        length = DeviceConverter.GetWordLength<long>();
        Assert.AreEqual(4, length);

        length = DeviceConverter.GetWordLength<ulong>();
        Assert.AreEqual(4, length);
    }

    [TestMethod]
    [ExpectedException(typeof(DeviceAddressException))]
    public void ToByteAddress_ShouldThrowExceptionForInvalidHexDevice()
    {
        Prefix hexPrefix = faker.PickRandom(
            Prefix.X,
            Prefix.Y, 
            Prefix.B,
            Prefix.W,
            Prefix.SB,
            Prefix.DX,
            Prefix.DY 
        );

        DeviceConverter.ToByteAddress(hexPrefix, faker.Random.String());
    }

    [TestMethod]
    [ExpectedException(typeof(DeviceAddressException))]
    public void ToByteAddress_ShouldThrowExceptionForInvalidDecDevice1()
    {
        Prefix decPrefix = faker.PickRandomWithout(
            Prefix.X,
            Prefix.Y, 
            Prefix.B,
            Prefix.W,
            Prefix.SB,
            Prefix.SW,
            Prefix.DX,
            Prefix.DY 
        );

        DeviceConverter.ToByteAddress(decPrefix, faker.Random.String());
    }

    [TestMethod]
    [ExpectedException(typeof(DeviceAddressException))]
    public void ToByteAddress_ShouldThrowExceptionForInvalidDecDevice2()
    {
        Prefix decPrefix = faker.PickRandomWithout(
            Prefix.X,
            Prefix.Y, 
            Prefix.B,
            Prefix.W,
            Prefix.SB,
            Prefix.SW,
            Prefix.DX,
            Prefix.DY 
        );

        DeviceConverter.ToByteAddress(decPrefix, faker.Random.Int(max:-1).ToString());
    }

    [TestMethod]
    public void ToByteAddress_ShouldConvertHexDeviceToByte()
    {
        Prefix hexPrefix = faker.PickRandom(
            Prefix.X,
            Prefix.Y, 
            Prefix.B,
            Prefix.W,
            Prefix.SB,
            Prefix.DX,
            Prefix.DY 
        );

        ushort address = faker.Random.UShort();
        byte[] expected = BitConverter.GetBytes(address).ToArray();
        expected = expected.Concat([(byte)0x00, (byte)hexPrefix]).ToArray();

        byte[] actual = DeviceConverter.ToByteAddress(hexPrefix, address.ToString("X"));

        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void ToByteAddress_ShouldConvertHexDeviceToByte2()
    {
        Prefix hexPrefix = faker.PickRandom(
            Prefix.X,
            Prefix.Y,
            Prefix.B,
            Prefix.W,
            Prefix.SB,
            Prefix.DX,
            Prefix.DY
        );

        ushort address = Convert.ToUInt16("0", 16);
        byte[] expected = BitConverter.GetBytes(address).ToArray();
        expected = expected.Concat([(byte)0x00, (byte)hexPrefix]).ToArray();

        byte[] actual = DeviceConverter.ToByteAddress(hexPrefix, address.ToString("X"));

        CollectionAssert.AreEqual(expected, actual);


        address = Convert.ToUInt16("F", 16);
        expected = BitConverter.GetBytes(address).ToArray();
        expected = expected.Concat([(byte)0x00, (byte)hexPrefix]).ToArray();

        actual = DeviceConverter.ToByteAddress(hexPrefix, address.ToString("X"));

        CollectionAssert.AreEqual(expected, actual);
        

        address = Convert.ToUInt16("100", 16);
        expected = BitConverter.GetBytes(address).ToArray();
        expected = expected.Concat([(byte)0x00, (byte)hexPrefix]).ToArray();

        actual = DeviceConverter.ToByteAddress(hexPrefix, address.ToString("X"));

        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void ToByteAddress_ShouldConvertDecDeviceToByte()
    {
        Prefix decPrefix = faker.PickRandomWithout(
            Prefix.X,
            Prefix.Y, 
            Prefix.B,
            Prefix.W,
            Prefix.SB,
            Prefix.SW,
            Prefix.DX,
            Prefix.DY 
        );

        ushort address = faker.Random.UShort();
        byte[] expected = BitConverter.GetBytes((uint)address).ToArray();
        expected[^1] = (byte)decPrefix;

        byte[] actual = DeviceConverter.ToByteAddress(decPrefix, address.ToString());

        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void ToByteAddress_ShouldConvertHexDeviceToByteForiQR()
    {
        Prefix hexPrefix = faker.PickRandom(
            Prefix.X,
            Prefix.Y,
            Prefix.B,
            Prefix.W,
            Prefix.SB,
            Prefix.DX,
            Prefix.DY
        );

        ushort address = faker.Random.UShort();
        byte[] expected = BitConverter.GetBytes((uint)address)
            .Concat([(byte)hexPrefix, (byte)0x00])
            .ToArray();

        byte[] actual = DeviceConverter.ToByteAddress(hexPrefix, address.ToString("X"), ProcessorSeries.iQR);

        Assert.AreEqual(6, actual.Length);
        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void ToByteAddress_ShouldConvertDecDeviceToByteForiQR()
    {
        Prefix decPrefix = faker.PickRandomWithout(
            Prefix.X,
            Prefix.Y,
            Prefix.B,
            Prefix.W,
            Prefix.SB,
            Prefix.SW,
            Prefix.DX,
            Prefix.DY
        );

        uint address = faker.Random.UInt();
        byte[] expected = BitConverter.GetBytes(address)
            .Concat([(byte)decPrefix, (byte)0x00])
            .ToArray();

        byte[] actual = DeviceConverter.ToByteAddress(decPrefix, address.ToString(), ProcessorSeries.iQR);

        Assert.AreEqual(6, actual.Length);
        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void ToASCIIAddress_ShouldConvertForQ()
    {
        Assert.AreEqual("D*000010", DeviceConverter.ToASCIIAddress(Prefix.D, "10"));
        Assert.AreEqual("SB00001A", DeviceConverter.ToASCIIAddress(Prefix.SB, "1A"));
    }

    [TestMethod]
    public void ToASCIIAddress_ShouldConvertForiQR()
    {
        Assert.AreEqual("D***00000010", DeviceConverter.ToASCIIAddress(Prefix.D, "10", ProcessorSeries.iQR));
        Assert.AreEqual("SB**0000001A", DeviceConverter.ToASCIIAddress(Prefix.SB, "1A", ProcessorSeries.iQR));
    }

    [TestMethod]
    public void ToSubCommand_ShouldSetExtensionBitForiQR()
    {
        // Q はそのまま
        CollectionAssert.AreEqual(new byte[] { 0x00, 0x00 }, DeviceConverter.ToSubCommand([0x00, 0x00], ProcessorSeries.Q));
        CollectionAssert.AreEqual(new byte[] { 0x01, 0x00 }, DeviceConverter.ToSubCommand([0x01, 0x00], ProcessorSeries.Q));

        // iQR は先頭バイトに拡張指定ビット(0x02)を立てる
        CollectionAssert.AreEqual(new byte[] { 0x02, 0x00 }, DeviceConverter.ToSubCommand([0x00, 0x00], ProcessorSeries.iQR));
        CollectionAssert.AreEqual(new byte[] { 0x03, 0x00 }, DeviceConverter.ToSubCommand([0x01, 0x00], ProcessorSeries.iQR));
    }

    [TestMethod]
    [ExpectedException(typeof(DeviceAddressException))]
    public void GetOffsetAddress_ShouldThrowExceptionForInvalidDecDevice1()
    {
        Prefix decPrefix = faker.PickRandomWithout(
            Prefix.X,
            Prefix.Y, 
            Prefix.B,
            Prefix.W,
            Prefix.SB,
            Prefix.SW,
            Prefix.DX,
            Prefix.DY 
        );

        DeviceConverter.GetOffsetAddress(decPrefix, faker.Random.String(), faker.Random.Int());
    }

    [TestMethod]
    [ExpectedException(typeof(DeviceAddressException))]
    public void GetOffsetAddress_ShouldThrowExceptionForInvalidDecDevice2()
    {
        Prefix decPrefix = faker.PickRandomWithout(
            Prefix.X,
            Prefix.Y, 
            Prefix.B,
            Prefix.W,
            Prefix.SB,
            Prefix.SW,
            Prefix.DX,
            Prefix.DY 
        );

        DeviceConverter.GetOffsetAddress(decPrefix, faker.Random.Int(max:-1).ToString(), faker.Random.Int());
    }

    [TestMethod]
    [ExpectedException(typeof(OverflowException))]
    public void GetOffsetAddress_ShouldThrowExceptionForNegativeOffset1()
    {
        Prefix hexPrefix = faker.PickRandom(
            Prefix.X,
            Prefix.Y, 
            Prefix.B,
            Prefix.W,
            Prefix.SB,
            Prefix.DX,
            Prefix.DY 
        );

        uint address = faker.Random.UInt(max:int.MaxValue);
        DeviceConverter.GetOffsetAddress(hexPrefix, address.ToString("X"), -(int)(address + 1));
    }

    [TestMethod]
    [ExpectedException(typeof(OverflowException))]
    public void GetOffsetAddress_ShouldThrowExceptionForNegativeOffset2()
    {
        Prefix decPrefix = faker.PickRandomWithout(
            Prefix.X,
            Prefix.Y, 
            Prefix.B,
            Prefix.W,
            Prefix.SB,
            Prefix.SW,
            Prefix.DX,
            Prefix.DY 
        );

        uint address = faker.Random.UInt(max:int.MaxValue);
        DeviceConverter.GetOffsetAddress(decPrefix, address.ToString(), -(int)(address + 1));
    }

    [TestMethod]
    public void GetOffsetAddress_ShouldDecAddressOffset()
    {
        Prefix decPrefix = faker.PickRandomWithout(
            Prefix.X,
            Prefix.Y, 
            Prefix.B,
            Prefix.W,
            Prefix.SB,
            Prefix.SW,
            Prefix.DX,
            Prefix.DY 
        );

        uint address = faker.Random.UInt(max:int.MaxValue);
        int offset = faker.Random.Int(min:(int)-address);

        Assert.AreEqual(
            $"{address + offset}",
            DeviceConverter.GetOffsetAddress(decPrefix, address.ToString(), offset)
        );
    }

    [TestMethod]
    public void GetOffsetAddress_ShouldHexAddressOffset()
    {
        Prefix hexPrefix = faker.PickRandom(
            Prefix.X,
            Prefix.Y, 
            Prefix.B,
            Prefix.W,
            Prefix.SB,
            Prefix.DX,
            Prefix.DY 
        );

        uint address = faker.Random.UInt(max:int.MaxValue);
        int offset = faker.Random.Int(min:(int)-address);

        string actual = DeviceConverter.GetOffsetAddress(hexPrefix, address.ToString("X"), offset);
        Assert.AreEqual(
            $"{(address + offset).ToString("X")}",
            actual,
            $"{hexPrefix} {address.ToString("X")} {offset}"
        );
    }

    // 値の配列 ⇔ バイト列の変換が、1値ずつの変換（StructToBytes）と一致し、往復で元に戻ること
    private static void AssertRoundTrip<T>(params T[] values) where T : unmanaged
    {
        var expected = values.SelectMany(v => DeviceConverter.StructToBytes(v)).ToArray();
        var bytes = DeviceConverter.ConvertByteValueArray(values);

        CollectionAssert.AreEqual(expected, bytes, typeof(T).Name);
        CollectionAssert.AreEqual(values, DeviceConverter.ConvertValueArray<T>(bytes), typeof(T).Name);
    }

    [TestMethod]
    public void TestConvertArraysMatchSingleValueConversion()
    {
        var random = new Random(1);
        T[] Randoms<T>(Func<T> next) => Enumerable.Range(0, 100).Select(_ => next()).ToArray();

        AssertRoundTrip(Randoms(() => random.Next(2) == 1));
        AssertRoundTrip(Randoms(() => (short)random.Next(short.MinValue, short.MaxValue + 1)).Concat([short.MinValue, short.MaxValue, (short)-1]).ToArray());
        AssertRoundTrip(Randoms(() => (ushort)random.Next(0, ushort.MaxValue + 1)).Concat([ushort.MinValue, ushort.MaxValue]).ToArray());
        AssertRoundTrip(Randoms(() => random.Next(int.MinValue, int.MaxValue)).Concat([int.MinValue, int.MaxValue, -1]).ToArray());
        AssertRoundTrip(Randoms(() => (uint)random.NextInt64(0, uint.MaxValue)).Concat([uint.MinValue, uint.MaxValue]).ToArray());
        AssertRoundTrip(Randoms(() => random.NextInt64()).Concat([long.MinValue, long.MaxValue, -1L]).ToArray());
        AssertRoundTrip(Randoms(() => (ulong)random.NextInt64()).Concat([ulong.MinValue, ulong.MaxValue]).ToArray());
        AssertRoundTrip(Randoms(() => (float)(random.NextDouble() * 2e6 - 1e6)).Concat([float.MinValue, float.MaxValue, float.Epsilon, -0.5f]).ToArray());
        AssertRoundTrip(Randoms(() => random.NextDouble() * 2e12 - 1e12).Concat([double.MinValue, double.MaxValue, double.Epsilon, -0.5]).ToArray());

        // byte は下位バイトに値・上位バイトに 0、sbyte は16ビットに符号拡張した1ワード
        AssertRoundTrip(Randoms(() => (byte)random.Next(256)).Concat([byte.MinValue, byte.MaxValue]).ToArray());
        AssertRoundTrip(Randoms(() => (sbyte)random.Next(-128, 128)).Concat([sbyte.MinValue, sbyte.MaxValue, (sbyte)-1]).ToArray());
        CollectionAssert.AreEqual(new byte[] { 0xFF, 0xFF, 0x7F, 0x00 }, DeviceConverter.ConvertByteValueArray(new sbyte[] { -1, 127 }));

        // 応答の byte / sbyte は、ワードの下位バイトだけを使う（上位バイトの値は無視する）
        CollectionAssert.AreEqual(new byte[] { 0xAB, 0x01 }, DeviceConverter.ConvertValueArray<byte>([0xAB, 0x12, 0x01, 0xFF]));
        CollectionAssert.AreEqual(new sbyte[] { -1, 1 }, DeviceConverter.ConvertValueArray<sbyte>([0xFF, 0x00, 0x01, 0xFF]));

        // 長さが型のバイト数の倍数でない場合は ArgumentException（応答の解析では ReceivePacketException に変換される）
        Assert.ThrowsException<ArgumentException>(() => DeviceConverter.ConvertValueArray<int>([0x01, 0x02, 0x03]));
    }
}
