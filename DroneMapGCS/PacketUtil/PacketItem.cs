using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace PacketUtil
{
    public abstract partial class PacketItem : ObservableObject
    {
        public Packet? Parent = null;

        protected Dictionary<UInt64, string> _symboldict = new();


        protected string[] _bitlist = new string[0];
        public string[] BitList
        {
            get => _bitlist;
        }

        [ObservableProperty]
        public string _id;
        [ObservableProperty]
        public string _name;
        [ObservableProperty]
        public string _unit;
        [ObservableProperty]
        public string _symbol;
        [ObservableProperty]
        public string _hex;
        [ObservableProperty]
        public bool isSelected = true;

        public string Type = "";
        public string Format = "";
        public double Lsb = 1;
        public int ByteStart = 0;
        public int ByteSize = 0;
        public string DefValue = "";
        public string Comment = "";

        public bool fromByteData = false;


        protected string _value = "";

        protected double dvalue = 0;
        public string Value
        {
            get => _value;
            set
            {
                if (!fromByteData && Parent is not null)
                {
                    try 
                    { 
                        SetByteData(Parent.pConvertor, Parent.GetByteData(), value);
                    }catch
                    {
                        return;
                    }

                    Hex = Parent.pConvertor.GetHexString(Parent.GetByteData(), ByteStart, ByteSize);
                    SetProperty(ref _value, GetValue(Parent.pConvertor, Parent.GetByteData()));
                }
                else
                {
                    SetProperty(ref _value, value);
                    fromByteData = false;
                }
            }
        }


        public PacketItem(string id, string name, string unit, string type, string format, double lsb, int bytestart, int bytesize, string defvalue, string comment)
        {
            Id = id;
            Name = name;
            Unit = unit;
            Type = type;
            Format = format;
            Lsb = lsb;
            ByteStart = bytestart;
            ByteSize = bytesize;
            DefValue = defvalue;
            Comment = comment;

            Value = "0";
            Symbol = "";
            Hex = "0x0";

        }

        public void Initialize(Packet packet)
        {
            Parent = packet;
            ParseSymbolDict();
            ParseBitList();
        }

        private void ParseSymbolDict()
        {
            if (Comment.StartsWith("Symbol"))
            {
                //string input = "Symbol,0:Off,1:InitSw,2:InitSensors,3:InitNav,4:Nav,7:CoarseAlign,8:StaticAlign,10:Stanby,11:StHdngAlign";

                string[] pairs = Comment.Replace("Symbol,", "").Split(',');

                foreach (string pair in pairs)
                {
                    string[] keyValue = pair.Split(':');
                    if (keyValue.Length == 2 && UInt64.TryParse(keyValue[0], out UInt64 key))
                    {
                        _symboldict[key] = keyValue[1];
                    }
                }

                foreach (var item in _symboldict)
                {
                    Console.WriteLine($"Name:{Name}, Key: {item.Key}, Value: {item.Value}");
                }
            }
        }
        private void ParseBitList()
        {
            if (Comment.StartsWith("Bit"))
            {
                _bitlist = Comment.Replace("Bit,", "").Split(',');
            }
        }

        public abstract void SetByteData(IPacketConvert pConvertor, byte[] ByteData,string svalue);
       
        public abstract string GetValue(IPacketConvert pConvertor, byte[] ByteData);

        public double GetDValue()
        {
            return dvalue;
        }

        public override string ToString()
        {
            return $"{Name}:{Value}({Symbol})";
        }
    }
}
