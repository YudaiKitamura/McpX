# 使用例
## C# 非同期処理の例
```csharp
using McpXLib;
using McpXLib.Enums;


class Program
{
    static async Task Main(string[] args)
    {
        // McpX 通信インスタンスを生成
        // - IPアドレス: 192.168.12.88
        // - ポート番号: 10000
        // - ASCII形式通信: true
        // - UDP使用: false（TCP通信）
        using (var mcpx = new McpX("192.168.12.88", 10000, isAscii: true, isUdp: false))
        {
            // ビットデバイス M0 に bool 型で true を書き込む
            await mcpx.WriteAsync<bool>(Prefix.M, "0", true);

            // ビットデバイス M0 の値を bool 型で読み取る
            bool m0 = await mcpx.ReadAsync<bool>(Prefix.M, "0");

            Console.WriteLine($"M0: {m0}");


            // ワードデバイス D0 に short 型で 32767 を書き込む
            await mcpx.WriteAsync<short>(Prefix.D, "0", 32767);

            // ワードデバイス D0 の値を short 型で読み取る
            short d0 = await mcpx.ReadAsync<short>(Prefix.D, "0");

            Console.WriteLine($"D0: {d0}");


            // ダブルワードデバイス D2 に int 型で 2147483647 を書き込む
            await mcpx.WriteAsync<int>(Prefix.D, "2", 2147483647);

            // ダブルワードデバイス D2 の値を int 型で読み取る
            int d2 = await mcpx.ReadAsync<int>(Prefix.D, "2");

            Console.WriteLine($"D2: {d2}");


            // ダブルワードデバイス D4 に float 型で最大値を設定して書き込む
            await mcpx.WriteAsync<float>(Prefix.D, "4", (float)3.4028235E+38);

            // D4 の値を float 型で読み取る
            float d4 = await mcpx.ReadAsync<float>(Prefix.D, "4");

            Console.WriteLine($"D4: {d4}");


            // ダブルワードデバイス D6 に double 型で最大値を設定して書き込む
            await mcpx.WriteAsync<double>(Prefix.D, "6", (double)1.7976931348623157E+308);

            // D6 の値を double 型で読み取る
            double d6 = await mcpx.ReadAsync<double>(Prefix.D, "6");

            Console.WriteLine($"D6: {d6}");


            // 7000 ワード分の short 配列を D10 から一括書き込み
            short[] dbwArr = new short[7000];
            for (int i = 0; i < dbwArr.Length; i++)
            {
                dbwArr[i] = (short)i;
            }
            await mcpx.BatchWriteAsync<short>(Prefix.D, "10", dbwArr);

            // D10 から 7000 ワード分を一括で short 型として読み取る
            short[] dbrArr = await mcpx.BatchReadAsync<short>(Prefix.D, "10", 7000);

            int l = 0;
            foreach (var dr in dbrArr) 
            {
                Console.WriteLine($"D{ 10 + l }: { dr }");
                l++;
            }


            // ランダムなワード/ダブルワードDeviceに short/int 型で書き込み
            await mcpx.RandomWriteAsync<short, int>(
                wordDevices: [ 
                    (Prefix.D, "8000", 32766),
                    (Prefix.D, "8010", 32767)
                ],
                doubleWordDevices: [
                    (Prefix.D, "8020", 2147483646),
                    (Prefix.D, "8030", 2147483647) 
                ]
            );

            // ランダムなワード/ダブルワードアドレスから short/int 型で読み取り
            var drrArr = await mcpx.RandomReadAsync<short, int>(
                wordAddresses: [ 
                    (Prefix.D, "8000"),
                    (Prefix.D, "8010")
                ],
                doubleWordAddresses: [
                    (Prefix.D, "8020"),
                    (Prefix.D, "8030") 
                ]
            );

            Console.WriteLine($"D8000: { drrArr.wordValues[0] }");
            Console.WriteLine($"D8010: { drrArr.wordValues[1] }");

            Console.WriteLine($"D8020: { drrArr.doubleValues[0] }");
            Console.WriteLine($"D8030: { drrArr.doubleValues[1] }");

            
            // ワード・ダブルワードデバイスをモニタ登録する（初回のみ必要）
            await mcpx.MonitorRegistAsync(
                wordAddresses: [ 
                    (Prefix.D, "8000"),
                    (Prefix.D, "8010")
                ],
                doubleWordAddresses: [
                    (Prefix.D, "8020"),
                    (Prefix.D, "8030") 
                ]
            );

            // モニタ登録したデバイスの値を取得する（ワード: short 型、ダブルワード: int 型）
            var dmArr = await mcpx.MonitorAsync<short, int>(
                wordAddresses: [ 
                    (Prefix.D, "8000"),
                    (Prefix.D, "8010")
                ],
                doubleWordAddresses: [
                    (Prefix.D, "8020"),
                    (Prefix.D, "8030") 
                ]
            );

            Console.WriteLine($"D8000: { dmArr.wordValues[0] }");
            Console.WriteLine($"D8010: { dmArr.wordValues[1] }");

            Console.WriteLine($"D8020: { dmArr.doubleValues[0] }");
            Console.WriteLine($"D8030: { dmArr.doubleValues[1] }");
        }
    }
}
```

