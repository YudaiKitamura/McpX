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
        ' Create an McpX communication instance
        ' - IPアドレス: 192.168.12.88 (IP address)
        ' - ポート番号: 10000 (Port number)
        ' - ASCII形式通信: true (Use ASCII communication format)
        ' - UDP使用: false（= TCP通信） (Use TCP, not UDP)
        Using mcpx As New McpX("192.168.12.88", 10000, isAscii:=True, isUdp:=False)

            ' ビットデバイス M0 に bool 型で true を書き込む
            ' Write a value of type bool (true) to bit device M0
            Await mcpx.WriteAsync(Of Boolean)(Prefix.M, "0", True)

            ' ビットデバイス M0 の値を bool 型で読み取る
            ' Read a value of type bool from bit device M0
            Dim m0 As Boolean = Await mcpx.ReadAsync(Of Boolean)(Prefix.M, "0")
            Console.WriteLine($"M0: {m0}")

            ' ワードデバイス D0 に short 型で 32767 を書き込む
            ' Write a value of type short (32767) to word device D0
            Await mcpx.WriteAsync(Of Short)(Prefix.D, "0", CShort(32767))

            ' ワードデバイス D0 の値を short 型で読み取る
            ' Read a value of type short from word device D0
            Dim d0 As Short = Await mcpx.ReadAsync(Of Short)(Prefix.D, "0")
            Console.WriteLine($"D0: {d0}")

            ' ダブルワードデバイス D2 に int 型で 2147483647 を書き込む
            ' Write a value of type int (2147483647) to double word device D2
            Await mcpx.WriteAsync(Of Integer)(Prefix.D, "2", 2147483647)

            ' ダブルワードデバイス D2 の値を int 型で読み取る
            ' Read a value of type int from double word device D2
            Dim d2 As Integer = Await mcpx.ReadAsync(Of Integer)(Prefix.D, "2")
            Console.WriteLine($"D2: {d2}")

            ' ダブルワードデバイス D4 に float 型で最大値を設定して書き込む
            ' Write the maximum float value to double word device D4
            Await mcpx.WriteAsync(Of Single)(Prefix.D, "4", CSng(3.4028235E+38))

            ' D4 の値を float 型で読み取る（修正済）
            ' Read value from D4 as float (corrected)
            Dim d4 As Single = Await mcpx.ReadAsync(Of Single)(Prefix.D, "4")
            Console.WriteLine($"D4: {d4}")

            ' ダブルワードデバイス D6 に double 型で最大値を設定して書き込む
            ' Write the maximum double value to double word device D6
            Await mcpx.WriteAsync(Of Double)(Prefix.D, "6", 1.7976931348623157E+308)

            ' D6 の値を double 型で読み取る（修正済）
            ' Read value from D6 as double (corrected)
            Dim d6 As Double = Await mcpx.ReadAsync(Of Double)(Prefix.D, "6")
            Console.WriteLine($"D6: {d6}")

            ' 7000 ワード分の short 配列を D10 から一括書き込み
            ' Create a short array of 7000 words and write them starting from D10
            Dim dbwArr(6999) As Short
            For i As Integer = 0 To dbwArr.Length - 1
                dbwArr(i) = CShort(i)
            Next
            Await mcpx.BatchWriteAsync(Of Short)(Prefix.D, "10", dbwArr)

            ' D10 から 7000 ワード分を一括で short 型として読み取る
            ' Read 7000 words as short values starting from D10
            Dim dbrArr As Short() = Await mcpx.BatchReadAsync(Of Short)(Prefix.D, "10", 7000)

            Dim l As Integer = 0
            For Each dr In dbrArr
                Console.WriteLine($"D{10 + l}: {dr}")
                l += 1
            Next

            ' ランダムなワード/ダブルワードDeviceに short/int 型で書き込み
            ' Write to random word (short) and double word (int) device
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
            ' Read from random word (short) and double word (int) addresses
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
            ' Register specified word and double word devices for monitoring (required only once before Monitor call)
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
            ' Retrieve monitored values for registered devices (words as short, double words as int)
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
