using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PacketUtil
{
    partial class BinToCsvVM : ObservableObject
	{

        private static BinToCsvVM? _instance = null;
        public static BinToCsvVM GetInstance()
        {
            if (_instance == null)
            {
                _instance = new BinToCsvVM();
            }
            return _instance;
        }
        public BinToCsvVM()
        {
        }
 
		public void CsvConvertStart(string path)
		{
            Task.Factory.StartNew(() => CsvConvert(path));
		}

        public void CsvConvert(string path)
        {
            if (Packets != null)
            {
                foreach (Packet packet in Packets)
                {
                    Convert(path, packet);
                }
            }
        }

        public bool Convert(string path, PacketUtil.Packet packet)
        {
            Console.WriteLine($"{path} {packet.Name}");

            string filePath = $"{path}//{packet.Name}.bin";
            string csvfilePath = $"{path}//{packet.Name}.csv";
            byte[] fileData;

            packet.PacketStatus = "o";
            FileStream fs;
            // Open the file with FileStream
            try
            {
                fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            }
            catch (Exception)
            {
                Console.WriteLine($"None of {filePath}");
                packet.PacketStatus = "Fail";
                return false;
            }

            fileData = new byte[packet.PacketLength];
            packet.PacketLogger = new(Packet.START_LOGGING_CSV, csvfilePath);
            packet.PacketLogger.WriteLine(packet.CsvHeader());
            int bytesRead;
            long unitCount = fs.Length / packet.PacketLength / 50+1;
            int count = 0;
            while ((bytesRead = fs.Read(fileData, 0, packet.PacketLength)) > 0)
            {
                packet.SetByteData(fileData);
                packet.Log();
                count++;
                if (count % unitCount == 0)
                    packet.PacketStatus += "o";
            }
            packet.PacketStatus += "Done";
            return true;
        }
    }
}
