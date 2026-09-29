using System;
using System.Globalization;

namespace PacketUtil
{
    public interface IPacketConvert
    {
        float GetFloat(byte[] packet, int bytestart);
        double GetDouble(byte[] packet, int bytestart);
        Int64 GetSigned(byte[] packet, int bytestart, int bytesize);
        UInt64 GetUnsigned(byte[] packet, int bytestart, int bytesize);
        string GetHexString(byte[] pdata, int bytestart, int bytesize);

        void SetFloat(byte[] pdata, int bytestart, string value);
        void SetDouble(byte[] pdata, int bytestart, string value);
        void SetSigned(byte[] pdata, int bytestart, int bytesize, double lsb, string format, string value);
        void SetUnsigned(byte[] pdata, int bytestart, int bytesize, double lsb, string format, string value);
        void SetHexString(byte[] pdata, int bytestart, int bytesize, string value);
    }
}



