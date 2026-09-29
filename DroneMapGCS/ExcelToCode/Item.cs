namespace ExcelToCode
{

    internal class Item
    {
        public static Dictionary<string, int> SizeDict = new Dictionary<string, int>()
        {
            {"char", 1},{"int8", 1} ,{"int8_t", 1},
            {"uchar", 1},{"byte", 1},{"uint8", 1} ,{"uint8_t", 1},
            {"short", 2},{"int16", 2} ,{"int16_t", 2} ,
            {"ushort", 2},{"uint16", 2},{"uint16_t", 2},
            {"int", 4},{"int32", 4},{"int32_t", 4},
            {"uint", 4},{"uint32", 4},{"uint32_t", 4},
            {"long", 8},{"int64", 8},{"int64_t", 8},
            {"ulong", 8},{"uint64", 8},{"uint64_t", 8},
            {"float", 4},{"float32", 4}, {"single", 4},
            {"double", 8},{"float64", 8}
        };

        public static Dictionary<string, bool> SignedDict = new Dictionary<string, bool>()
        {
            {"char", true},{"int8", true},{"int8_t", true},
            {"uchar", false},{"byte", false},{"uint8", false},{"uint8_t", false},
            {"short", true},{"int16", true},{"int16_t", true},
            {"ushort", false},{"uint16", false},{"uint16_t", false},
            {"int", true},{"int32", true},{"int32_t", true},
            {"uint", false},{"uint32", false},{"uint32_t", false},
            {"long", true}, {"int64", true},{"int64_t", true},
            {"ulong", false},{"uint64", false},{"uint64_t", false},
            {"float", true},{"float32", true}, {"single", true},
            {"double", true},{"float64", true}
        };

        public string Name = "";
        public string KorName = "";
        public int ByteStart = 0;
        public string Type = "";
        public string FullScale = "";
        public double Lsb = 0;
        public string Default = "";
        public string Unit = "";
        public string Comment = "";


        public int Size = 0;
        public bool Signed = false;
        public string Format = "";

        double GetLsb(string Lsb)
        {
            Lsb = Lsb.Trim();
            if (Lsb == null || Lsb == "_" || Lsb == "-" || Lsb == "")
                return 1;

            double dLsb = Convert.ToDouble(Lsb);
            if (dLsb == 0)
                return 1;
            return dLsb;
        }
        static int GetDecimalPlaces(double number)
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

        public Item(string Name, string KorName, int ByteStart, int Size, string Type, string FullScale, string Lsb, string Default, string Unit, string Comment)
        {
            this.Name = char.ToUpper(Name[0]) + Name.Substring(1);
            this.KorName = KorName;
            this.ByteStart = ByteStart;
            this.Type = Type;
            this.FullScale = FullScale;
            this.Lsb = GetLsb(Lsb);
            this.Default = Default.Replace("\"", "'").Replace("‬", "").Replace("\n", "|").Replace("\r", " ");
            this.Unit = Unit;
            this.Comment = Comment.Replace("\"", "'").Replace("‬", "").Replace("\n", "|").Replace("\r", " ");


            //if(Size!= this.Size)
            //{
            //    Console.WriteLine($"Size Error {Name} Excel :{Size}. Dict {this.Size}");
            //    Environment.Exit(-1);
            //}


            if (this.Type.Contains("string"))
            {
                Format = "S";
                this.Signed = false;
                this.Size = Size;
            }
            else
            {
                this.Signed = SignedDict[this.Type];
                this.Size = SizeDict[this.Type];

                if (this.Type.Contains("float") || this.Type.Contains("double"))
                    Format = "F6";
                else if (this.Lsb != 1)
                    Format = "F" + GetDecimalPlaces(this.Lsb).ToString();
                else if (
                    Comment.Contains("0x") || Comment.Contains("Bit") ||
                    Comment.Contains("Text") || Comment.Contains("b0") || Comment.Contains("b1"))
                    Format = "X" + (this.Size * 2).ToString();
                else
                    Format = "D";
            }
            
        }
    }
}
