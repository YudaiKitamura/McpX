using System.Text.RegularExpressions;
using McpXLib.Enums;
using McpXLib.Exceptions;


namespace McpXLib.Utils;
public static class DeviceConverter
{
    private static readonly Dictionary<Type, int> wordLengths = new()
    {
        { typeof(bool), 1 },
        { typeof(short), 1 },
        { typeof(ushort), 1 },
        { typeof(int), 2 },
        { typeof(uint), 2 },
        { typeof(float), 2 },
        { typeof(double), 4 },
        { typeof(long), 4 },
        { typeof(ulong), 4 }
    };

    public static T[] ConvertValueArray<T>(byte[] bytes) where T : unmanaged
    {
        int byteLength = typeof(T) != typeof(bool) && typeof(T) != typeof(byte) ? GetWordLength<T>() * 2 : 1;
        if (bytes.Length % byteLength != 0)
        {
            throw new ArgumentException("Byte array is not the correct length.");
        }

        var length = bytes.Length / byteLength;
        var values = new T[length];

        for (int i = 0; i < length; i++) 
        {
            if (typeof(T) == typeof(bool)) 
            {
                values[i] = (T)(object)BitConverter.ToBoolean(bytes.Skip(i).Take(1).ToArray(), 0);
            }
            else if (typeof(T) == typeof(short)) 
            {
                values[i] = (T)(object)BitConverter.ToInt16(bytes.Skip(i * byteLength).Take(byteLength).ToArray(), 0);
            }
            else if (typeof(T) == typeof(ushort)) 
            {
                values[i] = (T)(object)BitConverter.ToUInt16(bytes.Skip(i * byteLength).Take(byteLength).ToArray(), 0);
            }
            else if (typeof(T) == typeof(int)) 
            {
                values[i] = (T)(object)BitConverter.ToInt32(bytes.Skip(i * byteLength).Take(byteLength).ToArray(), 0);
            }
            else if (typeof(T) == typeof(uint)) 
            {
                values[i] = (T)(object)BitConverter.ToUInt32(bytes.Skip(i * byteLength).Take(byteLength).ToArray(), 0);
            }
            else if (typeof(T) == typeof(float)) 
            {
                values[i] = (T)(object)BitConverter.ToSingle(bytes.Skip(i * byteLength).Take(byteLength).ToArray(), 0);
            }
            else if (typeof(T) == typeof(double)) 
            {
                values[i] = (T)(object)BitConverter.ToDouble(bytes.Skip(i * byteLength).Take(byteLength).ToArray(), 0);
            }
            else if (typeof(T) == typeof(long)) 
            {
                values[i] = (T)(object)BitConverter.ToInt64(bytes.Skip(i * byteLength).Take(byteLength).ToArray(), 0);
            }
            else if (typeof(T) == typeof(ulong)) 
            {
                values[i] = (T)(object)BitConverter.ToUInt64(bytes.Skip(i * byteLength).Take(byteLength).ToArray(), 0);
            }
            else if (typeof(T) == typeof(byte)) 
            {
                return (T[])(object)bytes;
            }   
        }

        return values;
    }

    public static int GetWordLength<T>() where T : unmanaged
    {
        if (wordLengths.TryGetValue(typeof(T), out int length))
        {
            return length;
        }
        
        throw new NotSupportedException("Type {typeof(T)} is not supported.");
    }

    public static byte[] ConvertByteValueArray<T>(T[] values) where T : unmanaged
    {
        var bytes = new List<byte>();
        foreach (var value in values) 
        {
            bytes.AddRange(StructToBytes(value));
        }

        return bytes.ToArray();
    }

    public static byte[] ToByteAddress(Prefix prefix, string address)
    {
        if (!ValidateAddress(prefix, address)) 
        {
            throw new DeviceAddressException($"{prefix}{address} is invalid.");
        }

        byte[] bytes;

        if (!IsHexDevice(prefix))
        {
            bytes = BitConverter.GetBytes(uint.Parse(address));
            bytes[bytes.Length - 1] = (byte)prefix;
            return bytes;
        }

        string hex = address.Length == 1 ? address.PadLeft(2, '0') : address;
        hex = hex.PadRight(6, '0');

        bytes = Enumerable.Range(0, hex.Length / 2)
            .Select(i => Convert.ToByte(hex.Substring(i * 2, 2), 16))
            .ToArray();

        Array.Resize(ref bytes, bytes.Length + 1);
        bytes[bytes.Length - 1] = (byte)prefix;

        return bytes;
    }

