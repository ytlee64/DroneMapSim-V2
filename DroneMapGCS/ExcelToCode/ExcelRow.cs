using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DocumentFormat.OpenXml.Office2019.Presentation;

namespace ExcelToCode
{
    public class ExcelRow
    {
        public static Dictionary<string, int> SizeDict { get; } = new Dictionary<string, int>()
            {
                {"char", 1},{"int8_t", 1},{"int8", 1},
                {"uchar", 1},{"byte", 1},{"uint8_t", 1},{"uint8", 1},
                {"short", 2},{"int16_t", 2},{"int16", 2},
                {"ushort", 2},{"uint16_t", 2},{"uint16", 2},
                {"int", 4},{"int32_t", 4},{"int32", 4},
                {"uint", 4},{"uint32_t", 4},{"uint32", 4},
                {"long", 8},{"int64_t", 8},{"int64", 8},
                {"ulong", 8},{"uint64_t", 8},{"uint64", 8},
                {"float", 4},{"float32", 4}, {"single", 4},
                {"double", 8},{"float64", 8},
                {"string", 1}
            };

        public static Dictionary<string, bool> SignedDict { get; } = new Dictionary<string, bool>()
            {
                {"char", true},{"int8_t", true},{"int8", true},
                {"uchar", false},{"byte", false},{"uint8_t", false},{"uint8", false},
                {"short", true},{"int16_t", true},{"int16", true},
                {"ushort", false},{"uint16_t", false},{"uint16", false},
                {"int", true},{"int32_t", true},{"int32", true},
                {"uint", false},{"uint32_t", false},{"uint32", false},
                {"long", true}, {"int64_t", true},{"int64", true},
                {"ulong", false},{"uint64_t", false},{"uint64", false},
                {"float", true},{"float32", true}, {"single", true},
                {"double", true},{"float64", true},
                {"string", false}
            };

        public string Name { get; set; } = "";
        public string KorName { get; set; } = "";
        public int ByteStart { get; set; } = 0;
        public string Type { get; set; } = "";
        public string FullScale { get; set; } = "";
        public double Lsb { get; set; } = 0;
        public string DefaultValue { get; set; } = "";
        public string Unit { get; set; } = "";
        public string Comment { get; set; } = "";

        public int Size { get; set; } = 0;
        public bool Signed { get; set; } = false;
        public string Format { get; set; } = "";

        public int Count=1;

        private double GetLsb(string lsb)
        {
            if (lsb == "" || lsb == "_" || lsb == "-")
                return 1;
            try
            {
                double dLsb = Convert.ToDouble(lsb.Trim());
                return dLsb == 0 ? 1 : dLsb;
            }
            catch
            {
                ErrorHelper.ShowFatalAndExit("ParseError", $"GetLsb {Name} {lsb} Exception");
            }

            return 1;
        }

        private static int GetDecimalPlaces(double number)
        {
            if (number == 0)
                return 0;

            int decimalPlaces = 0;
            double scaledNumber = number;
            while (scaledNumber < 1)
            {
                decimalPlaces++;
                scaledNumber *= 10;
            }

            return decimalPlaces;
        }


        public ExcelRow(string name, string korName, int byteStart, int size, string type, string fullScale, string lsb, string defaultValue, string unit, string comment)
        {
            Name = name;
            //Console.WriteLine($"Item Name {Name}");
            KorName = korName;
            ByteStart = byteStart;
            Type = type.ToLower();
            FullScale = fullScale;
            Lsb = GetLsb(lsb);

            Unit = unit;
            Comment = StringHelpers.Sanitize(comment);

            DefaultValue = StringHelpers.NormalizeNumber(defaultValue);
            
            
            Signed = SignedDict[Type];

            StringHelpers.TryParseArrayNotation(name, out var baseName, out var count);

            Count = count;
            Size = SizeDict[Type];

            if(Type == "string" )
            {
                Name = baseName;
                Count = 1;
                Size = count;
            }else if(count>1)
            {
                Name = baseName;
            }

            //if (size != Size)
            //{
            //    ErrorHelper.ShowFatalAndExit("PaserError", $"ExcelRow Invalid size for field '{Name}': declared={size}, expected={Size} for type '{Type}'.");
            //}

            Format = GetFormat();
        }

        private string GetFormat()
        {
            if (Type.Contains("string"))
            {
                return "S";
            }
            else if (Type.Contains("float") || Type.Contains("double"))
            {
                return "F8";
            }
            else if (Lsb != 1)
            {
                return $"F{GetDecimalPlaces(Lsb) * 2}";
            }
            else if (//Name.Contains("id", StringComparison.OrdinalIgnoreCase) ||
                     Comment.Contains("0x") ||
                     Comment.Contains("Bit") ||
                     Comment.Contains("bit") ||
                     Comment.Contains("b0") ||
                     Comment.Contains("b1"))
            {
                return $"X{Size * 2}";
            }
            else
            {
                return "D";
            }
        }
    }
}
