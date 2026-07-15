using McpXLib.Commands;
using McpXLib.Enums;
using McpXLib.Utils;
using McpXLib.Exceptions;

// 新しいビルダーAPIは、後方互換のため残している旧ランダムAPI([Obsolete])を内部的に利用するため、
// このファイル内での CS0618(古い形式のメンバー使用) 警告は抑制する。外部利用者には引き続き警告が出る。
#pragma warning disable CS0618

namespace McpXLib;

/// <summary>
/// MCプロトコル拡張クラス
/// </summary>
/// <remarks>
/// Mcpクラス（MCプロトコル）を拡張して、デバイスアクセス点数などの機能制限を補うクラスです。
/// </remarks> 
public partial class McpX : Mcp
{
    private readonly string? password;

    /// <summary>
    /// インスタンス初期化
    /// </summary>
    /// <remarks>
    /// PLCのパラメータ設定に合わせたPLCの接続情報を指定します。<br/>
    /// 必要に応じて、リモートロックの解除を行います。
    /// </remarks>
    /// <param name="ip">PLCのIPアドレスを指定します。</param>
    /// <param name="port">PLCのポートを指定します。</param>
    /// <param name="password">PLCのリモートパスワードを指定します。（リモートパスワードを設定している場合に指定してください。）</param>
    /// <param name="isAscii">ASCIIコードによる交信を行う場合に<c>true</c>を指定します。（デフォルトは、バイナリ交信:<c>false</c>です。）</param>
    /// <param name="isUdp">UDPによる交信を行う場合に<c>true</c>を指定します。（デフォルトは、TCP交信:<c>false</c>です。）</param>
    /// <param name="requestFrame">フレーム（データ交信電文）の種類を指定します。（デフォルトは、3Eフレーム:<c>RequestFrame.E3</c>です。）</param>
    /// <param name="timeoutMilliseconds">通信タイムアウト時間（ミリ秒）を指定します。（デフォルトは、5秒です。）</param>
    /// <param name="processorSeries">PLCのシリーズを指定します。MELSEC iQ-Rシリーズへ拡張デバイス指定で交信する場合に<c>ProcessorSeries.iQR</c>を指定します。（デフォルトは、MELSEC-Q/Lシリーズ:<c>ProcessorSeries.Q</c>です。）</param>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    public McpX(
        string ip,
        int port,
        string? password = null,
        bool isAscii = false,
        bool isUdp = false,
        RequestFrame requestFrame = RequestFrame.E3,
        ushort timeoutMilliseconds = 5000,
        ProcessorSeries processorSeries = ProcessorSeries.Q
    ) : base (
        ip: ip,
        port: port,
        isAscii: isAscii,
        isUdp: isUdp,
        requestFrame: requestFrame,
        processorSeries: processorSeries,
        timeout: timeoutMilliseconds
    )
    {
        this.password = password;

        if (password != null) 
        {
            RemoteUnlock(password);
        }
    }

    /// <summary>
    /// 単一デバイス読み込み
    /// </summary>
    /// <remarks>
    /// 単一デバイスの値をPLCから読み込みます。<br/>
    /// 指定された型<c>T</c>に応じて、内部的に読み込むデバイス点数は自動的に調整されます。
    /// </remarks>
    /// <typeparam name="T">
    /// 読み込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <param name="prefix">読み込み対象の先頭デバイスコードを指定します。</param>
    /// <param name="address">読み込み対象の先頭アドレスを指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <returns>PLCから読み込んだ値を指定された型<c>T</c>に変換して返します。</returns>
    public T Read<T>(Prefix prefix, string address) where T : unmanaged
    {
        if (typeof(T) == typeof(bool))
        {
            return (T)(object)BitBatchRead(
                prefix: prefix,
                address: address,
                bitLength: 1
            ).First();
        }
        else
        {    
            return WordBatchRead<T>(
                prefix: prefix,
                address: address,
                wordLength: (ushort)DeviceConverter.GetWordLength<T>()
            ).First();
        }
    }

    /// <summary>
    /// 単一デバイス読み込み（非同期）
    /// </summary>
    /// <remarks>
    /// 指定したデバイスの値を非同期でPLCから読み込みます。<br/>
    /// 指定された型<c>T</c>に応じて、内部的に読み込むデバイス点数は自動的に調整されます。
    /// </remarks>
    /// <typeparam name="T">
    /// 読み込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <param name="prefix">読み込み対象の先頭デバイスコードを指定します。</param>
    /// <param name="address">読み込み対象の先頭アドレス</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <returns>PLCから読み込んだ値を指定された型<c>T</c>に変換して返します。</returns>
    public async Task<T> ReadAsync<T>(Prefix prefix, string address) where T : unmanaged
    {
        if (typeof(T) == typeof(bool))
        {
            var bitValues = await BitBatchReadAsync(
                prefix: prefix,
                address: address,
                bitLength: 1
            );

            return (T)(object)bitValues.First();
        }
        else
        {    
            var wordValues = await WordBatchReadAsync<T>(
                prefix: prefix,
                address: address,
                wordLength: (ushort)DeviceConverter.GetWordLength<T>()
            );

            return (T)(object)wordValues.First();
        }
    }

    /// <summary>
    /// 連続デバイス読み込み
    /// </summary>
    /// <remarks>
    /// 指定したデバイス範囲の値をPLCから読み込みます。<br/>
    /// </remarks>
    /// <typeparam name="T">
    /// 読み込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <param name="prefix">読み込み対象の先頭デバイスコードを指定します。</param>
    /// <param name="address">読み込み対象の先頭アドレスを指定します。</param>
    /// <param name="length">
    /// 読み込み対象の要素数を指定します。<br/>
    /// 指定された型<c>T</c>に応じて、内部的に読み込むデバイス点数は自動的に調整されます。<br/>
    /// そのため、この引数には「最終的に取得する配列の要素数（<c>T</c>型の要素数）」を指定してください。
    /// </param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <returns>PLCから読み込んだ値を指定された型<c>T</c>に変換して返します。</returns>
    public T[] BatchRead<T>(Prefix prefix, string address, ushort length) where T : unmanaged
    {
        var values = new List<T>();

        if (typeof(T) == typeof(bool))
        {
            ushort maxLengh = BitBatchReadCommand.MAX_BIT_LENGTH;
            for (int i = 0; i <= length / maxLengh; i++) 
            {
                int offset = i * maxLengh;
                int bitLength = Math.Min(maxLengh, length - offset);

                if (bitLength == 0) break;

                values.AddRange(
                    (T[])(object)BitBatchRead(
                        prefix: prefix,
                        address: DeviceConverter.GetOffsetAddress(prefix, address, offset),
                        bitLength: (ushort)bitLength
                    )
                );
            }

            return values.ToArray();

        }
        else
        {
            ushort wordSize = (ushort)DeviceConverter.GetWordLength<T>();
            ushort wordLength = (ushort)(wordSize * length);
            ushort maxLengh = WordBatchReadCommand<T>.MAX_WORD_LENGTH;

            for (int i = 0; i <= wordLength / maxLengh; i++) 
            {
                int offset = i * maxLengh;
                int readLength = Math.Min(maxLengh, wordLength - offset);

                if (readLength == 0) break;

                values.AddRange(
                    WordBatchRead<T>(
                        prefix: prefix,
                        address: DeviceConverter.GetOffsetAddress(prefix, address, offset),
                        wordLength: (ushort)(readLength / wordSize)
                    )
                );
            }

            return values.ToArray();
        }
    }

    /// <summary>
    /// 連続デバイス読み込み（非同期）
    /// </summary>
    /// <remarks>
    /// 指定したデバイス範囲の値を非同期でPLCから読み込みます。<br/>
    /// </remarks>
    /// <typeparam name="T">
    /// 読み込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <param name="prefix">読み込み対象の先頭デバイスコードを指定します。</param>
    /// <param name="address">読み込み対象の先頭アドレスを指定します。</param>
    /// <param name="length">
    /// 読み込み対象の要素数を指定します。<br/>
    /// 指定された型<c>T</c>に応じて、内部的に読み込むデバイス点数は自動的に調整されます。<br/>
    /// そのため、この引数には「最終的に取得する配列の要素数（<c>T</c>型の要素数）」を指定してください。
    /// </param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <returns>PLCから読み込んだ値を指定された型<c>T</c>に変換して返します。</returns>
    public async Task<T[]> BatchReadAsync<T>(Prefix prefix, string address, ushort length) where T : unmanaged
    {
        var values = new List<T>();

        if (typeof(T) == typeof(bool))
        {
            ushort maxLengh = BitBatchReadCommand.MAX_BIT_LENGTH;
            for (int i = 0; i <= length / maxLengh; i++) 
            {
                int offset = i * maxLengh;
                int bitLength = Math.Min(maxLengh, length - offset);

                if (bitLength == 0) break;

                values.AddRange(
                    (T[])(object) await BitBatchReadAsync(
                        prefix: prefix,
                        address: DeviceConverter.GetOffsetAddress(prefix, address, offset),
                        bitLength: (ushort)bitLength
                    )
                );
            }

            return values.ToArray();

        }
        else
        {
            ushort wordSize = (ushort)DeviceConverter.GetWordLength<T>();
            ushort wordLength = (ushort)(wordSize * length);
            ushort maxLengh = WordBatchReadCommand<T>.MAX_WORD_LENGTH;

            for (int i = 0; i <= wordLength / maxLengh; i++) 
            {
                int offset = i * maxLengh;
                int readLength = Math.Min(maxLengh, wordLength - offset);

                if (readLength == 0) break;

                values.AddRange(
                    await WordBatchReadAsync<T>(
                        prefix: prefix,
                        address: DeviceConverter.GetOffsetAddress(prefix, address, offset),
                        wordLength: (ushort)(readLength / wordSize)
                    )
                );
            }

            return values.ToArray();
        }
    }

    /// <summary>
    /// 単一デバイス書き込み
    /// </summary>
    /// <remarks>
    /// 単一デバイスの値をPLCに書き込みます。<br/>
    /// 指定された型<c>T</c>に応じて、内部的に書き込むデバイス点数は自動的に調整されます。
    /// </remarks>
    /// <typeparam name="T">
    /// 書き込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <param name="prefix">書き込み対象の先頭デバイスコードを指定します。</param>
    /// <param name="address">書き込み対象の先頭アドレスを指定します。</param>
    /// <param name="value">書き込みする値を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    public void Write<T>(Prefix prefix, string address, T value) where T : unmanaged
    {
        T[] values = [value];
        if (typeof(T) == typeof(bool))
        {
            BitBatchWrite(
                prefix: prefix,
                address: address,
                values: (bool[])(object)values
            );
        }
        else
        {
            WordBatchWrite<T>(
                prefix: prefix,
                address: address,
                values: values
            );
        }
    }
    
    /// <summary>
    /// 単一デバイス書き込み（非同期）
    /// </summary>
    /// <remarks>
    /// 指定したデバイスに値を非同期でPLCへ書き込みます。<br/>
    /// 指定された型<c>T</c>に応じて、内部的に書き込むデバイス点数は自動的に調整されます。
    /// </remarks>
    /// <typeparam name="T">
    /// 書き込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <param name="prefix">書き込み対象の先頭デバイスコードを指定します。</param>
    /// <param name="address">書き込み対象の先頭アドレスを指定します。</param>
    /// <param name="value">書き込みする値を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    public async Task WriteAsync<T>(Prefix prefix, string address, T value) where T : unmanaged
    {
        T[] values = [value];
        if (typeof(T) == typeof(bool))
        {
            await BitBatchWriteAsync(
                prefix: prefix,
                address: address,
                values: (bool[])(object)values
            );
        }
        else
        {
            await WordBatchWriteAsync<T>(
                prefix: prefix,
                address: address,
                values: values
            );
        }
    }

    /// <summary>
    /// 連続デバイス書き込み
    /// </summary>
    /// <remarks>
    /// 指定したデバイス範囲に値をPLCへ書き込みます。<br/>
    /// 指定された型<c>T</c>に応じて、内部的に書き込むデバイス点数は自動的に調整されます。
    /// </remarks>
    /// <typeparam name="T">
    /// 書き込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <param name="prefix">書き込み対象の先頭デバイスコードを指定します。</param>
    /// <param name="address">書き込み対象の先頭アドレスを指定します。</param>
    /// <param name="values">書き込みする値を配列で指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <returns><c>values</c>の値をそのまま返します。</returns>
    public T[] BatchWrite<T>(Prefix prefix, string address, T[]values) where T : unmanaged
    {
        if (typeof(T) == typeof(bool))
        {
            ushort maxLengh = BitBatchWriteCommand.MAX_BIT_LENGTH;
            for (int i = 0; i <= values.Length / maxLengh; i++) 
            {
                int offset = i * maxLengh;
                int length = Math.Min(maxLengh, values.Length - offset);
                
                if (length == 0) break;

                BitBatchWrite(
                    prefix: prefix,
                    address: DeviceConverter.GetOffsetAddress(prefix, address, offset),
                    values: (bool[])(object)values.Skip(offset).Take(length).ToArray()
                );
            }

            return values.ToArray();
        }
        else
        {
            ushort wordSize = (ushort)DeviceConverter.GetWordLength<T>();
            ushort wordLength = (ushort)(wordSize * values.Length);
            ushort maxLengh = WordBatchWriteCommand<T>.MAX_WORD_LENGTH;

            for (int i = 0; i <= wordLength / maxLengh; i++) 
            {
                int offset = i * maxLengh;
                int length = Math.Min(maxLengh, wordLength - offset);

                if (length == 0) break;

                WordBatchWrite<T>(
                    prefix: prefix,
                    address: DeviceConverter.GetOffsetAddress(prefix, address, offset),
                    values: values.Skip(offset / wordSize).Take(length / wordSize).ToArray()
                );
            }

            return values.ToArray();
        }
    }

    /// <summary>
    /// 連続デバイス書き込み（非同期）
    /// </summary>
    /// <remarks>
    /// 指定したデバイス範囲に値を非同期でPLCへ書き込みます。<br/>
    /// 指定された型<c>T</c>に応じて、内部的に書き込むデバイス点数は自動的に調整されます。
    /// </remarks>
    /// <typeparam name="T">
    /// 書き込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <param name="prefix">書き込み対象の先頭デバイスコードを指定します。</param>
    /// <param name="address">書き込み対象の先頭アドレスを指定します。</param>
    /// <param name="values">書き込みする値を配列で指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <returns><c>values</c>の値をそのまま返します。</returns>
    public async Task<T[]> BatchWriteAsync<T>(Prefix prefix, string address, T[]values) where T : unmanaged
    {
        if (typeof(T) == typeof(bool))
        {
            ushort maxLengh = BitBatchWriteCommand.MAX_BIT_LENGTH;
            for (int i = 0; i <= values.Length / maxLengh; i++) 
            {
                int offset = i * maxLengh;
                int length = Math.Min(maxLengh, values.Length - offset);
                
                if (length == 0) break;

                await BitBatchWriteAsync(
                    prefix: prefix,
                    address: DeviceConverter.GetOffsetAddress(prefix, address, offset),
                    values: (bool[])(object)values.Skip(offset).Take(length).ToArray()
                );
            }

            return values.ToArray();
        }
        else
        {
            ushort wordSize = (ushort)DeviceConverter.GetWordLength<T>();
            ushort wordLength = (ushort)(wordSize * values.Length);
            ushort maxLengh = WordBatchWriteCommand<T>.MAX_WORD_LENGTH;

            for (int i = 0; i <= wordLength / maxLengh; i++) 
            {
                int offset = i * maxLengh;
                int length = Math.Min(maxLengh, wordLength - offset);

                if (length == 0) break;

                await WordBatchWriteAsync<T>(
                    prefix: prefix,
                    address: DeviceConverter.GetOffsetAddress(prefix, address, offset),
                    values: values.Skip(offset / wordSize).Take(length / wordSize).ToArray()
                );
            }

            return values.ToArray();
        }
    }

    /// <summary>
    /// ランダムデバイス読み込み
    /// </summary>
    /// <remarks>
    /// 指定したデバイスの値をPLCから読み込みます。<br/>
    /// 指定された型<c>T1</c>、<c>T2</c>に応じて、内部的に読み込むデバイス点数は自動的に調整されます。
    /// </remarks>
    /// <typeparam name="T1">
    /// 16ビット単位で読み込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <typeparam name="T2">
    /// 32ビット単位で読み込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <param name="wordAddresses">
    /// 16ビット単位で読み込むデバイスアドレスの配列を指定します。<br />
    /// ・<c>prefix</c>:読み込み対象のデバイスコードを指定します。<br/>
    /// ・<c>address</c>:読み込み対象のアドレスを指定します。
    /// </param>
    /// <param name="doubleWordAddresses">
    /// 32ビット単位で読み込むデバイスの配列を指定します。<br/>
    /// ・<c>prefix</c>:読み込み対象のデバイスコードを指定します。<br/>
    /// ・<c>address</c>:読み込み対象のアドレスを指定します。
    /// </param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <returns>
    /// PLCから読み込んだ値を指定した型<c>T1</c>、<c>T2</c>に変換して返します。<br/>
    /// ・<c>wordValues</c>: 16ビット単位で読み込まれた <c>T1</c>型の値の配列<br/>
    /// ・<c>doubleValues</c>: 32ビット単位で読み込まれた <c>T2</c>型の値の配列
    /// </returns>
    [Obsolete("Use the RandomRead(Action<RandomReadBuilder>) builder overload instead.")]
    public (T1[] wordValues, T2[] doubleValues) RandomRead<T1, T2>((Prefix prefix, string address)[] wordAddresses, (Prefix prefix, string address)[] doubleWordAddresses)
        where T1 : unmanaged
        where T2 : unmanaged
    {
        if (typeof(T1) == typeof(bool) || typeof(T2) == typeof(bool))
        {
            T1[] wv = typeof(T1) == typeof(bool)
                ? (T1[])(object)RandomReadBit(wordAddresses)
                : RandomRead<T1, ushort>(wordAddresses, Array.Empty<(Prefix, string)>()).wordValues;
            T2[] dv = typeof(T2) == typeof(bool)
                ? (T2[])(object)RandomReadBit(doubleWordAddresses)
                : RandomRead<ushort, T2>(Array.Empty<(Prefix, string)>(), doubleWordAddresses).doubleValues;
            return (wv, dv);
        }

        List<T1> allWordValues = new List<T1>();
        List<T2> allDoubleValues = new List<T2>();

        int maxWordLength = WordRandomReadCommand<T1, T2>.GetMaxWordLength(ProcessorSeries);
        int wordIndex = 0, doubleIndex = 0;
        while (wordIndex < wordAddresses.Length || doubleIndex < doubleWordAddresses.Length)
        {
            var wordBatch = wordAddresses.Skip(wordIndex).Take(maxWordLength).ToArray();
            var doubleBatch = doubleWordAddresses.Skip(doubleIndex).Take(maxWordLength - wordBatch.Length).ToArray();

            if (wordBatch.Length + doubleBatch.Length == 0)
                break;

            var (wordValues, doubleValues) = WordRandomRead<T1, T2>(wordBatch, doubleBatch);

            allWordValues.AddRange(wordValues);
            allDoubleValues.AddRange(doubleValues);

            wordIndex += wordBatch.Length;
            doubleIndex += doubleBatch.Length;
        }

        return (allWordValues.ToArray(), allDoubleValues.ToArray());
    }

    /// <summary>
    /// ランダムデバイス読み込み（非同期）
    /// </summary>
    /// <remarks>
    /// 指定したデバイスの値を非同期でPLCから読み込みます。<br/>
    /// 指定された型<c>T1</c>、<c>T2</c>に応じて、内部的に読み込むデバイス点数は自動的に調整されます。
    /// </remarks>
    /// <typeparam name="T1">
    /// 16ビット単位で読み込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <typeparam name="T2">
    /// 32ビット単位で読み込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <param name="wordAddresses">
    /// 16ビット単位で読み込むデバイスアドレスの配列を指定します。<br />
    /// ・<c>prefix</c>:読み込み対象のデバイスコードを指定します。<br/>
    /// ・<c>address</c>:読み込み対象のアドレスを指定します。
    /// </param>
    /// <param name="doubleWordAddresses">
    /// 32ビット単位で読み込むデバイスの配列を指定します。<br/>
    /// ・<c>prefix</c>:読み込み対象のデバイスコードを指定します。<br/>
    /// ・<c>address</c>:読み込み対象のアドレスを指定します。
    /// </param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <returns>
    /// PLCから読み込んだ値を指定した型<c>T1</c>、<c>T2</c>に変換して返します。<br/>
    /// ・<c>wordValues</c>: 16ビット単位で読み込まれた <c>T1</c>型の値の配列<br/>
    /// ・<c>doubleValues</c>: 32ビット単位で読み込まれた <c>T2</c>型の値の配列
    /// </returns>
    [Obsolete("Use the RandomReadAsync(Action<RandomReadBuilder>) builder overload instead.")]
    public async Task<(T1[] wordValues, T2[] doubleValues)> RandomReadAsync<T1, T2>((Prefix prefix, string address)[] wordAddresses, (Prefix prefix, string address)[] doubleWordAddresses)
        where T1 : unmanaged
        where T2 : unmanaged
    {
        if (typeof(T1) == typeof(bool) || typeof(T2) == typeof(bool))
        {
            T1[] wv = typeof(T1) == typeof(bool)
                ? (T1[])(object)await RandomReadBitAsync(wordAddresses)
                : (await RandomReadAsync<T1, ushort>(wordAddresses, Array.Empty<(Prefix, string)>())).wordValues;
            T2[] dv = typeof(T2) == typeof(bool)
                ? (T2[])(object)await RandomReadBitAsync(doubleWordAddresses)
                : (await RandomReadAsync<ushort, T2>(Array.Empty<(Prefix, string)>(), doubleWordAddresses)).doubleValues;
            return (wv, dv);
        }

        List<T1> allWordValues = new List<T1>();
        List<T2> allDoubleValues = new List<T2>();

        int maxWordLength = WordRandomReadCommand<T1, T2>.GetMaxWordLength(ProcessorSeries);
        int wordIndex = 0, doubleIndex = 0;
        while (wordIndex < wordAddresses.Length || doubleIndex < doubleWordAddresses.Length)
        {
            var wordBatch = wordAddresses.Skip(wordIndex).Take(maxWordLength).ToArray();
            var doubleBatch = doubleWordAddresses.Skip(doubleIndex).Take(maxWordLength - wordBatch.Length).ToArray();

            if (wordBatch.Length + doubleBatch.Length == 0)
                break;

            var (wordValues, doubleValues) = await WordRandomReadAsync<T1, T2>(wordBatch, doubleBatch);

            allWordValues.AddRange(wordValues);
            allDoubleValues.AddRange(doubleValues);

            wordIndex += wordBatch.Length;
            doubleIndex += doubleBatch.Length;
        }

        return (allWordValues.ToArray(), allDoubleValues.ToArray());
    }

    /// <summary>
    /// ランダムデバイス書き込み
    /// </summary>
    /// <remarks>
    /// 指定したデバイスに対し、値をPLCへ書き込みます。<br/>
    /// 指定された型<c>T1</c>、<c>T2</c>に応じて、内部的に読み込むデバイス点数は自動的に調整されます。
    /// </remarks>
    /// <typeparam name="T1">
    /// 16ビット単位で書き込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <typeparam name="T2">
    /// 32ビット単位で書き込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <param name="wordDevices">
    /// 16ビット単位で書き込むデバイスの配列を指定します。<br/>
    /// ・<c>prefix</c>:書き込み対象のデバイスコードを指定します。<br/>
    /// ・<c>address</c>:書き込み対象のアドレスを指定します。<br/>
    /// ・<c>value</c>:書き込みする値を指定します。
    /// </param>
    /// <param name="doubleWordDevices">
    /// 32ビット単位で書き込むデバイスの配列を指定します。<br/>
    /// ・<c>prefix</c>:書き込み対象のデバイスコードを指定します。<br/>
    /// ・<c>address</c>:書き込み対象のアドレスを指定します。
    /// ・<c>value</c>:書き込みする値を指定します。
    /// </param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    [Obsolete("Use the RandomWrite(Action<RandomWriteBuilder>) builder overload instead.")]
    public void RandomWrite<T1, T2>(
        (Prefix prefix, string address, T1 value)[] wordDevices,
        (Prefix prefix, string address, T2 value)[] doubleWordDevices)
        where T1 : unmanaged
        where T2 : unmanaged
    {
        if (typeof(T1) == typeof(bool) || typeof(T2) == typeof(bool))
        {
            if (typeof(T1) == typeof(bool))
                RandomWriteBit(wordDevices.Select(d => (d.prefix, d.address, (bool)(object)d.value)).ToArray());
            else
                RandomWrite<T1, ushort>(wordDevices, Array.Empty<(Prefix, string, ushort)>());

            if (typeof(T2) == typeof(bool))
                RandomWriteBit(doubleWordDevices.Select(d => (d.prefix, d.address, (bool)(object)d.value)).ToArray());
            else
                RandomWrite<ushort, T2>(Array.Empty<(Prefix, string, ushort)>(), doubleWordDevices);

            return;
        }

        int maxSize = WordRandomWriteCommand<T1, T2>.GetMaxWordLength(ProcessorSeries);
        int wordSize = WordRandomWriteCommand<T1, T2>.WORD_SIZE;
        int doubleWordSize = WordRandomWriteCommand<T1, T2>.DOUBLE_WORD_SIZE;

        List<(Prefix prefix, string address, T1 value)> wordList = wordDevices.ToList();
        List<(Prefix prefix, string address, T2 value)> doubleWordList = doubleWordDevices.ToList();

        while (wordList.Count > 0 || doubleWordList.Count > 0)
        {
            List<(Prefix prefix, string address, T1 value)> wordBatch = new();
            List<(Prefix prefix, string address, T2 value)> doubleWordBatch = new();

            int batchSize = 0;

            while (wordList.Count > 0)
            {
                var item = wordList[0];
                if (batchSize + wordSize > maxSize) break;
                wordBatch.Add(item);
                wordList.RemoveAt(0);
                batchSize += wordSize;
            }

            while (doubleWordList.Count > 0)
            {
                var item = doubleWordList[0];
                if (batchSize + doubleWordSize > maxSize) break;
                doubleWordBatch.Add(item);
                doubleWordList.RemoveAt(0);
                batchSize += doubleWordSize;
            }

            if (wordBatch.Count > 0 || doubleWordBatch.Count > 0)
            {
                WordRandomWrite(wordBatch.ToArray(), doubleWordBatch.ToArray());
            }
        }
    }

    /// <summary>
    /// ランダムデバイス書き込み（非同期）
    /// </summary>
    /// <remarks>
    /// 指定したデバイスに対し、値を非同期でPLCへ書き込みます。<br/>
    /// 指定された型<c>T1</c>、<c>T2</c>に応じて、内部的に読み込むデバイス点数は自動的に調整されます。
    /// </remarks>
    /// <typeparam name="T1">
    /// 16ビット単位で書き込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <typeparam name="T2">
    /// 32ビット単位で書き込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <param name="wordDevices">
    /// 16ビット単位で書き込むデバイスの配列を指定します。<br/>
    /// ・<c>prefix</c>:書き込み対象のデバイスコードを指定します。<br/>
    /// ・<c>address</c>:書き込み対象のアドレスを指定します。<br/>
    /// ・<c>value</c>:書き込みする値を指定します。
    /// </param>
    /// <param name="doubleWordDevices">
    /// 32ビット単位で書き込むデバイスの配列を指定します。<br/>
    /// ・<c>prefix</c>:書き込み対象のデバイスコードを指定します。<br/>
    /// ・<c>address</c>:書き込み対象のアドレスを指定します。
    /// ・<c>value</c>:書き込みする値を指定します。
    /// </param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    [Obsolete("Use the RandomWriteAsync(Action<RandomWriteBuilder>) builder overload instead.")]
    public async Task RandomWriteAsync<T1, T2>(
        (Prefix prefix, string address, T1 value)[] wordDevices,
        (Prefix prefix, string address, T2 value)[] doubleWordDevices)
        where T1 : unmanaged
        where T2 : unmanaged
    {
        if (typeof(T1) == typeof(bool) || typeof(T2) == typeof(bool))
        {
            if (typeof(T1) == typeof(bool))
                await RandomWriteBitAsync(wordDevices.Select(d => (d.prefix, d.address, (bool)(object)d.value)).ToArray());
            else
                await RandomWriteAsync<T1, ushort>(wordDevices, Array.Empty<(Prefix, string, ushort)>());

            if (typeof(T2) == typeof(bool))
                await RandomWriteBitAsync(doubleWordDevices.Select(d => (d.prefix, d.address, (bool)(object)d.value)).ToArray());
            else
                await RandomWriteAsync<ushort, T2>(Array.Empty<(Prefix, string, ushort)>(), doubleWordDevices);

            return;
        }

        int maxSize = WordRandomWriteCommand<T1, T2>.GetMaxWordLength(ProcessorSeries);
        int wordSize = WordRandomWriteCommand<T1, T2>.WORD_SIZE;
        int doubleWordSize = WordRandomWriteCommand<T1, T2>.DOUBLE_WORD_SIZE;

        List<(Prefix prefix, string address, T1 value)> wordList = wordDevices.ToList();
        List<(Prefix prefix, string address, T2 value)> doubleWordList = doubleWordDevices.ToList();

        while (wordList.Count > 0 || doubleWordList.Count > 0)
        {
            List<(Prefix prefix, string address, T1 value)> wordBatch = new();
            List<(Prefix prefix, string address, T2 value)> doubleWordBatch = new();

            int batchSize = 0;

            while (wordList.Count > 0)
            {
                var item = wordList[0];
                if (batchSize + wordSize > maxSize) break;
                wordBatch.Add(item);
                wordList.RemoveAt(0);
                batchSize += wordSize;
            }

            while (doubleWordList.Count > 0)
            {
                var item = doubleWordList[0];
                if (batchSize + doubleWordSize > maxSize) break;
                doubleWordBatch.Add(item);
                doubleWordList.RemoveAt(0);
                batchSize += doubleWordSize;
            }

            if (wordBatch.Count > 0 || doubleWordBatch.Count > 0)
            {
                await WordRandomWriteAsync(wordBatch.ToArray(), doubleWordBatch.ToArray());
            }
        }
    }

    /// <summary>
    /// ランダムビットデバイス読み込み
    /// </summary>
    /// <remarks>
    /// 指定した（非連続の）ビットデバイスのON/OFFをPLCから読み込みます。<br/>
    /// MCプロトコルにはビット単位のランダム読み出しコマンドが存在しないため、内部的には各ビットデバイスを
    /// ワードアクセス（16点単位）で読み出し、先頭ビット(bit0)を抽出して返します。
    /// </remarks>
    /// <param name="bitAddresses">
    /// 読み込み対象のビットデバイスアドレスの配列を指定します。<br/>
    /// ・<c>prefix</c>:読み込み対象のデバイスコードを指定します。<br/>
    /// ・<c>address</c>:読み込み対象のアドレスを指定します。
    /// </param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <returns>PLCから読み込んだ各ビットデバイスのON(<c>true</c>)/OFF(<c>false</c>)を配列で返します。</returns>
    [Obsolete("Use RandomRead(b => b.Add<bool>(prefix, address, ...)) builder overload instead.")]
    public bool[] RandomReadBit((Prefix prefix, string address)[] bitAddresses)
    {
        var result = new List<bool>();

        int maxWordLength = WordRandomReadCommand<ushort, ushort>.GetMaxWordLength(ProcessorSeries);
        for (int i = 0; i < bitAddresses.Length; i += maxWordLength)
        {
            var chunk = bitAddresses.Skip(i).Take(maxWordLength).ToArray();
            var (words, _) = WordRandomRead<ushort, ushort>(chunk, Array.Empty<(Prefix, string)>());
            result.AddRange(words.Select(w => (w & 0x0001) != 0));
        }

        return result.ToArray();
    }

    /// <summary>
    /// ランダムビットデバイス読み込み（非同期）
    /// </summary>
    /// <remarks>
    /// 指定した（非連続の）ビットデバイスのON/OFFを非同期でPLCから読み込みます。<br/>
    /// MCプロトコルにはビット単位のランダム読み出しコマンドが存在しないため、内部的には各ビットデバイスを
    /// ワードアクセス（16点単位）で読み出し、先頭ビット(bit0)を抽出して返します。
    /// </remarks>
    /// <param name="bitAddresses">
    /// 読み込み対象のビットデバイスアドレスの配列を指定します。<br/>
    /// ・<c>prefix</c>:読み込み対象のデバイスコードを指定します。<br/>
    /// ・<c>address</c>:読み込み対象のアドレスを指定します。
    /// </param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <returns>PLCから読み込んだ各ビットデバイスのON(<c>true</c>)/OFF(<c>false</c>)を配列で返します。</returns>
    [Obsolete("Use RandomReadAsync(b => b.Add<bool>(prefix, address, ...)) builder overload instead.")]
    public async Task<bool[]> RandomReadBitAsync((Prefix prefix, string address)[] bitAddresses)
    {
        var result = new List<bool>();

        int maxWordLength = WordRandomReadCommand<ushort, ushort>.GetMaxWordLength(ProcessorSeries);
        for (int i = 0; i < bitAddresses.Length; i += maxWordLength)
        {
            var chunk = bitAddresses.Skip(i).Take(maxWordLength).ToArray();
            var (words, _) = await WordRandomReadAsync<ushort, ushort>(chunk, Array.Empty<(Prefix, string)>());
            result.AddRange(words.Select(w => (w & 0x0001) != 0));
        }

        return result.ToArray();
    }

    /// <summary>
    /// ランダムビットデバイス書き込み
    /// </summary>
    /// <remarks>
    /// 指定した（非連続の）ビットデバイスに対し、ON/OFFをビット単位でPLCへ書き込みます。<br/>
    /// MCプロトコルのビット単位ランダム書き込み（コマンド:1402 / サブコマンド:0001）を使用します。
    /// </remarks>
    /// <param name="bitDevices">
    /// 書き込み対象のビットデバイスの配列を指定します。<br/>
    /// ・<c>prefix</c>:書き込み対象のデバイスコードを指定します。<br/>
    /// ・<c>address</c>:書き込み対象のアドレスを指定します。<br/>
    /// ・<c>value</c>:書き込むON(<c>true</c>)/OFF(<c>false</c>)を指定します。
    /// </param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    [Obsolete("Use RandomWrite(b => b.Add<bool>(prefix, address, value)) builder overload instead.")]
    public void RandomWriteBit((Prefix prefix, string address, bool value)[] bitDevices)
    {
        int maxBitLength = BitRandomWriteCommand.GetMaxBitLength(ProcessorSeries);
        for (int i = 0; i < bitDevices.Length; i += maxBitLength)
        {
            var chunk = bitDevices.Skip(i).Take(maxBitLength).ToArray();
            BitRandomWrite(chunk);
        }
    }

    /// <summary>
    /// ランダムビットデバイス書き込み（非同期）
    /// </summary>
    /// <remarks>
    /// 指定した（非連続の）ビットデバイスに対し、ON/OFFをビット単位で非同期でPLCへ書き込みます。<br/>
    /// MCプロトコルのビット単位ランダム書き込み（コマンド:1402 / サブコマンド:0001）を使用します。
    /// </remarks>
    /// <param name="bitDevices">
    /// 書き込み対象のビットデバイスの配列を指定します。<br/>
    /// ・<c>prefix</c>:書き込み対象のデバイスコードを指定します。<br/>
    /// ・<c>address</c>:書き込み対象のアドレスを指定します。<br/>
    /// ・<c>value</c>:書き込むON(<c>true</c>)/OFF(<c>false</c>)を指定します。
    /// </param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    [Obsolete("Use RandomWriteAsync(b => b.Add<bool>(prefix, address, value)) builder overload instead.")]
    public async Task RandomWriteBitAsync((Prefix prefix, string address, bool value)[] bitDevices)
    {
        int maxBitLength = BitRandomWriteCommand.GetMaxBitLength(ProcessorSeries);
        for (int i = 0; i < bitDevices.Length; i += maxBitLength)
        {
            var chunk = bitDevices.Skip(i).Take(maxBitLength).ToArray();
            await BitRandomWriteAsync(chunk);
        }
    }

    /// <summary>
    /// ランダムデバイス読み込み（ビルダー）
    /// </summary>
    /// <remarks>
    /// 非連続のデバイスを、型ごとにアクセス単位（ビット／ワード／ダブルワード）を意識せずに一括で読み込みます。<br/>
    /// <paramref name="build"/> 内で <see cref="RandomReadBuilder.Add{T}(Prefix, string, Action{T})"/> により
    /// デバイスと読み取り後のコールバックを並べます。読み取り完了後、各コールバックに変換済みの値が渡されます。<br/>
    /// アクセス単位・点数上限・パケット分割は内部で自動処理されます。
    /// </remarks>
    /// <param name="build">読み込むデバイスとコールバックを登録するビルダー操作を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    public void RandomRead(Action<RandomReadBuilder> build)
    {
        var builder = new RandomReadBuilder();
        build(builder);

        var wordAddresses = builder.wordEntries.Select(e => (e.prefix, e.address)).ToArray();
        var doubleWordAddresses = builder.doubleWordEntries.Select(e => (e.prefix, e.address)).ToArray();

        if (wordAddresses.Length == 0 && doubleWordAddresses.Length == 0)
        {
            return;
        }

        var (rawWords, rawDoubleWords) = RandomRead<ushort, uint>(wordAddresses, doubleWordAddresses);

        for (int i = 0; i < builder.wordEntries.Count; i++)
        {
            builder.wordEntries[i].apply(rawWords[i]);
        }

        for (int j = 0; j < builder.doubleWordEntries.Count; j++)
        {
            builder.doubleWordEntries[j].apply(rawDoubleWords[j]);
        }
    }

    /// <summary>
    /// ランダムデバイス読み込み（ビルダー・非同期）
    /// </summary>
    /// <remarks>
    /// 非連続のデバイスを、型ごとにアクセス単位（ビット／ワード／ダブルワード）を意識せずに非同期で一括読み込みします。<br/>
    /// <paramref name="build"/> 内で <see cref="RandomReadBuilder.Add{T}(Prefix, string, Action{T})"/> により
    /// デバイスと読み取り後のコールバックを並べます。読み取り完了後、各コールバックに変換済みの値が渡されます。
    /// </remarks>
    /// <param name="build">読み込むデバイスとコールバックを登録するビルダー操作を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    public async Task RandomReadAsync(Action<RandomReadBuilder> build)
    {
        var builder = new RandomReadBuilder();
        build(builder);

        var wordAddresses = builder.wordEntries.Select(e => (e.prefix, e.address)).ToArray();
        var doubleWordAddresses = builder.doubleWordEntries.Select(e => (e.prefix, e.address)).ToArray();

        if (wordAddresses.Length == 0 && doubleWordAddresses.Length == 0)
        {
            return;
        }

        var (rawWords, rawDoubleWords) = await RandomReadAsync<ushort, uint>(wordAddresses, doubleWordAddresses);

        for (int i = 0; i < builder.wordEntries.Count; i++)
        {
            builder.wordEntries[i].apply(rawWords[i]);
        }

        for (int j = 0; j < builder.doubleWordEntries.Count; j++)
        {
            builder.doubleWordEntries[j].apply(rawDoubleWords[j]);
        }
    }

    /// <summary>
    /// ランダムデバイス書き込み（ビルダー）
    /// </summary>
    /// <remarks>
    /// 非連続のデバイスに対し、型ごとにアクセス単位（ビット／ワード／ダブルワード）を意識せずに一括で書き込みます。<br/>
    /// <paramref name="build"/> 内で <see cref="RandomWriteBuilder.Add{T}(Prefix, string, T)"/> により
    /// デバイスと値を並べます。アクセス単位・点数上限・パケット分割は内部で自動処理されます。<br/>
    /// ワード／ダブルワードとビットはMCプロトコル上サブコマンドが異なるため、別パケットで送信されます。
    /// </remarks>
    /// <param name="build">書き込むデバイスと値を登録するビルダー操作を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    public void RandomWrite(Action<RandomWriteBuilder> build)
    {
        var builder = new RandomWriteBuilder();
        build(builder);

        if (builder.wordDevices.Count > 0 || builder.doubleWordDevices.Count > 0)
        {
            RandomWrite<ushort, uint>(builder.wordDevices.ToArray(), builder.doubleWordDevices.ToArray());
        }

        if (builder.bitDevices.Count > 0)
        {
            RandomWriteBit(builder.bitDevices.ToArray());
        }
    }

    /// <summary>
    /// ランダムデバイス書き込み（ビルダー・非同期）
    /// </summary>
    /// <remarks>
    /// 非連続のデバイスに対し、型ごとにアクセス単位（ビット／ワード／ダブルワード）を意識せずに非同期で一括書き込みします。<br/>
    /// <paramref name="build"/> 内で <see cref="RandomWriteBuilder.Add{T}(Prefix, string, T)"/> により
    /// デバイスと値を並べます。ワード／ダブルワードとビットは別パケットで送信されます。
    /// </remarks>
    /// <param name="build">書き込むデバイスと値を登録するビルダー操作を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    public async Task RandomWriteAsync(Action<RandomWriteBuilder> build)
    {
        var builder = new RandomWriteBuilder();
        build(builder);

        if (builder.wordDevices.Count > 0 || builder.doubleWordDevices.Count > 0)
        {
            await RandomWriteAsync<ushort, uint>(builder.wordDevices.ToArray(), builder.doubleWordDevices.ToArray());
        }

        if (builder.bitDevices.Count > 0)
        {
            await RandomWriteBitAsync(builder.bitDevices.ToArray());
        }
    }

    /// <summary>
    /// モニタデバイス登録（ビルダー）
    /// </summary>
    /// <remarks>
    /// モニタ対象の非連続デバイスを、型ごとにアクセス単位（ビット／ワード／ダブルワード）を意識せずに登録(コマンド: 0801)します。<br/>
    /// <paramref name="build"/> 内で <see cref="MonitorBuilder.Add{T}(Prefix, string, Action{T})"/> により
    /// デバイスと読み取り後のコールバックを並べます。<br/>
    /// 戻り値の <see cref="MonitorSession"/> で <see cref="MonitorSession.Read"/> を繰り返し実行できます。<br/>
    /// モニタ登録はパケット分割ができず（コマンドの特性上、再登録は全置換）、点数上限は
    /// ワード＋ダブルワードの合計で 192点（Q/L）／96点（iQ-R）です。超過時は例外をスローします。
    /// </remarks>
    /// <param name="build">モニタ対象のデバイスとコールバックを登録するビルダー操作を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="ArgumentException">モニタ登録の点数上限を超過した場合に例外をスローします（分割不可）。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <returns>登録済みデバイスを繰り返し読み出すための <see cref="MonitorSession"/>。</returns>
    public MonitorSession MonitorRegist(Action<MonitorBuilder> build)
    {
        var builder = new MonitorBuilder();
        build(builder);

        var wordAddresses = builder.wordEntries.Select(e => (e.prefix, e.address)).ToArray();
        var doubleWordAddresses = builder.doubleWordEntries.Select(e => (e.prefix, e.address)).ToArray();

        // モニタ登録は分割不可。点数上限を超えると MonitorRegistCommand が例外をスローする。
        MonitorRegist(wordAddresses, doubleWordAddresses);

        return new MonitorSession(this, builder);
    }

    /// <summary>
    /// モニタデバイス登録（ビルダー・非同期）
    /// </summary>
    /// <remarks>
    /// モニタ対象の非連続デバイスを、型ごとにアクセス単位（ビット／ワード／ダブルワード）を意識せずに非同期で登録(コマンド: 0801)します。<br/>
    /// 戻り値の <see cref="MonitorSession"/> で <see cref="MonitorSession.ReadAsync"/> を繰り返し実行できます。<br/>
    /// モニタ登録はパケット分割ができず、点数上限は 192点（Q/L）／96点（iQ-R）です。超過時は例外をスローします。
    /// </remarks>
    /// <param name="build">モニタ対象のデバイスとコールバックを登録するビルダー操作を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="ArgumentException">モニタ登録の点数上限を超過した場合に例外をスローします（分割不可）。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <returns>登録済みデバイスを繰り返し読み出すための <see cref="MonitorSession"/>。</returns>
    public async Task<MonitorSession> MonitorRegistAsync(Action<MonitorBuilder> build)
    {
        var builder = new MonitorBuilder();
        build(builder);

        var wordAddresses = builder.wordEntries.Select(e => (e.prefix, e.address)).ToArray();
        var doubleWordAddresses = builder.doubleWordEntries.Select(e => (e.prefix, e.address)).ToArray();

        await MonitorRegistAsync(wordAddresses, doubleWordAddresses);

        return new MonitorSession(this, builder);
    }

