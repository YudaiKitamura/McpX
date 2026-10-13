Imports System
Imports McpXLib
Imports McpXLib.Enums

Module Program
    Sub Main()
        ' PLC に接続する（IPアドレス・ポート番号は PLC のパラメータ設定に合わせてください）
        ' Connect to the PLC (match the IP address and port to the PLC parameters)
        Dim options As New McpXOptions With {
            .Host = "192.168.12.88",
            .Port = 10000
        }
        ' options.ProcessorSeries = ProcessorSeries.iQR   ' MELSEC iQ-R シリーズの場合 / for the MELSEC iQ-R series
        ' options.IsAscii = True                          ' ASCII コード交信の場合 / for ASCII code

        Using mcpx As McpX = McpX.Connect(options)

            ' 単一デバイスの読み書き。型に応じてデバイス点数は自動で決まる
            ' Single device access. The number of words follows the type
            mcpx.Write(Of Boolean)(Prefix.M, "0", True)
            mcpx.Write(Of Short)(Prefix.D, "0", CShort(32767))
            mcpx.Write(Of Integer)(Prefix.D, "2", 2147483647)

            Console.WriteLine($"M0: {mcpx.Read(Of Boolean)(Prefix.M, "0")}")
            Console.WriteLine($"D0: {mcpx.Read(Of Short)(Prefix.D, "0")}")
            Console.WriteLine($"D2: {mcpx.Read(Of Integer)(Prefix.D, "2")}")

            ' 連続デバイスの一括読み書き（上限を超える場合は自動で分割する）
            ' Batch access (requests over the limit are split automatically)
            Dim values(1999) As Short
            For i As Integer = 0 To values.Length - 1
                values(i) = CShort(i)
            Next
            mcpx.BatchWrite(Of Short)(Prefix.D, "100", values)

            Dim read As Short() = mcpx.BatchRead(Of Short)(Prefix.D, "100", values.Length)
            Console.WriteLine($"D100-D2099: {read(0)} ... {read(read.Length - 1)}")

            ' ランダムアクセス
            ' Random access
            mcpx.RandomWrite(Sub(b) b.Add(Of Short)(Prefix.D, "8000", CShort(100)).Add(Of Integer)(Prefix.D, "8010", 200000))

            mcpx.RandomRead(Sub(b) b _
                .Add(Of Short)(Prefix.D, "8000", Sub(v) Console.WriteLine($"D8000: {v}")) _
                .Add(Of Integer)(Prefix.D, "8010", Sub(v) Console.WriteLine($"D8010: {v}")))

            ' モニタ：一度登録したデバイスを、繰り返し読み出す
            ' Monitor: register devices once, then read them repeatedly
            Dim session = mcpx.MonitorRegister(Sub(b) b _
                .Add(Of Short)(Prefix.D, "8000", Sub(v) Console.WriteLine($"D8000: {v}")) _
                .Add(Of Integer)(Prefix.D, "8010", Sub(v) Console.WriteLine($"D8010: {v}")))

            For i As Integer = 1 To 3
                session.Read()
            Next

            ' 文字列（Shift_JIS）の読み書き。読み込みはワード数で指定する
            ' Strings (Shift_JIS). Specify the read length in words
            mcpx.WriteString(Prefix.D, "9000", "McpX")
            Console.WriteLine($"D9000: {mcpx.ReadString(Prefix.D, "9000", 4)}")

        End Using
    End Sub
End Module
