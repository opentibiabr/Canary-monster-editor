using System;
using System.IO;

namespace SevenZip
{
    public class DataErrorException : ApplicationException { public DataErrorException() : base("Data Error") { } }
    public class InvalidParamException : ApplicationException { public InvalidParamException() : base("Invalid Parameter") { } }

    public interface ICodeProgress { void SetProgress(Int64 inSize, Int64 outSize); }
    public interface ICoder { void Code(Stream inStream, Stream outStream, Int64 inSize, Int64 outSize, ICodeProgress progress); }
    public interface ISetDecoderProperties { void SetDecoderProperties(byte[] properties); }
}

namespace SevenZip.Compression.LZ
{
    public class OutWindow
    {
        byte[] _buffer = null;
        uint _pos;
        uint _windowSize = 0;
        uint _streamPos;
        Stream _stream;

        public uint TrainSize = 0;

        public void Create(uint windowSize)
        {
            if (_windowSize != windowSize)
            {
                _buffer = new byte[windowSize];
            }
            _windowSize = windowSize;
            _pos = 0;
            _streamPos = 0;
        }

        public void Init(Stream stream, bool solid)
        {
            ReleaseStream();
            _stream = stream;
            if (!solid)
            {
                _streamPos = 0;
                _pos = 0;
                TrainSize = 0;
            }
        }

        public bool Train(Stream stream)
        {
            long len = stream.Length;
            uint len32 = (uint)((len < _windowSize) ? len : _windowSize);
            TrainSize = len32;
            stream.Position = len - len32;
            _streamPos = len32;
            _pos = 0;
            while (len32 > 0)
            {
                uint curSize = _windowSize - _pos;
                if (len32 < curSize) curSize = len32;
                int numReadBytes = stream.Read(_buffer, (int)_pos, (int)curSize);
                if (numReadBytes == 0) return false;
                len32 -= (uint)numReadBytes;
                _pos += (uint)numReadBytes;
                _streamPos += (uint)numReadBytes;
                if (_pos == _windowSize) _streamPos = _pos = 0;
            }
            return true;
        }

        public void ReleaseStream()
        {
            Flush();
            _stream = null;
        }

        public void Flush()
        {
            uint size = _pos - _streamPos;
            if (size == 0) return;
            _stream.Write(_buffer, (int)_streamPos, (int)size);
            if (_pos >= _windowSize) _pos = 0;
            _streamPos = _pos;
        }

        public void CopyBlock(uint distance, uint len)
        {
            uint pos = _pos - distance - 1;
            if (pos >= _windowSize) pos += _windowSize;
            for (; len > 0; len--)
            {
                if (pos >= _windowSize) pos = 0;
                _buffer[_pos++] = _buffer[pos++];
                if (_pos >= _windowSize) Flush();
            }
        }

        public void PutByte(byte b)
        {
            _buffer[_pos++] = b;
            if (_pos >= _windowSize) Flush();
        }

        public byte GetByte(uint distance)
        {
            uint pos = _pos - distance - 1;
            if (pos >= _windowSize) pos += _windowSize;
            return _buffer[pos];
        }
    }
}

namespace SevenZip.Compression.RangeCoder
{
    class Decoder
    {
        public const uint kTopValue = (1 << 24);
        public uint Range;
        public uint Code;
        public Stream Stream;

        public void Init(Stream stream)
        {
            Stream = stream;
            Code = 0;
            Range = 0xFFFFFFFF;
            for (int i = 0; i < 5; i++)
                Code = (Code << 8) | (byte)Stream.ReadByte();
        }

        public void ReleaseStream() { Stream = null; }

        public void Normalize()
        {
            while (Range < kTopValue)
            {
                Code = (Code << 8) | (byte)Stream.ReadByte();
                Range <<= 8;
            }
        }

        public uint DecodeDirectBits(int numTotalBits)
        {
            uint range = Range;
            uint code = Code;
            uint result = 0;
            for (int i = numTotalBits; i > 0; i--)
            {
                range >>= 1;
                uint t = (code - range) >> 31;
                code -= range & (t - 1);
                result = (result << 1) | (1 - t);

                if (range < kTopValue)
                {
                    code = (code << 8) | (byte)Stream.ReadByte();
                    range <<= 8;
                }
            }
            Range = range;
            Code = code;
            return result;
        }