#if !AOT
    /// <summary>
    /// 文字列読み込み
    /// </summary>
    /// <remarks>
    /// 指定したデバイス範囲の値を文字列として、PLCから読み込みます。
    /// </remarks>
    /// <param name="prefix">読み込み対象の先頭デバイスコードを指定します。</param>
    /// <param name="address">読み込み対象の先頭アドレスを指定します。</param>
    /// <param name="length">読み込みデバイス点数を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <returns>PLCから読み込んだ値（Shift_JIS）を文字列に変換して返します。</returns>
    public string ReadString(Prefix prefix, string address, ushort length)
    {
        return DeviceConverter.ConvertString(
            BatchRead<byte>(prefix, address, length)
        );
    }

    /// <summary>
    /// 文字列読み込み（非同期）
    /// </summary>
    /// <remarks>
    /// 指定したデバイス範囲の値を文字列として、非同期でPLCから読み込みます。
    /// </remarks>
    /// <param name="prefix">読み込み対象の先頭デバイスコードを指定します。</param>
    /// <param name="address">読み込み対象の先頭アドレスを指定します。</param>
    /// <param name="length">読み込みデバイス点数を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <returns>PLCから読み込んだ値（Shift_JIS）を文字列に変換して返します。</returns>
    public async Task<string> ReadStringAsync(Prefix prefix, string address, ushort length)
    {
        return DeviceConverter.ConvertString(
            await BatchReadAsync<byte>(prefix, address, length)
        );
    }

    /// <summary>
    /// 文字列書き込み
    /// </summary>
    /// <remarks>
    /// 指定したデバイスに対して、Shift_JISに変換した文字列データをPLCへ書き込みます。
    /// </remarks>
    /// <param name="prefix">書き込み対象の先頭デバイスコードを指定します。</param>
    /// <param name="address">書き込み対象の先頭アドレスを指定します。</param>
    /// <param name="value">書き込みする文字列を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    public void WriteString(Prefix prefix, string address, string value)
    {
        BatchWrite<ushort>(prefix, address, DeviceConverter.ConvertStringToUshorts(value));
    }

    /// <summary>
    /// 文字列書き込み（非同期）
    /// </summary>
    /// <remarks>
    /// 指定したデバイスに対して、Shift_JISに変換した文字列データを非同期でPLCへ書き込みます。
    /// </remarks>
    /// <param name="prefix">書き込み対象の先頭デバイスコードを指定します。</param>
    /// <param name="address">書き込み対象の先頭アドレスを指定します。</param>
    /// <param name="value">書き込みする文字列を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    public async Task WriteStringAsync(Prefix prefix, string address, string value)
    {
        await BatchWriteAsync<ushort>(prefix, address, DeviceConverter.ConvertStringToUshorts(value));
    }
#endif

    /// <summary>
    /// インスタンス破棄
    /// </summary>
    /// <remarks>
    /// 使用済みのリソースを解放し、必要に応じてPLCのリモートロックを実行します。
    /// </remarks>
    public override void Dispose()
    {
        if (password != null) 
        {
            RemoteLock(password);
        }

        base.Dispose();
    }
}
