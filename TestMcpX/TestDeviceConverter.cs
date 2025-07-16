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
    }

    private void TestConvertByteValueArray<T>(params T[] values) where T : unmanaged
    {
        byte[] expected = values.SelectMany<T,byte>(v =>
        {
            if (typeof(T) == typeof(bool))
            {
                return [(bool)(object)v ? (byte)0x01 : (byte)0x00];
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
                return [(byte)(object)v];
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
    public void ToByteAddress_ShuldThrowExceptionForInvalidHexDevice()
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
    public void ToByteAddress_ShuldThrowExceptionForInvalidDecDevice1()
    {
        Prefix decPrefix = faker.PickRandomWithout(
            Prefix.X,
            Prefix.Y, 
            Prefix.B,
            Prefix.W,
            Prefix.SB,
            Prefix.DX,
            Prefix.DY 
        );

        DeviceConverter.ToByteAddress(decPrefix, faker.Random.String());
    }

    [TestMethod]
    [ExpectedException(typeof(DeviceAddressException))]
    public void ToByteAddress_ShuldThrowExceptionForInvalidDecDevice2()
    {
        Prefix decPrefix = faker.PickRandomWithout(
            Prefix.X,
            Prefix.Y, 
            Prefix.B,
            Prefix.W,
            Prefix.SB,
            Prefix.DX,
            Prefix.DY 
        );

        DeviceConverter.ToByteAddress(decPrefix, faker.Random.Int(max:-1).ToString());
    }

    [TestMethod]
    public void ToByteAddress_ShuldConvertHexDeviceToByte()
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
    public void ToByteAddress_ShuldConvertHexDeviceToByte2()
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
    public void ToByteAddress_ShuldConvertDecDeviceToByte()
    {
        Prefix decPrefix = faker.PickRandomWithout(
            Prefix.X,
            Prefix.Y, 
            Prefix.B,
            Prefix.W,
            Prefix.SB,
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
    [ExpectedException(typeof(DeviceAddressException))]
    public void GetOffsetAddress_ShuldThrowExceptionForInvalidDecDevice1()
    {
        Prefix decPrefix = faker.PickRandomWithout(
            Prefix.X,
            Prefix.Y, 
            Prefix.B,
            Prefix.W,
            Prefix.SB,
            Prefix.DX,
            Prefix.DY 
        );

        DeviceConverter.GetOffsetAddress(decPrefix, faker.Random.String(), faker.Random.Int());
    }

    [TestMethod]
    [ExpectedException(typeof(DeviceAddressException))]
    public void GetOffsetAddress_ShuldThrowExceptionForInvalidDecDevice2()
    {
        Prefix decPrefix = faker.PickRandomWithout(
            Prefix.X,
            Prefix.Y, 
            Prefix.B,
            Prefix.W,
            Prefix.SB,
            Prefix.DX,
            Prefix.DY 
        );

        DeviceConverter.GetOffsetAddress(decPrefix, faker.Random.Int(max:-1).ToString(), faker.Random.Int());
    }

    [TestMethod]
    [ExpectedException(typeof(OverflowException))]
    public void GetOffsetAddress_ShuldThrowExceptionForNegativeOffset1()
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
    public void GetOffsetAddress_ShuldThrowExceptionForNegativeOffset2()
    {
        Prefix decPrefix = faker.PickRandomWithout(
            Prefix.X,
            Prefix.Y, 
            Prefix.B,
            Prefix.W,
            Prefix.SB,
            Prefix.DX,
            Prefix.DY 
        );

        uint address = faker.Random.UInt(max:int.MaxValue);
        DeviceConverter.GetOffsetAddress(decPrefix, address.ToString(), -(int)(address + 1));
    }

    [TestMethod]
    public void GetOffsetAddress_ShuldDecAddressOffset()
    {
        Prefix decPrefix = faker.PickRandomWithout(
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

        Assert.AreEqual(
            $"{address + offset}",
            DeviceConverter.GetOffsetAddress(decPrefix, address.ToString(), offset)
        );
    }

    [TestMethod]
    public void GetOffsetAddress_ShuldHexAddressOffset()
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
}