        public uint DecodeBit(uint size0, int numTotalBits)
        {
            uint newBound = (Range >> numTotalBits) * size0;
            uint symbol;
            if (Code < newBound)
            {
                symbol = 0;
                Range = newBound;
            }
            else
            {
                symbol = 1;
                Code -= newBound;
                Range -= newBound;
            }
            Normalize();
            return symbol;
        }
    }

    struct BitDecoder
    {
        public const int kNumBitModelTotalBits = 11;
        public const uint kBitModelTotal = (1 << kNumBitModelTotalBits);
        const int kNumMoveBits = 5;

        uint Prob;

        public void Init() { Prob = kBitModelTotal >> 1; }

        public uint Decode(Decoder rangeDecoder)
        {
            uint newBound = (rangeDecoder.Range >> kNumBitModelTotalBits) * Prob;
            if (rangeDecoder.Code < newBound)
            {
                rangeDecoder.Range = newBound;
                Prob += (kBitModelTotal - Prob) >> kNumMoveBits;
                if (rangeDecoder.Range < Decoder.kTopValue)
                {
                    rangeDecoder.Code = (rangeDecoder.Code << 8) | (byte)rangeDecoder.Stream.ReadByte();
                    rangeDecoder.Range <<= 8;
                }
                return 0;
            }
            else
            {
                rangeDecoder.Range -= newBound;
                rangeDecoder.Code -= newBound;
                Prob -= (Prob) >> kNumMoveBits;
                if (rangeDecoder.Range < Decoder.kTopValue)
                {
                    rangeDecoder.Code = (rangeDecoder.Code << 8) | (byte)rangeDecoder.Stream.ReadByte();
                    rangeDecoder.Range <<= 8;
                }
                return 1;
            }
        }
    }

    struct BitTreeDecoder
    {
        BitDecoder[] Models;
        int NumBitLevels;

        public BitTreeDecoder(int numBitLevels)
        {
            NumBitLevels = numBitLevels;
            Models = new BitDecoder[1 << numBitLevels];
        }

        public void Init()
        {
            for (uint i = 1; i < (1 << NumBitLevels); i++)
                Models[i].Init();
        }

        public uint Decode(Decoder rangeDecoder)
        {
            uint m = 1;
            for (int bitIndex = NumBitLevels; bitIndex > 0; bitIndex--)
                m = (m << 1) + Models[m].Decode(rangeDecoder);
            return m - ((uint)1 << NumBitLevels);
        }

        public uint ReverseDecode(Decoder rangeDecoder)
        {
            uint m = 1;
            uint symbol = 0;
            for (int bitIndex = 0; bitIndex < NumBitLevels; bitIndex++)
            {
                uint bit = Models[m].Decode(rangeDecoder);
                m <<= 1;
                m += bit;
                symbol |= (bit << bitIndex);
            }
            return symbol;
        }

        public static uint ReverseDecode(BitDecoder[] Models, uint startIndex, Decoder rangeDecoder, int NumBitLevels)
        {
            uint m = 1;
            uint symbol = 0;
            for (int bitIndex = 0; bitIndex < NumBitLevels; bitIndex++)
            {
                uint bit = Models[startIndex + m].Decode(rangeDecoder);
                m <<= 1;
                m += bit;
                symbol |= (bit << bitIndex);
            }
            return symbol;
        }
    }
}

