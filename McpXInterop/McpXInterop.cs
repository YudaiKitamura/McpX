using System.Runtime.InteropServices;
using McpXLib;
using McpXLib.Exceptions;
using McpXLib.Enums;

namespace McpXInterop;

[StructLayout(LayoutKind.Sequential)]
public struct Device
{
    public Prefix Prefix;
    public IntPtr Address;

    public string GetAddressString()
    {
        return Marshal.PtrToStringAnsi(Address)!;
    } 
}

public static class McpXInterop
{
    private static int nextId = 1;
    private static readonly object lockObj = new();
    private static readonly Dictionary<int, McpX> connections = new();

    [UnmanagedCallersOnly(EntryPoint = "plc_connect")]
    public static int Connect(
        IntPtr ipPtr,
        int port,
        IntPtr passwordPtr,
        bool isAscii,
        bool isUdp,
        RequestFrame requestFrame)
    {
        string ip = Marshal.PtrToStringAnsi(ipPtr)!;
        string password = Marshal.PtrToStringAnsi(passwordPtr)!;

        var mcpx = new McpX(
            ip: ip,
            port: port,
            password: password, 
            isAscii: isAscii,
            isUdp: isUdp,
            requestFrame: requestFrame
        );

        lock (lockObj)
        {
            int id = nextId++;
            connections[id] = mcpx;
            return id;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "plc_close")]
    public static void Close(int connectionId)
    {
        lock (lockObj)
        {
            if (!connections.TryGetValue(connectionId, out var mcpx))
                throw new InvalidOperationException("Invalid connection ID aaa");

            mcpx.Dispose();
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "batch_read_short")]
    public static int BatchReadShort(int connectionId, IntPtr outValues, Device device, int length)
    {
        return BatchRead<short>(connectionId, outValues, device, length);
    }

    [UnmanagedCallersOnly(EntryPoint = "batch_read_int")]
    public static int BatchReadInt(int connectionId, IntPtr outValues, Device device, int length)
    {
        return BatchRead<int>(connectionId, outValues, device, length);
    }

    [UnmanagedCallersOnly(EntryPoint = "batch_read_float")]
    public static int BatchReadFloat(int connectionId, IntPtr outValues, Device device, int length)
    {
        return BatchRead<float>(connectionId, outValues, device, length);
    }

    [UnmanagedCallersOnly(EntryPoint = "batch_read_bool")]
    public static int BatchReadBool(int connectionId, IntPtr outValues, Device device, int length)
    {
        return BatchRead<bool>(connectionId, outValues, device, length);
    }

    [UnmanagedCallersOnly(EntryPoint = "batch_write_short")]
    public static int BatchWriteShort(int connectionId, IntPtr outValues, Device device, int length)
    {
        return BatchWrite<short>(connectionId, outValues, device, length);
    }

    [UnmanagedCallersOnly(EntryPoint = "batch_write_int")]
    public static int BatchWriteInt(int connectionId, IntPtr outValues, Device device, int length)
    {
        return BatchWrite<int>(connectionId, outValues, device, length);
    }

    [UnmanagedCallersOnly(EntryPoint = "batch_write_float")]
    public static int BatchWriteFloat(int connectionId, IntPtr outValues, Device device, int length)
    {
        return BatchWrite<float>(connectionId, outValues, device, length);
    }

    [UnmanagedCallersOnly(EntryPoint = "batch_write_bool")]
    public static int BatchWriteBool(int connectionId, IntPtr outValues, Device device, int length)
    {
        return BatchWrite<short>(connectionId, outValues, device, length);
    }

    private static int BatchRead<T>(int connectionId, IntPtr outValues, Device device, int length) where T : unmanaged
    {
        lock (lockObj)
        {
            if (!connections.TryGetValue(connectionId, out var mcpx))
                throw new InvalidOperationException("Invalid connection ID");

            try
            {
                SetValues<T>(
                    inValues: mcpx.BatchRead<T>(device.Prefix, device.GetAddressString(), (ushort)length),
                    outValues: outValues
                );
            }
            catch (DeviceAddressException)
            {
                return -1;
            }
            catch (RecivePacketException)
            {
                return -2;
            }
            catch (McProtocolException)
            {
                return -3;
            }

            return 0;
        }
    }

    private static int BatchWrite<T>(int connectionId, IntPtr outValues, Device device, int length) where T : unmanaged
    {
        lock (lockObj)
        {
            if (!connections.TryGetValue(connectionId, out var mcpx))
                throw new InvalidOperationException("Invalid connection ID");

            try
            {
                mcpx.BatchWrite<T>(device.Prefix, device.GetAddressString(), GetValues<T>(outValues, length));
            }
            catch (DeviceAddressException)
            {
                return -1;
            }
            catch (RecivePacketException)
            {
                return -2;
            }
            catch (McProtocolException)
            {
                return -3;
            }

            return 0;
        }
    }


    private static void SetValues<T>(T[] inValues, IntPtr outValues) where T : unmanaged
    {
        unsafe
        {
            T* dst = (T*)outValues;
            for (int i = 0; i < inValues.Count(); i++)
            {
                dst[i] = inValues[i];
            }
        }
    }

    private static T[] GetValues<T>(IntPtr inValues, int length) where T : unmanaged
    {
        T[] result = new T[length];

        unsafe
        {
            T* src = (T*)inValues;
            for (int i = 0; i < length; i++)
            {
                result[i] = src[i];
            }
        }

        return result;
    }
}
