
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacketUtil
{
    public class PacketConvertLittleEndian : IPacketConvert
    {
        public float GetFloat(byte[] packet, int bytestart)
        {
            byte[] bytes = new byte[4];
      
            for (int i = 0; i < 4; i++)
            {
                bytes[i] = packet[bytestart + i];
            }
     
            return BitConverter.ToSingle(bytes, 0);
        }

        public double GetDouble(byte[] packet, int bytestart)
        {
            byte[] bytes = new byte[8];

            for (int i = 0; i < 8; i++)
            {
                bytes[i] = packet[bytestart + i];
            }

            return BitConverter.ToDouble(bytes, 0);
        }

        public Int64 GetSigned(byte[] packet, int bytestart, int bytesize)
        {

            switch (bytesize)
            {
                case 1: return (char)packet[bytestart];
                case 2:
                    return (Int16)(
                            packet[bytestart + 0] |
                            packet[bytestart + 1] << 8);
                case 4:
                    return (Int32)(
                            packet[bytestart + 0] |
                            packet[bytestart + 1] << 8 |
                            packet[bytestart + 2] << 16 |
                            packet[bytestart + 3] << 24);
                case 8:
                    return (Int64)(
                            (UInt64)packet[bytestart + 0] |
                            (UInt64)packet[bytestart + 1] << 8 |
                            (UInt64)packet[bytestart + 2] << 16 |
                            (UInt64)packet[bytestart + 3] << 24 |
                            (UInt64)packet[bytestart + 4] << 32 |
                            (UInt64)packet[bytestart + 5] << 40 |
                            (UInt64)packet[bytestart + 6] << 48 |
                            (UInt64)packet[bytestart + 7] << 56);
            }


            return 0;

        }

        public UInt64 GetUnsigned(byte[] packet, int bytestart, int bytesize)
        {

            switch (bytesize)
            {
                case 1: return (byte)packet[bytestart];
                case 2:
                    return (UInt16)(
                            packet[bytestart + 0] |
                            packet[bytestart + 1] << 8);
                case 4:
                    return (UInt32)(
                            packet[bytestart + 0] |
                            packet[bytestart + 1] << 8 |
                            packet[bytestart + 2] << 16 |
                            packet[bytestart + 3] << 24);
                case 8:
                    return (UInt64)(
                            (UInt64)packet[bytestart + 0] |
                            (UInt64)packet[bytestart + 1] << 8 |
                            (UInt64)packet[bytestart + 2] << 16 |
                            (UInt64)packet[bytestart + 3] << 24 |
                            (UInt64)packet[bytestart + 4] << 32 |
                            (UInt64)packet[bytestart + 5] << 40 |
                            (UInt64)packet[bytestart + 6] << 48 |
                            (UInt64)packet[bytestart + 7] << 56);
            }


            return 0;

        }

        public void SetFloat(byte[] pdata, int bytestart, string value)
        {
            float dvalue = float.Parse(value);
            Byte[] bytes = BitConverter.GetBytes(dvalue);
            for (int i = 0; i < 4; i++)
            {
                pdata[bytestart + i] = bytes[i];
            }
        }

        public void SetDouble(byte[] pdata, int bytestart, string value)
        {
            double dvalue = double.Parse(value);
            Byte[] bytes = BitConverter.GetBytes(dvalue);
            for (int i = 0; i < 8; i++)
            {
                pdata[bytestart + i] = bytes[i];
            }
        }

        public void SetSigned(byte[] pdata, int bytestart, int bytesize, double lsb, string format, string value)
        {
            Int64 ivalue = 0;
            if (value == "")
                ivalue = 0;
            else if (format[0] == 'F')
                ivalue = (Int64)(float.Parse(value) / lsb);
            else if (format[0] == 'D')
                ivalue = Int64.Parse(value);
            else if (format[0] == 'X')
                ivalue = Convert.ToInt64(value, 16);

            switch (bytesize)
            {
                case 1: pdata[bytestart] = (byte)(ivalue & 0xff); break;
                case 2:
                    pdata[bytestart + 0] = (byte)(ivalue >> 0 & 0xff);
                    pdata[bytestart + 1] = (byte)(ivalue >> 8 & 0xff);
                    break;
                case 4:
                    pdata[bytestart + 0] = (byte)(ivalue >> 0 & 0xff);
                    pdata[bytestart + 1] = (byte)(ivalue >> 8 & 0xff);
                    pdata[bytestart + 2] = (byte)(ivalue >> 16 & 0xff);
                    pdata[bytestart + 3] = (byte)(ivalue >> 24 & 0xff);
                    break;
                case 8:
                    pdata[bytestart + 0] = (byte)(ivalue >> 0 & 0xff);
                    pdata[bytestart + 1] = (byte)(ivalue >> 8 & 0xff);
                    pdata[bytestart + 2] = (byte)(ivalue >> 16 & 0xff);
                    pdata[bytestart + 3] = (byte)(ivalue >> 24 & 0xff);
                    pdata[bytestart + 4] = (byte)(ivalue >> 32 & 0xff);
                    pdata[bytestart + 5] = (byte)(ivalue >> 40 & 0xff);
                    pdata[bytestart + 6] = (byte)(ivalue >> 48 & 0xff);
                    pdata[bytestart + 7] = (byte)(ivalue >> 56 & 0xff);
                    break;
            }
        }
        public void SetUnsigned(byte[] pdata, int bytestart, int bytesize, double lsb, string format, string value)
        {   
            UInt64 ivalue = 0;
            if (value == "")
                ivalue = 0;
            else if (format[0] == 'F')
                ivalue = (UInt64)(float.Parse(value) / lsb);
            else if (format[0] == 'D')
                ivalue = UInt64.Parse(value);
            else if (format[0] == 'X')
                ivalue = Convert.ToUInt64(value, 16);

            switch (bytesize)
            {
                case 1: pdata[bytestart] = (byte)(ivalue & 0xff); break;
                case 2:
                    pdata[bytestart + 0] = (byte)(ivalue >> 0 & 0xff);
                    pdata[bytestart + 1] = (byte)(ivalue >> 8 & 0xff);
                    break;
                case 4:
                    pdata[bytestart + 0] = (byte)(ivalue >> 0 & 0xff);
                    pdata[bytestart + 1] = (byte)(ivalue >> 8 & 0xff);
                    pdata[bytestart + 2] = (byte)(ivalue >> 16 & 0xff);
                    pdata[bytestart + 3] = (byte)(ivalue >> 24 & 0xff);
                    break;
                case 8:
                    pdata[bytestart + 0] = (byte)(ivalue >> 0 & 0xff);
                    pdata[bytestart + 1] = (byte)(ivalue >> 8 & 0xff);
                    pdata[bytestart + 2] = (byte)(ivalue >> 16 & 0xff);
                    pdata[bytestart + 3] = (byte)(ivalue >> 24 & 0xff);
                    pdata[bytestart + 4] = (byte)(ivalue >> 32 & 0xff);
                    pdata[bytestart + 5] = (byte)(ivalue >> 40 & 0xff);
                    pdata[bytestart + 6] = (byte)(ivalue >> 48 & 0xff);
                    pdata[bytestart + 7] = (byte)(ivalue >> 56 & 0xff);
                    break;
            }
        }


        public string GetHexString(byte[] pdata, int bytestart, int bytesize)
        {
            string res = "0x";
            try
            {
                for (int i = bytestart + bytesize - 1; i >= bytestart; i--)
                {
                    res += $"{pdata[i]:X02}";
                }
            }
            catch
            {
            }
            return res;
        }

        public void SetHexString(byte[] pdata, int bytestart, int bytesize, string value)
        {
            for (int i = bytestart + bytesize - 1; i >= bytestart; i--)
            {
                byte hex = 0;
                try
                {
                    hex = Convert.ToByte(value.Substring((i - bytestart) * 2, 2), 16);
                }
                catch
                {
                }
                pdata[i] = hex;

            }
        }
    }
}