namespace SevenZip.Compression.LZMA
{
    internal abstract class Base
    {
        public const uint kNumRepDistances = 4;
        public const uint kNumStates = 12;
        public struct State
        {
            public uint Index;
            public void Init() { Index = 0; }
            public void UpdateChar()
            {
                if (Index < 4) Index = 0;
                else if (Index < 10) Index -= 3;
                else Index -= 6;
            }
            public void UpdateMatch() { Index = (uint)(Index < 7 ? 7 : 10); }
            public void UpdateRep() { Index = (uint)(Index < 7 ? 8 : 11); }
            public void UpdateShortRep() { Index = (uint)(Index < 7 ? 9 : 11); }
            public bool IsCharState() { return Index < 7; }
        }
        public const int kNumPosSlotBits = 6;
        public const int kNumLenToPosStatesBits = 2;
        public const uint kNumLenToPosStates = 1 << kNumLenToPosStatesBits;
        public const uint kMatchMinLen = 2;
        public static uint GetLenToPosState(uint len)
        {
            len -= kMatchMinLen;
            if (len < kNumLenToPosStates) return len;
            return (uint)(kNumLenToPosStates - 1);
        }
        public const int kNumAlignBits = 4;
        public const uint kAlignTableSize = 1 << kNumAlignBits;
        public const uint kStartPosModelIndex = 4;
        public const uint kEndPosModelIndex = 14;
        public const uint kNumFullDistances = 1 << ((int)kEndPosModelIndex / 2);
        public const int kNumPosStatesBitsMax = 4;
        public const uint kNumPosStatesMax = (1 << kNumPosStatesBitsMax);
        public const int kNumLowLenBits = 3;
        public const int kNumMidLenBits = 3;
        public const int kNumHighLenBits = 8;
        public const uint kNumLowLenSymbols = 1 << kNumLowLenBits;
        public const uint kNumMidLenSymbols = 1 << kNumMidLenBits;
    }

    public class Decoder : ICoder, ISetDecoderProperties
    {
        class LenDecoder
        {
            SevenZip.Compression.RangeCoder.BitDecoder m_Choice = new SevenZip.Compression.RangeCoder.BitDecoder();
            SevenZip.Compression.RangeCoder.BitDecoder m_Choice2 = new SevenZip.Compression.RangeCoder.BitDecoder();
            SevenZip.Compression.RangeCoder.BitTreeDecoder[] m_LowCoder = new SevenZip.Compression.RangeCoder.BitTreeDecoder[Base.kNumPosStatesMax];
            SevenZip.Compression.RangeCoder.BitTreeDecoder[] m_MidCoder = new SevenZip.Compression.RangeCoder.BitTreeDecoder[Base.kNumPosStatesMax];
            SevenZip.Compression.RangeCoder.BitTreeDecoder m_HighCoder = new SevenZip.Compression.RangeCoder.BitTreeDecoder(Base.kNumHighLenBits);
            uint m_NumPosStates = 0;

            public void Create(uint numPosStates)
            {
                for (uint posState = m_NumPosStates; posState < numPosStates; posState++)
                {
                    m_LowCoder[posState] = new SevenZip.Compression.RangeCoder.BitTreeDecoder(Base.kNumLowLenBits);
                    m_MidCoder[posState] = new SevenZip.Compression.RangeCoder.BitTreeDecoder(Base.kNumMidLenBits);
                }
                m_NumPosStates = numPosStates;
            }

            public void Init()
            {
                m_Choice.Init();
                for (uint posState = 0; posState < m_NumPosStates; posState++)
                {
                    m_LowCoder[posState].Init();
                    m_MidCoder[posState].Init();
                }
                m_Choice2.Init();
                m_HighCoder.Init();
            }

            public uint Decode(SevenZip.Compression.RangeCoder.Decoder rangeDecoder, uint posState)
            {
                if (m_Choice.Decode(rangeDecoder) == 0)
                    return m_LowCoder[posState].Decode(rangeDecoder);
                else
                {
                    uint symbol = Base.kNumLowLenSymbols;
                    if (m_Choice2.Decode(rangeDecoder) == 0)
                        symbol += m_MidCoder[posState].Decode(rangeDecoder);
                    else
                    {
                        symbol += Base.kNumMidLenSymbols;
                        symbol += m_HighCoder.Decode(rangeDecoder);
                    }
                    return symbol;
                }
            }
        }

        class LiteralDecoder
        {
            struct Decoder2
            {
                SevenZip.Compression.RangeCoder.BitDecoder[] m_Decoders;
                public void Create() { m_Decoders = new SevenZip.Compression.RangeCoder.BitDecoder[0x300]; }
                public void Init() { for (int i = 0; i < 0x300; i++) m_Decoders[i].Init(); }

