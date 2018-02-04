using System;
using System.Collections;
using System.Text;

using BaseLib;

/// ReadXXX means normal read(read from end to start, cursor will --)
///  FReadXXX means forward read(Read from start to end, cursor will ++)
public class ByteBuffer
{
    private byte[] _buffer;
    private int _iCurosr;
    private int _nSize;

    public int size { get { return _nSize; } private set { } }
    public int position { get { return _iCurosr; } set { _iCurosr = value; } }

    /// Attach to a exist buffer
    public ByteBuffer(byte[] byteBuffer, int iCursorPos = 0)
    {
        _buffer = byteBuffer;
        _nSize = byteBuffer.Length;
        _iCurosr = iCursorPos;
    }

    public ByteBuffer(byte[] byteBuffer, int nSize, int iCursorPos = 0)
    {
        Debugger.Assert(nSize <= byteBuffer.Length);

        _buffer = byteBuffer;
        _nSize = nSize;
        _iCurosr = iCursorPos;
    }

    public ByteBuffer(int nSize)
    {
        _nSize = nSize;
        _buffer = new byte[nSize];
        _iCurosr = 0;
    }

    /// Test if still has remaining byte can be read.
    /// If isForward is true, means check forward reading.
    public bool HashRemaining(bool isForward = false)
    {
        return isForward ? _iCurosr < _nSize : _iCurosr > 0;
    }

    public byte[] GetInternalBuffer()
    {
        return _buffer;
    }

    /// Note: byte is uint8
    public void WriteByte(byte data)
    {
        Debugger.Assert(_iCurosr + 1 <= _nSize);

        _buffer[_iCurosr] = data;
        _iCurosr++;
    }

    /// Note: byte is uint8
    public byte ReadByte()
    {
        _iCurosr--;
        byte data = _buffer[_iCurosr];

        Debugger.Assert(_iCurosr >= 0);

        return data;
    }
    
    public byte FReadByte()
    {
        Debugger.Assert( ( _iCurosr + 1 ) <= _nSize);

        byte data = _buffer[_iCurosr];
        _iCurosr++;
        return data;
    }

    public void WriteInt(int data)
    {
        Debugger.Assert(_iCurosr + 4 <= _nSize);

        _buffer[_iCurosr + 3] = (byte)(data >> 0);
        _buffer[_iCurosr + 2] = (byte)(data >> 8);
        _buffer[_iCurosr + 1] = (byte)(data >> 16);
        _buffer[_iCurosr + 0] = (byte)(data >> 24);

        _iCurosr += 4;
    }

    public int ReadInt()
    {
        _iCurosr -= 4;

        int data = (int)((((_buffer[_iCurosr + 3] & 0xff) << 0)
                | ((_buffer[_iCurosr + 2] & 0xff) << 8)
                | ((_buffer[_iCurosr + 1] & 0xff) << 16) | ((_buffer[_iCurosr + 0] & 0xff) << 24)));  

        Debugger.Assert(_iCurosr >= 0);

        return data;
    }

    public int FReadInt()
    {
        Debugger.Assert( ( _iCurosr + 4 ) <= _nSize);

        int data = (int)((((_buffer[_iCurosr + 3] & 0xff) << 0)
                | ((_buffer[_iCurosr + 2] & 0xff) << 8)
                | ((_buffer[_iCurosr + 1] & 0xff) << 16) | ((_buffer[_iCurosr + 0] & 0xff) << 24)));

        _iCurosr += 4;

        return data;
    }

    public void WriteShort(short data)
    {
        Debugger.Assert(_iCurosr + 2 <= _nSize);

        _buffer[_iCurosr + 1] = (byte)(data >> 0);
        _buffer[_iCurosr + 0] = (byte)(data >> 8);

        _iCurosr += 2;
    }

    public void WriteUShort(ushort data)
    {
        Debugger.Assert(_iCurosr + 2 <= _nSize);

        _buffer[_iCurosr + 1] = (byte)(data >> 0);
        _buffer[_iCurosr + 0] = (byte)(data >> 8);

        _iCurosr += 2;
    }

    public short ReadShort()
    {
        _iCurosr -= 2;

        short data = (short)(((_buffer[_iCurosr + 1] & 0xff) | _buffer[_iCurosr + 0] << 8)); ;

        Debugger.Assert(_iCurosr >= 0);

        return data;
    }

    public ushort ReadUShort()
    {
        _iCurosr -= 2;

        ushort data = (ushort)(((_buffer[_iCurosr + 1] & 0xff) | _buffer[_iCurosr + 0] << 8)); ;

        Debugger.Assert(_iCurosr >= 0);

        return data;
    }

    public short FReadShort()
    {
        Debugger.Assert((_iCurosr + 2) <= _nSize);

        short data = (short)(((_buffer[_iCurosr + 1] & 0xff) | _buffer[_iCurosr + 0] << 8)); ;
        _iCurosr += 2;

        return data;
    }

    public ushort FReadUShort()
    {
        Debugger.Assert((_iCurosr + 2) <= _nSize);

        ushort data = (ushort)(((_buffer[_iCurosr + 1] & 0xff) | _buffer[_iCurosr + 0] << 8)); ;
        _iCurosr += 2;

        return data;
    }

    public void WriteBytes(byte[] data)
    {
        Debugger.Assert(_iCurosr + data.Length <= _nSize);

        Buffer.BlockCopy(data, 0, _buffer, _iCurosr, data.Length);
        _iCurosr += data.Length;
    }

    public byte[] ReadBytes(int nLength)
    {
        _iCurosr -= nLength;

        byte[] data = new byte[nLength];
        Buffer.BlockCopy(_buffer, _iCurosr, data, 0, nLength);

        Debugger.Assert(_iCurosr >= 0);

        return data;
    }

    public byte[] FReadBytes(int nLength)
    {
        Debugger.Assert((_iCurosr + nLength ) <= _nSize);

        byte[] data = new byte[nLength];
        Buffer.BlockCopy(_buffer, _iCurosr, data, 0, nLength);

        _iCurosr += nLength;

        return data;
    }

    public void WriteString(string data)
    {
        Debugger.Assert(_iCurosr + data.Length <= _nSize);

        byte[] bytes = Encoding.UTF8.GetBytes(data);
        Buffer.BlockCopy(bytes, 0, _buffer, _iCurosr, data.Length);
        _iCurosr += data.Length;
    }

    public string ReadString(int nLength)
    {
        _iCurosr -= nLength;
        string data = Encoding.UTF8.GetString(_buffer, _iCurosr, nLength);
        
        Debugger.Assert(_iCurosr >= 0);

        return data;
    }

    public string FReadString(int nLength)
    {
        Debugger.Assert((_iCurosr + nLength) <= _nSize);

        string data = Encoding.UTF8.GetString(_buffer, _iCurosr, nLength);
        _iCurosr += nLength;

        return data;
    }
}