## C# 同期処理の例
```csharp
using McpXLib;
using McpXLib.Enums;


class Program
{
    static void Main(string[] args)
    {
        // McpX 通信インスタンスを生成
        // - IPアドレス: 192.168.12.88
        // - ポート番号: 10000
        // - ASCII形式通信: true
        // - UDP使用: false（TCP通信）
        using (var mcpx = new McpX("192.168.12.88", 10000, isAscii: true, isUdp: false))
        {
            // ビットデバイス M0 に bool 型で true を書き込む
            mcpx.Write<bool>(Prefix.M, "0", true);

            // ビットデバイス M0 の値を bool 型で読み取る
            bool m0 = mcpx.Read<bool>(Prefix.M, "0");

            Console.WriteLine($"M0: {m0}");


            // ワードデバイス D0 に short 型で 32767 を書き込む
            mcpx.Write<short>(Prefix.D, "0", 32767);

            // ワードデバイス D0 の値を short 型で読み取る
            short d0 = mcpx.Read<short>(Prefix.D, "0");

            Console.WriteLine($"D0: {d0}");


            // ダブルワードデバイス D2 に int 型で 2147483647 を書き込む
            mcpx.Write<int>(Prefix.D, "2", 2147483647);

            // ダブルワードデバイス D2 の値を int 型で読み取る
            int d2 = mcpx.Read<int>(Prefix.D, "2");

            Console.WriteLine($"D2: {d2}");


            // ダブルワードデバイス D4 に float 型で最大値を設定して書き込む
            mcpx.Write<float>(Prefix.D, "4", (float)3.4028235E+38);

            // D4 の値を float 型で読み取る
            float d4 = mcpx.Read<float>(Prefix.D, "4");

            Console.WriteLine($"D4: {d4}");


            // ダブルワードデバイス D6 に double 型で最大値を設定して書き込む
            mcpx.Write<double>(Prefix.D, "6", (double)1.7976931348623157E+308);

            // D6 の値を double 型で読み取る
            double d6 = mcpx.Read<double>(Prefix.D, "6");

            Console.WriteLine($"D6: {d6}");


            // 7000 ワード分の short 配列を D10 から一括書き込み
            short[] dbwArr = new short[7000];
            for (int i = 0; i < dbwArr.Length; i++)
            {
                dbwArr[i] = (short)i;
            }
            mcpx.BatchWrite<short>(Prefix.D, "10", dbwArr);

            // D10 から 7000 ワード分を一括で short 型として読み取る
            short[] dbrArr = mcpx.BatchRead<short>(Prefix.D, "10", 7000);

            int l = 0;
            foreach (var dr in dbrArr) 
            {
                Console.WriteLine($"D{ 10 + l }: { dr }");
                l++;
            }


            // ランダムなワード/ダブルワードDeviceに short/int 型で書き込み
            mcpx.RandomWrite<short, int>(
                wordDevices: [ 
                    (Prefix.D, "8000", 32766),
                    (Prefix.D, "8010", 32767)
                ],
                doubleWordDevices: [
                    (Prefix.D, "8020", 2147483646),
                    (Prefix.D, "8030", 2147483647) 
                ]
            );

            // ランダムなワード/ダブルワードアドレスから short/int 型で読み取り
            var drrArr = mcpx.RandomRead<short, int>(
                wordAddresses: [ 
                    (Prefix.D, "8000"),
                    (Prefix.D, "8010")
                ],
                doubleWordAddresses: [
                    (Prefix.D, "8020"),
                    (Prefix.D, "8030") 
                ]
            );

            Console.WriteLine($"D8000: { drrArr.wordValues[0] }");
            Console.WriteLine($"D8010: { drrArr.wordValues[1] }");

            Console.WriteLine($"D8020: { drrArr.doubleValues[0] }");
            Console.WriteLine($"D8030: { drrArr.doubleValues[1] }");

            
            // ワード・ダブルワードデバイスをモニタ登録する（初回のみ必要）
            mcpx.MonitorRegist(
                wordAddresses: [ 
                    (Prefix.D, "8000"),
                    (Prefix.D, "8010")
                ],
                doubleWordAddresses: [
                    (Prefix.D, "8020"),
                    (Prefix.D, "8030") 
                ]
            );

            // モニタ登録したデバイスの値を取得する（ワード: short 型、ダブルワード: int 型）
            var dmArr = mcpx.Monitor<short, int>(
                wordAddresses: [ 
                    (Prefix.D, "8000"),
                    (Prefix.D, "8010")
                ],
                doubleWordAddresses: [
                    (Prefix.D, "8020"),
                    (Prefix.D, "8030") 
                ]
            );

            Console.WriteLine($"D8000: { dmArr.wordValues[0] }");
            Console.WriteLine($"D8010: { dmArr.wordValues[1] }");

            Console.WriteLine($"D8020: { dmArr.doubleValues[0] }");
            Console.WriteLine($"D8030: { dmArr.doubleValues[1] }");
        }
    }
}
```

