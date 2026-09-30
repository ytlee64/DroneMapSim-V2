using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Interop;

namespace PacketUtil
{
    public partial class Packet: ObservableObject
    {
        public const int START_LOGGING_CSV= 11;
        public const int START_LOGGING_BINARY = 12;
        public const int STOP_LOGGING = 22;

        public int RecvCount = 0;

        [ObservableProperty]
        private string _name = "Packet";

        [ObservableProperty]
        private string _packetStatus = "";

        [ObservableProperty]
        private ObservableCollection<PacketItem> _items = new();

        public int PacketLength = 0;

        protected string ListType = "Value";
        protected byte[] ByteData = new byte[0];
        protected Packet()
        {
            WeakReferenceMessenger.Default.Register<MessangerCmd>(this,OnCommand);
        }

        public IPacketConvert pConvertor = new PacketConvertBigEndian();

        
        public void SetByteData(byte[] data)
        {
            Array.Copy(data, ByteData, Math.Min(ByteData.Length,data.Length));
            ByteDataToItems();
            //Log();
        }

        public void SetByteData(byte[] data,int offset)
        {
            Array.Copy(data, offset, ByteData,0, Math.Min(ByteData.Length, data.Length));
            ByteDataToItems();
            //Log();
        }


        public byte[] GetByteData()
        {
            return this.ByteData;
        }

        public Logger? PacketLogger = null;
        
        public void Log()
        {
            //Console.WriteLine($"PacketLogger {PacketLogger.Mode}");

            if (PacketLogger!=null)
            {
                if (PacketLogger.Mode == START_LOGGING_CSV)
                {
                    string? msg = ToString();
                    msg = (msg == null) ? "" : msg;
                    PacketLogger.WriteLine(msg);
                }
                else
                {
                    PacketLogger.WriteBytes(GetByteData(), PacketLength);
                }
            }
        }


        public void OnCommand(object recipient, MessangerCmd cmd)
        {
            if(cmd.id== START_LOGGING_CSV)
            {
                PacketLogger = new(START_LOGGING_CSV,cmd.cmd,$"{Name}.csv", CsvHeader());
            }
            else if (cmd.id == START_LOGGING_BINARY)
            {
                Console.WriteLine($"{Name} START_LOGGING_BINARY");
                PacketLogger = new(START_LOGGING_BINARY,cmd.cmd, $"{Name}.bin", null);
            }
            else if (cmd.id == STOP_LOGGING)
            {
                PacketLogger = null;
            }
        }

        public void ReadFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("file does not exist: {0}", filePath);
                return;
            }
            string[] lines = File.ReadAllLines(filePath);
            
            StringListToItems(lines);
        }

        public virtual void StringListToItems(string[] lines) { }
        
        //public virtual void ItemsToByteData() 
        //{
        //    foreach (PacketItem item in Items)
        //    {
        //        item.SetByteData(pConvertor, ByteData, item.Value);
        //    }
        //}
        public virtual void ByteDataToItems()
        {
            foreach (PacketItem item in Items)
            {
                //Console.WriteLine(item.Name,item.ByteStart,item.ByteSize,PacketLength);
                item.fromByteData = true;
                item.Value = item.GetValue(pConvertor, ByteData);
                item.Hex = pConvertor.GetHexString(ByteData, item.ByteStart, item.ByteSize);
            }
        }

        public string CsvHeader()
        {
            string result = "";
            foreach (PacketItem item in Items)
            {
                if(item.IsSelected)
                    result += item.Name + ",";
            }
            return result;
        }
        public override string ToString()
        {
            string result = "";

            foreach (PacketItem item in Items)
            {
                if (item.IsSelected)
                    result += item.Value + ",";
            }
            return result;
        }
        public virtual void Save(string filename)
        {
            if (Items != null)
                using (StreamWriter sw = new StreamWriter(filename))
                {
                    foreach (PacketItem item in Items)
                    {
                        sw.WriteLine(item.Name + "=" + item.Value);
                    }
                }
        }

    }
}