    public static string ToASCIIAddress(Prefix prefix, string address)
    {
        if (!ValidateAddress(prefix, address)) 
        {
            throw new DeviceAddressException($"{prefix}{address} is invalid.");
        }

        string prefixStr = prefix.ToString();
        prefixStr = prefixStr.Length == 1 ? prefixStr + "*" : prefixStr;
        string addressStr = address.PadLeft(6, '0');

        return prefixStr + addressStr;
    }

    public static string GetOffsetAddress(Prefix prefix, string address, int offset)
    {
        if (!ValidateAddress(prefix, address))
        {
            throw new DeviceAddressException($"{prefix}{address} is invalid.");
        }

        uint nextDeviceDecAddress;
        if (!IsHexDevice(prefix))
        {
            if (uint.Parse(address) + offset < 0) 
            {
                throw new OverflowException($"Prefix:{prefix} Address:{address} Offet:{offset}");
            }
            nextDeviceDecAddress = (uint)(uint.Parse(address) + offset);
            return nextDeviceDecAddress.ToString();
        }
        else 
        {
            uint decAddress = Convert.ToUInt32(address, 16);
            if (decAddress + offset < 0) 
            {
                throw new OverflowException($"Prefix:{prefix} Address:{address} DecAddress:{decAddress} Offet:{offset}");
            }
            nextDeviceDecAddress = (uint)(decAddress + offset);
            return nextDeviceDecAddress.ToString("X"); 
        }
    }

    public static byte[] StructToBytes<T>(T value) where T : unmanaged
    {
        if (typeof(T) == typeof(bool))
        {
            return (bool)(object)value ? [0x01] : [0x00];
        }
        else if (typeof(T) == typeof(short))
        {
            return BitConverter.GetBytes((short)(object)value);
        }
        else if (typeof(T) == typeof(ushort))
        {
            return BitConverter.GetBytes((ushort)(object)value);
        }
        else if (typeof(T) == typeof(int))
        {
            return BitConverter.GetBytes((int)(object)value);
        }
        else if (typeof(T) == typeof(uint))
        {
            return BitConverter.GetBytes((uint)(object)value);
        }
        else if (typeof(T) == typeof(long))
        {
            return BitConverter.GetBytes((long)(object)value);
        }
        else if (typeof(T) == typeof(ulong))
        {
            return BitConverter.GetBytes((ulong)(object)value);
        }
        else if (typeof(T) == typeof(float))
        {
            return BitConverter.GetBytes((float)(object)value);
        }
        else if (typeof(T) == typeof(double))
        {
            return BitConverter.GetBytes((double)(object)value);
        }

        throw new NotSupportedException($"Type {typeof(T)} is not supported.");
    }

    public static byte[] ReverseByTwoBytes(byte[] input)
    {
        if (input.Length % 2 != 0)
            throw new ArgumentException("Length of the array must be a multiple of 2.");

        byte[] result = new byte[input.Length];
        for (int i = 0; i < input.Length; i += 2)
        {
            result[i] = input[i + 1];
            result[i + 1] = input[i];
        }
        return result;
    }

    private static bool ValidateAddress(Prefix prefix, string address) 
    {
        if (!IsHexDevice(prefix))
        {
            return uint.TryParse(address, out _);
        }
        else
        {
            return Regex.IsMatch(address, @"^[0-9A-Fa-f]+$");
        }
    }

    private static bool IsHexDevice(Prefix prefix)
    {
        return prefix == Prefix.X ||
            prefix == Prefix.Y ||
            prefix == Prefix.B ||
            prefix == Prefix.W ||
            prefix == Prefix.SB ||
            prefix == Prefix.SW ||
            prefix == Prefix.DX ||
            prefix == Prefix.DY;
    }
}