                public byte DecodeNormal(SevenZip.Compression.RangeCoder.Decoder rangeDecoder)
                {
                    uint symbol = 1;
                    do
                        symbol = (symbol << 1) | m_Decoders[symbol].Decode(rangeDecoder);
                    while (symbol < 0x100);
                    return (byte)symbol;
                }

                public byte DecodeWithMatchByte(SevenZip.Compression.RangeCoder.Decoder rangeDecoder, byte matchByte)
                {
                    uint symbol = 1;
                    do
                    {
                        uint matchBit = (uint)(matchByte >> 7) & 1;
                        matchByte <<= 1;
                        uint bit = m_Decoders[((1 + matchBit) << 8) + symbol].Decode(rangeDecoder);
                        symbol = (symbol << 1) | bit;
                        if (matchBit != bit)
                        {
                            while (symbol < 0x100)
                                symbol = (symbol << 1) | m_Decoders[symbol].Decode(rangeDecoder);
                            break;
                        }
                    }
                    while (symbol < 0x100);
                    return (byte)symbol;
                }
            }

            Decoder2[] m_Coders;
            int m_NumPrevBits;
            int m_NumPosBits;
            uint m_PosMask;

            public void Create(int numPosBits, int numPrevBits)
            {
                if (m_Coders != null && m_NumPrevBits == numPrevBits && m_NumPosBits == numPosBits) return;
                m_NumPosBits = numPosBits;
                m_PosMask = ((uint)1 << numPosBits) - 1;
                m_NumPrevBits = numPrevBits;
                uint numStates = (uint)1 << (m_NumPrevBits + m_NumPosBits);
                m_Coders = new Decoder2[numStates];
                for (uint i = 0; i < numStates; i++) m_Coders[i].Create();
            }

            public void Init()
            {
                uint numStates = (uint)1 << (m_NumPrevBits + m_NumPosBits);
                for (uint i = 0; i < numStates; i++) m_Coders[i].Init();
            }

            uint GetState(uint pos, byte prevByte)
            { return ((pos & m_PosMask) << m_NumPrevBits) + (uint)(prevByte >> (8 - m_NumPrevBits)); }

            public byte DecodeNormal(SevenZip.Compression.RangeCoder.Decoder rangeDecoder, uint pos, byte prevByte)
            { return m_Coders[GetState(pos, prevByte)].DecodeNormal(rangeDecoder); }

            public byte DecodeWithMatchByte(SevenZip.Compression.RangeCoder.Decoder rangeDecoder, uint pos, byte prevByte, byte matchByte)
            { return m_Coders[GetState(pos, prevByte)].DecodeWithMatchByte(rangeDecoder, matchByte); }
        }

        SevenZip.Compression.LZ.OutWindow m_OutWindow = new SevenZip.Compression.LZ.OutWindow();
        SevenZip.Compression.RangeCoder.Decoder m_RangeDecoder = new SevenZip.Compression.RangeCoder.Decoder();

        SevenZip.Compression.RangeCoder.BitDecoder[] m_IsMatchDecoders = new SevenZip.Compression.RangeCoder.BitDecoder[Base.kNumStates << Base.kNumPosStatesBitsMax];
        SevenZip.Compression.RangeCoder.BitDecoder[] m_IsRepDecoders = new SevenZip.Compression.RangeCoder.BitDecoder[Base.kNumStates];
        SevenZip.Compression.RangeCoder.BitDecoder[] m_IsRepG0Decoders = new SevenZip.Compression.RangeCoder.BitDecoder[Base.kNumStates];
        SevenZip.Compression.RangeCoder.BitDecoder[] m_IsRepG1Decoders = new SevenZip.Compression.RangeCoder.BitDecoder[Base.kNumStates];
        SevenZip.Compression.RangeCoder.BitDecoder[] m_IsRepG2Decoders = new SevenZip.Compression.RangeCoder.BitDecoder[Base.kNumStates];
        SevenZip.Compression.RangeCoder.BitDecoder[] m_IsRep0LongDecoders = new SevenZip.Compression.RangeCoder.BitDecoder[Base.kNumStates << Base.kNumPosStatesBitsMax];