## VisualBasic 非同期処理の例
```vb
Imports System
Imports System.Threading.Tasks
Imports McpXLib
Imports McpXLib.Enums

Module Program
    Sub Main()
        RunAsync().Wait()
    End Sub
    Async Function RunAsync() As Task
        ' McpX 通信インスタンスを生成
        ' - IPアドレス: 192.168.12.88
        ' - ポート番号: 10000
        ' - ASCII形式通信: true
        ' - UDP使用: false（TCP通信）
        Using mcpx As New McpX("192.168.12.88", 10000, isAscii:=True, isUdp:=False)

            ' ビットデバイス M0 に bool 型で true を書き込む
            Await mcpx.WriteAsync(Of Boolean)(Prefix.M, "0", True)

            ' ビットデバイス M0 の値を bool 型で読み取る
            Dim m0 As Boolean = Await mcpx.ReadAsync(Of Boolean)(Prefix.M, "0")
            Console.WriteLine($"M0: {m0}")

            ' ワードデバイス D0 に short 型で 32767 を書き込む
            Await mcpx.WriteAsync(Of Short)(Prefix.D, "0", CShort(32767))

            ' ワードデバイス D0 の値を short 型で読み取る
            Dim d0 As Short = Await mcpx.ReadAsync(Of Short)(Prefix.D, "0")
            Console.WriteLine($"D0: {d0}")

            ' ダブルワードデバイス D2 に int 型で 2147483647 を書き込む
            Await mcpx.WriteAsync(Of Integer)(Prefix.D, "2", 2147483647)

            ' ダブルワードデバイス D2 の値を int 型で読み取る
            Dim d2 As Integer = Await mcpx.ReadAsync(Of Integer)(Prefix.D, "2")
            Console.WriteLine($"D2: {d2}")

            ' ダブルワードデバイス D4 に float 型で最大値を設定して書き込む
            Await mcpx.WriteAsync(Of Single)(Prefix.D, "4", CSng(3.4028235E+38))

            ' D4 の値を float 型で読み取る
            Dim d4 As Single = Await mcpx.ReadAsync(Of Single)(Prefix.D, "4")
            Console.WriteLine($"D4: {d4}")

            ' ダブルワードデバイス D6 に double 型で最大値を設定して書き込む
            Await mcpx.WriteAsync(Of Double)(Prefix.D, "6", 1.7976931348623157E+308)

            ' D6 の値を double 型で読み取る
            Dim d6 As Double = Await mcpx.ReadAsync(Of Double)(Prefix.D, "6")
            Console.WriteLine($"D6: {d6}")

            ' 7000 ワード分の short 配列を D10 から一括書き込み
            Dim dbwArr(6999) As Short
            For i As Integer = 0 To dbwArr.Length - 1
                dbwArr(i) = CShort(i)
            Next
            Await mcpx.BatchWriteAsync(Of Short)(Prefix.D, "10", dbwArr)

            ' D10 から 7000 ワード分を一括で short 型として読み取る
            Dim dbrArr As Short() = Await mcpx.BatchReadAsync(Of Short)(Prefix.D, "10", 7000)

            Dim l As Integer = 0
            For Each dr In dbrArr
                Console.WriteLine($"D{10 + l}: {dr}")
                l += 1
            Next

            ' ランダムなワード/ダブルワードDeviceに short/int 型で書き込み
            Await mcpx.RandomWriteAsync(Of Short, Integer)(
                wordDevices:={
                    New ValueTuple(Of Prefix, String, Short)(Prefix.D, "8000", 32766),
                    New ValueTuple(Of Prefix, String, Short)(Prefix.D, "8010", 32767)
                },
                doubleWordDevices:={
                    New ValueTuple(Of Prefix, String, Integer)(Prefix.D, "8020", 2147483646),
                    New ValueTuple(Of Prefix, String, Integer)(Prefix.D, "8030", 2147483647)
                }
            )

            ' ランダムなワード/ダブルワードアドレスから short/int 型で読み取り
            Dim drrArr = Await mcpx.RandomReadAsync(Of Short, Integer)(
                wordAddresses:={
                    New ValueTuple(Of Prefix, String)(Prefix.D, "8000"),
                    New ValueTuple(Of Prefix, String)(Prefix.D, "8010")
                },
                doubleWordAddresses:={
                    New ValueTuple(Of Prefix, String)(Prefix.D, "8020"),
                    New ValueTuple(Of Prefix, String)(Prefix.D, "8030")
                }
            )

            Console.WriteLine($"D8000: {drrArr.wordValues(0)}")
            Console.WriteLine($"D8010: {drrArr.wordValues(1)}")
            Console.WriteLine($"D8020: {drrArr.doubleValues(0)}")
            Console.WriteLine($"D8030: {drrArr.doubleValues(1)}")

            ' ワード・ダブルワードデバイスをモニタ登録する（初回のみ必要）
            Await mcpx.MonitorRegistAsync(
                wordAddresses:={
                    New ValueTuple(Of Prefix, String)(Prefix.D, "8000"),
                    New ValueTuple(Of Prefix, String)(Prefix.D, "8010")
                },
                doubleWordAddresses:={
                    New ValueTuple(Of Prefix, String)(Prefix.D, "8020"),
                    New ValueTuple(Of Prefix, String)(Prefix.D, "8030")
                }
            )

            ' モニタ登録したデバイスの値を取得する（ワード: short 型、ダブルワード: int 型）
            Dim dmArr = Await mcpx.MonitorAsync(Of Short, Integer)(
                wordAddresses:={
                    New ValueTuple(Of Prefix, String)(Prefix.D, "8000"),
                    New ValueTuple(Of Prefix, String)(Prefix.D, "8010")
                },
                doubleWordAddresses:={
                    New ValueTuple(Of Prefix, String)(Prefix.D, "8020"),
                    New ValueTuple(Of Prefix, String)(Prefix.D, "8030")
                }
            )

            Console.WriteLine($"D8000: {dmArr.wordValues(0)}")
            Console.WriteLine($"D8010: {dmArr.wordValues(1)}")
            Console.WriteLine($"D8020: {dmArr.doubleValues(0)}")
            Console.WriteLine($"D8030: {dmArr.doubleValues(1)}")

        End Using
    End Function
End Module
```

