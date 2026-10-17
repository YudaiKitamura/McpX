Imports System
Imports System.Threading.Tasks
Imports McpXLib
Imports McpXLib.Enums

Module Program
    Sub Main()
        RunAsync().GetAwaiter().GetResult()
    End Sub

    Async Function RunAsync() As Task
        ' PLC に非同期で接続する（スレッドを止めずに接続する）
        ' Connect to the PLC asynchronously (without blocking a thread)
        Dim options As New McpXOptions With {
            .Host = "192.168.12.88",
            .Port = 10000
        }
        ' Using を抜けると接続を閉じる（VB には Await Using がないため、Dispose で閉じる）
        ' The connection is closed at End Using (VB has no Await Using, so Dispose closes it)
        Using mcpx As McpX = Await McpX.ConnectAsync(options)
            ' 単一デバイスの読み書き
            ' Single device access
            Await mcpx.WriteAsync(Of Boolean)(Prefix.M, "0", True)
            Await mcpx.WriteAsync(Of Short)(Prefix.D, "0", CShort(32767))

            Console.WriteLine($"M0: {Await mcpx.ReadAsync(Of Boolean)(Prefix.M, "0")}")
            Console.WriteLine($"D0: {Await mcpx.ReadAsync(Of Short)(Prefix.D, "0")}")

            ' 連続デバイスの一括読み書き（上限を超える場合は自動で分割する）
            ' Batch access (requests over the limit are split automatically)
            Dim values(1999) As Short
            For i As Integer = 0 To values.Length - 1
                values(i) = CShort(i)
            Next
            Await mcpx.BatchWriteAsync(Of Short)(Prefix.D, "100", values)

            Dim read As Short() = Await mcpx.BatchReadAsync(Of Short)(Prefix.D, "100", values.Length)
            Console.WriteLine($"D100-D2099: {read(0)} ... {read(read.Length - 1)}")

            ' ランダムアクセス
            ' Random access
            Await mcpx.RandomReadAsync(Sub(b) b _
                .Add(Of Short)(Prefix.D, "8000", Sub(v) Console.WriteLine($"D8000: {v}")) _
                .Add(Of Integer)(Prefix.D, "8010", Sub(v) Console.WriteLine($"D8010: {v}")))

            ' モニタ
            ' Monitor
            Dim session = Await mcpx.MonitorRegisterAsync(Sub(b) b _
                .Add(Of Short)(Prefix.D, "8000", Sub(v) Console.WriteLine($"D8000: {v}")))

            For i As Integer = 1 To 3
                Await session.ReadAsync()
                Await Task.Delay(1000)
            Next
        End Using
    End Function
End Module
