using McpXLib;
using McpXLib.Enums;


class Program
{
    static async Task Main(string[] args)
    {
        // McpX 通信インスタンスを生成
        // Create an McpX communication instance
        // - IPアドレス: 192.168.12.88 (IP address)
        // - ポート番号: 10000 (Port number)
        // - ASCII形式通信: true (Use ASCII communication format)
        // - UDP使用: false（= TCP通信） (Use TCP, not UDP)
        using (var mcpx = new McpX("192.168.12.88", 10000, isAscii: true, isUdp: false))
        {
            // ビットデバイス M0 に bool 型で true を書き込む
            // Write a value of type bool (true) to bit device M0
            await mcpx.WriteAsync<bool>(Prefix.M, "0", true);

            // ビットデバイス M0 の値を bool 型で読み取る
            // Read a value of type bool from bit device M0
            bool m0 = await mcpx.ReadAsync<bool>(Prefix.M, "0");

            Console.WriteLine($"M0: {m0}");


            // ワードデバイス D0 に short 型で 32767 を書き込む
            // Write a value of type short (32767) to word device D0
            await mcpx.WriteAsync<short>(Prefix.D, "0", 32767);

            // ワードデバイス D0 の値を short 型で読み取る
            // Read a value of type short from word device D0
            short d0 = await mcpx.ReadAsync<short>(Prefix.D, "0");

            Console.WriteLine($"D0: {d0}");


            // ダブルワードデバイス D2 に int 型で 2147483647 を書き込む
            // Write a value of type int (2147483647) to double word device D2
            await mcpx.WriteAsync<int>(Prefix.D, "2", 2147483647);

            // ダブルワードデバイス D2 の値を int 型で読み取る
            // Read a value of type int from double word device D2
            int d2 = await mcpx.ReadAsync<int>(Prefix.D, "2");

            Console.WriteLine($"D2: {d2}");


            // ダブルワードデバイス D4 に float 型で最大値を設定して書き込む
            // Write the maximum float value to double word device D4
            await mcpx.WriteAsync<float>(Prefix.D, "4", (float)3.4028235E+38);

            // D4 の値を float 型で読み取る
            // Read value from D4 as float (corrected)
            float d4 = await mcpx.ReadAsync<float>(Prefix.D, "4");

            Console.WriteLine($"D4: {d4}");


            // ダブルワードデバイス D6 に double 型で最大値を設定して書き込む
            // Write the maximum double value to double word device D6
            await mcpx.WriteAsync<double>(Prefix.D, "6", (double)1.7976931348623157E+308);

            // D6 の値を double 型で読み取る
            // Read value from D6 as double (corrected)
            double d6 = await mcpx.ReadAsync<double>(Prefix.D, "6");

            Console.WriteLine($"D6: {d6}");


            // 7000 ワード分の short 配列を D10 から一括書き込み
            // Create a short array of 7000 words and write them starting from D10
            short[] dbwArr = new short[7000];
            for (int i = 0; i < dbwArr.Length; i++)
            {
                dbwArr[i] = (short)i;
            }
            await mcpx.BatchWriteAsync<short>(Prefix.D, "10", dbwArr);

            // D10 から 7000 ワード分を一括で short 型として読み取る
            // Read 7000 words as short values starting from D10
            short[] dbrArr = await mcpx.BatchReadAsync<short>(Prefix.D, "10", 7000);

            int l = 0;
            foreach (var dr in dbrArr) 
            {
                Console.WriteLine($"D{ 10 + l }: { dr }");
                l++;
            }


            // ランダムなワード/ダブルワードDeviceに short/int 型で書き込み
            // Write to random word (short) and double word (int) device
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
            // Read from random word (short) and double word (int) addresses
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
            // Register specified word and double word devices for monitoring (required only once before Monitor call)
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
            // Retrieve monitored values for registered devices (words as short, double words as int)
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
