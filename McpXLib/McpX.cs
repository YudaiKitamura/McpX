using McpXLib.Commands;
using McpXLib.Enums;
using McpXLib.Utils;


namespace McpXLib;
public class McpX : Mcp
{
    private readonly string? password;

    public McpX(
        string ip, 
        int port, 
        string? password = null, 
        bool isAscii = false, 
        bool isUdp = false, 
        RequestFrame requestFrame = RequestFrame.E3
    ) : base (
        ip: ip, 
        port: port,
        isAscii: isAscii, 
        isUdp: isUdp, 
        requestFrame: requestFrame
    )
    {
        this.password = password;

        if (password != null) 
        {
            RemoteUnlock(password);
        }
    }

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
                        wordLength: (ushort)readLength
                    )
                );
            }

            return values.ToArray();
        }
    }

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
                        wordLength: (ushort)readLength
                    )
                );
            }

            return values.ToArray();
        }
    }

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

    public (T1[] wordValues, T2[] doubleValues) RandomRead<T1, T2>((Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses)
        where T1 : unmanaged
        where T2 : unmanaged
    {
        List<T1> allWordValues = new List<T1>();
        List<T2> allDoubleValues = new List<T2>();

        int wordIndex = 0, doubleIndex = 0;
        while (wordIndex < wordAddresses.Length || doubleIndex < doubleWordAddresses.Length)
        {
            var wordBatch = wordAddresses.Skip(wordIndex).Take(WordRandomReadCommand<T1, T2>.MAX_WORD_LENGTH).ToArray();
            var doubleBatch = doubleWordAddresses.Skip(doubleIndex).Take(WordRandomReadCommand<T1, T2>.MAX_WORD_LENGTH - wordBatch.Length).ToArray();
            
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

    public async Task<(T1[] wordValues, T2[] doubleValues)> RandomReadAsync<T1, T2>((Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses)
        where T1 : unmanaged
        where T2 : unmanaged
    {
        List<T1> allWordValues = new List<T1>();
        List<T2> allDoubleValues = new List<T2>();

        int wordIndex = 0, doubleIndex = 0;
        while (wordIndex < wordAddresses.Length || doubleIndex < doubleWordAddresses.Length)
        {
            var wordBatch = wordAddresses.Skip(wordIndex).Take(WordRandomReadCommand<T1, T2>.MAX_WORD_LENGTH).ToArray();
            var doubleBatch = doubleWordAddresses.Skip(doubleIndex).Take(WordRandomReadCommand<T1, T2>.MAX_WORD_LENGTH - wordBatch.Length).ToArray();
            
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

    public void RandomWrite<T1, T2>(
        (Prefix prefix, string address, T1 value)[] wordDevices,
        (Prefix prefix, string address, T2 value)[] doubleWordDevices)
        where T1 : unmanaged
        where T2 : unmanaged
    {
        int maxSize = WordRandomWriteCommand<T1, T2>.MAX_WORD_LENGTH;
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

    public async Task RandomWriteAsync<T1, T2>(
        (Prefix prefix, string address, T1 value)[] wordDevices,
        (Prefix prefix, string address, T2 value)[] doubleWordDevices)
        where T1 : unmanaged
        where T2 : unmanaged
    {
        int maxSize = WordRandomWriteCommand<T1, T2>.MAX_WORD_LENGTH;
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

    public string ReadString(Prefix prefix, string address, ushort length)
    {
        return DeviceConverter.ConvertString(
            BatchRead<byte>(prefix, address, length)
        );
    }

    public async Task<string> ReadStringAsync(Prefix prefix, string address, ushort length)
    {
        return DeviceConverter.ConvertString(
            await BatchReadAsync<byte>(prefix, address, length)
        );
    }

    public void WriteString(Prefix prefix, string address, string value)
    {
        BatchWrite<ushort>(prefix, address, DeviceConverter.ConvertStringToUshorts(value));
    }

    public async Task WriteStringAsync(Prefix prefix, string address, string value)
    {
        await BatchWriteAsync<ushort>(prefix, address, DeviceConverter.ConvertStringToUshorts(value));
    }

    public override void Dispose()
    {
        if (password != null) 
        {
            RemoteLock(password);
        }

        base.Dispose();
    }
}