## VisualBasic 同期処理の例
```vb
Imports McpXLib
Imports McpXLib.Enums

Module Program
    Sub Main()
        ' McpX 通信インスタンスを生成
        ' - IPアドレス: 192.168.12.88
        ' - ポート番号: 10000
        ' - ASCII形式通信: true
        ' - UDP使用: false（TCP通信）
        Using mcpx As New McpX("192.168.12.88", 10000, isAscii:=True, isUdp:=False)

            ' ビットデバイス M0 に bool 型で true を書き込む
            mcpx.Write(Of Boolean)(Prefix.M, "0", True)

            ' ビットデバイス M0 の値を bool 型で読み取る
            Dim m0 As Boolean = mcpx.Read(Of Boolean)(Prefix.M, "0")
            Console.WriteLine($"M0: {m0}")

            ' ワードデバイス D0 に short 型で 32767 を書き込む
            mcpx.Write(Of Short)(Prefix.D, "0", 32767)

            ' ワードデバイス D0 の値を short 型で読み取る
            Dim d0 As Short = mcpx.Read(Of Short)(Prefix.D, "0")
            Console.WriteLine($"D0: {d0}")

            ' ダブルワードデバイス D2 に int 型で 2147483647 を書き込む
            mcpx.Write(Of Integer)(Prefix.D, "2", 2147483647)

            ' ダブルワードデバイス D2 の値を int 型で読み取る
            Dim d2 As Integer = mcpx.Read(Of Integer)(Prefix.D, "2")
            Console.WriteLine($"D2: {d2}")

            ' ダブルワードデバイス D4 に float 型で最大値を設定して書き込む
            mcpx.Write(Of Single)(Prefix.D, "4", CSng(3.4028235E+38))

            ' D4 の値を float 型で読み取る
            Dim d4 As Single = mcpx.Read(Of Single)(Prefix.D, "4")
            Console.WriteLine($"D4: {d4}")

            ' ダブルワードデバイス D6 に double 型で最大値を設定して書き込む
            mcpx.Write(Of Double)(Prefix.D, "6", 1.7976931348623157E+308)

            ' D6 の値を double 型で読み取る
            Dim d6 As Double = mcpx.Read(Of Double)(Prefix.D, "6")
            Console.WriteLine($"D6: {d6}")

            ' 7000 ワード分の short 配列を D10 から一括書き込み
            Dim dbwArr(6999) As Short
            For i As Integer = 0 To dbwArr.Length - 1
                dbwArr(i) = CShort(i)
            Next
            mcpx.BatchWrite(Of Short)(Prefix.D, "10", dbwArr)

            ' D10 から 7000 ワード分を一括で short 型として読み取る
            Dim dbrArr As Short() = mcpx.BatchRead(Of Short)(Prefix.D, "10", 7000)

            Dim l As Integer = 0
            For Each dr In dbrArr
                Console.WriteLine($"D{10 + l}: {dr}")
                l += 1
            Next

            ' ランダムなワード/ダブルワードDeviceに short/int 型で書き込み
            mcpx.RandomWrite(Of Short, Integer)(
                wordDevices:={
                    New ValueTuple(Of Prefix, String, Short)(Prefix.D, "8000", 32766),
                    New ValueTuple(Of Prefix, String, Short)(Prefix.D, "8010", 32767)
                },
                doubleWordDevices:={
                    New ValueTuple(Of Prefix, String, Integer)(Prefix.D, "8020", 2147483646),
                    New ValueTuple(Of Prefix, String, Integer)(Prefix.D, "8030", 2147483647)
                }
            )

            ' ランダムなワード/ダブルワードアドレスから short/int 型で読み取り
            Dim drrArr = mcpx.RandomRead(Of Short, Integer)(
                wordAddresses:={
                    New ValueTuple(Of Prefix, String)(Prefix.D, "8000"),
                    New ValueTuple(Of Prefix, String)(Prefix.D, "8010")
                },
                doubleWordAddresses:={
                    New ValueTuple(Of Prefix, String)(Prefix.D, "8020"),
                    New ValueTuple(Of Prefix, String)(Prefix.D, "8030")
                }
            )

            Console.WriteLine($"D8000: {drrArr.wordValues(0)}")
            Console.WriteLine($"D8010: {drrArr.wordValues(1)}")
            Console.WriteLine($"D8020: {drrArr.doubleValues(0)}")
            Console.WriteLine($"D8030: {drrArr.doubleValues(1)}")

            ' ワード・ダブルワードデバイスをモニタ登録する（初回のみ必要）
            mcpx.MonitorRegist(
                wordAddresses:={
                    New ValueTuple(Of Prefix, String)(Prefix.D, "8000"),
                    New ValueTuple(Of Prefix, String)(Prefix.D, "8010")
                },
                doubleWordAddresses:={
                    New ValueTuple(Of Prefix, String)(Prefix.D, "8020"),
                    New ValueTuple(Of Prefix, String)(Prefix.D, "8030")
                }
            )

            ' モニタ登録したデバイスの値を取得する（ワード: short 型、ダブルワード: int 型）
            Dim dmArr = mcpx.Monitor(Of Short, Integer)(
                wordAddresses:={
                    New ValueTuple(Of Prefix, String)(Prefix.D, "8000"),
                    New ValueTuple(Of Prefix, String)(Prefix.D, "8010")
                },
                doubleWordAddresses:={
                    New ValueTuple(Of Prefix, String)(Prefix.D, "8020"),
                    New ValueTuple(Of Prefix, String)(Prefix.D, "8030")
                }
            )

            Console.WriteLine($"D8000: {dmArr.wordValues(0)}")
            Console.WriteLine($"D8010: {dmArr.wordValues(1)}")
            Console.WriteLine($"D8020: {dmArr.doubleValues(0)}")
            Console.WriteLine($"D8030: {dmArr.doubleValues(1)}")

        End Using
    End Sub
End Module
```