        SevenZip.Compression.RangeCoder.BitTreeDecoder[] m_PosSlotDecoder = new SevenZip.Compression.RangeCoder.BitTreeDecoder[Base.kNumLenToPosStates];
        SevenZip.Compression.RangeCoder.BitDecoder[] m_PosDecoders = new SevenZip.Compression.RangeCoder.BitDecoder[Base.kNumFullDistances - Base.kEndPosModelIndex];
        SevenZip.Compression.RangeCoder.BitTreeDecoder m_PosAlignDecoder = new SevenZip.Compression.RangeCoder.BitTreeDecoder(Base.kNumAlignBits);

        LenDecoder m_LenDecoder = new LenDecoder();
        LenDecoder m_RepLenDecoder = new LenDecoder();
        LiteralDecoder m_LiteralDecoder = new LiteralDecoder();

        uint m_DictionarySize;
        uint m_DictionarySizeCheck;
        uint m_PosStateMask;

        public Decoder()
        {
            m_DictionarySize = 0xFFFFFFFF;
            for (int i = 0; i < Base.kNumLenToPosStates; i++)
                m_PosSlotDecoder[i] = new SevenZip.Compression.RangeCoder.BitTreeDecoder(Base.kNumPosSlotBits);
        }

        void SetDictionarySize(uint dictionarySize)
        {
            if (m_DictionarySize != dictionarySize)
            {
                m_DictionarySize = dictionarySize;
                m_DictionarySizeCheck = Math.Max(m_DictionarySize, 1);
                uint blockSize = Math.Max(m_DictionarySizeCheck, (1 << 12));
                m_OutWindow.Create(blockSize);
            }
        }

        void SetLiteralProperties(int lp, int lc)
        {
            if (lp > 8) throw new InvalidParamException();
            if (lc > 8) throw new InvalidParamException();
            m_LiteralDecoder.Create(lp, lc);
        }

        void SetPosBitsProperties(int pb)
        {
            if (pb > Base.kNumPosStatesBitsMax) throw new InvalidParamException();
            uint numPosStates = (uint)1 << pb;
            m_LenDecoder.Create(numPosStates);
            m_RepLenDecoder.Create(numPosStates);
            m_PosStateMask = numPosStates - 1;
        }

        void Init(Stream inStream, Stream outStream)
        {
            m_RangeDecoder.Init(inStream);
            m_OutWindow.Init(outStream, false);

            uint i;
            for (i = 0; i < Base.kNumStates; i++)
            {
                for (uint j = 0; j <= m_PosStateMask; j++)
                {
                    uint index = (i << Base.kNumPosStatesBitsMax) + j;
                    m_IsMatchDecoders[index].Init();
                    m_IsRep0LongDecoders[index].Init();
                }
                m_IsRepDecoders[i].Init();
                m_IsRepG0Decoders[i].Init();
                m_IsRepG1Decoders[i].Init();
                m_IsRepG2Decoders[i].Init();
            }

            m_LiteralDecoder.Init();
            for (i = 0; i < Base.kNumLenToPosStates; i++) m_PosSlotDecoder[i].Init();
            for (i = 0; i < Base.kNumFullDistances - Base.kEndPosModelIndex; i++) m_PosDecoders[i].Init();

            m_LenDecoder.Init();
            m_RepLenDecoder.Init();
            m_PosAlignDecoder.Init();
        }

        public void Code(Stream inStream, Stream outStream, Int64 inSize, Int64 outSize, ICodeProgress progress)
        {
            Init(inStream, outStream);

            Base.State state = new Base.State();
            state.Init();
            uint rep0 = 0, rep1 = 0, rep2 = 0, rep3 = 0;

            UInt64 nowPos64 = 0;
            UInt64 outSize64 = (UInt64)outSize;
            if (nowPos64 < outSize64)
            {
                if (m_IsMatchDecoders[state.Index << Base.kNumPosStatesBitsMax].Decode(m_RangeDecoder) != 0)
                    throw new DataErrorException();
                state.UpdateChar();
                byte b = m_LiteralDecoder.DecodeNormal(m_RangeDecoder, 0, 0);
                m_OutWindow.PutByte(b);
                nowPos64++;
            }
            while (nowPos64 < outSize64)
            {
                uint posState = (uint)nowPos64 & m_PosStateMask;
                if (m_IsMatchDecoders[(state.Index << Base.kNumPosStatesBitsMax) + posState].Decode(m_RangeDecoder) == 0)
                {
                    byte b;
                    byte prevByte = m_OutWindow.GetByte(0);
                    if (!state.IsCharState())
                        b = m_LiteralDecoder.DecodeWithMatchByte(m_RangeDecoder, (uint)nowPos64, prevByte, m_OutWindow.GetByte(rep0));
                    else
                        b = m_LiteralDecoder.DecodeNormal(m_RangeDecoder, (uint)nowPos64, prevByte);
                    m_OutWindow.PutByte(b);
                    state.UpdateChar();
                    nowPos64++;
                }
                else
                {
                    uint len;
                    if (m_IsRepDecoders[state.Index].Decode(m_RangeDecoder) == 1)
                    {
                        if (m_IsRepG0Decoders[state.Index].Decode(m_RangeDecoder) == 0)
                        {
                            if (m_IsRep0LongDecoders[(state.Index << Base.kNumPosStatesBitsMax) + posState].Decode(m_RangeDecoder) == 0)
                            {
                                state.UpdateShortRep();
                                m_OutWindow.PutByte(m_OutWindow.GetByte(rep0));
                                nowPos64++;
                                continue;
                            }
                        }
                        else
                        {
                            UInt32 distance;
                            if (m_IsRepG1Decoders[state.Index].Decode(m_RangeDecoder) == 0) distance = rep1;
                            else
                            {
                                if (m_IsRepG2Decoders[state.Index].Decode(m_RangeDecoder) == 0) distance = rep2;
                                else { distance = rep3; rep3 = rep2; }
                                rep2 = rep1;
                            }
                            rep1 = rep0;
                            rep0 = distance;
                        }
                        len = m_RepLenDecoder.Decode(m_RangeDecoder, posState) + Base.kMatchMinLen;
                        state.UpdateRep();
                    }
                    else
                    {
                        rep3 = rep2; rep2 = rep1; rep1 = rep0;
                        len = Base.kMatchMinLen + m_LenDecoder.Decode(m_RangeDecoder, posState);
                        state.UpdateMatch();
                        uint posSlot = m_PosSlotDecoder[Base.GetLenToPosState(len)].Decode(m_RangeDecoder);
                        if (posSlot >= Base.kStartPosModelIndex)
                        {
                            int numDirectBits = (int)((posSlot >> 1) - 1);
                            rep0 = ((2 | (posSlot & 1)) << numDirectBits);
                            if (posSlot < Base.kEndPosModelIndex)
                                rep0 += SevenZip.Compression.RangeCoder.BitTreeDecoder.ReverseDecode(m_PosDecoders, rep0 - posSlot - 1, m_RangeDecoder, numDirectBits);
                            else
                            {
                                rep0 += (m_RangeDecoder.DecodeDirectBits(numDirectBits - Base.kNumAlignBits) << Base.kNumAlignBits);
                                rep0 += m_PosAlignDecoder.ReverseDecode(m_RangeDecoder);
                            }
                        }
                        else rep0 = posSlot;
                    }
                    if (rep0 >= m_OutWindow.TrainSize + nowPos64 || rep0 >= m_DictionarySizeCheck)
                    {
                        if (rep0 == 0xFFFFFFFF) break; // EOS
                        throw new DataErrorException();
                    }
                    m_OutWindow.CopyBlock(rep0, len);
                    nowPos64 += len;
                }
            }
            m_OutWindow.Flush();
            m_OutWindow.ReleaseStream();
            m_RangeDecoder.ReleaseStream();
        }

        public void SetDecoderProperties(byte[] properties)
        {
            if (properties.Length < 5) throw new InvalidParamException();
            int lc = properties[0] % 9;
            int remainder = properties[0] / 9;
            int lp = remainder % 5;
            int pb = remainder / 5;
            if (pb > Base.kNumPosStatesBitsMax) throw new InvalidParamException();
            UInt32 dictionarySize = 0;
            for (int i = 0; i < 4; i++) dictionarySize += ((UInt32)(properties[1 + i])) << (i * 8);
            SetDictionarySize(dictionarySize);
            SetLiteralProperties(lp, lc);
            SetPosBitsProperties(pb);
        }
    }
}
