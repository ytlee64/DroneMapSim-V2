using System.Data;

using IWshRuntimeLibrary;
using Scripting = IWshRuntimeLibrary;

namespace ExcelToCode
{
    public static class MakeCCode
    {
        private const string HeaderFilePath = "Header.txt";
        private const string FooterFilePath = "Footer.txt";
        private const string OutputDirectory = "./cpp/";

        public static void Header(TextStream output)
        {
            ExcelUtil.MyPrint(output, ExcelUtil.GetFileText(HeaderFilePath));
        }

        public static void Footer(TextStream output)
        {
            ExcelUtil.MyPrint(output, ExcelUtil.GetFileText(FooterFilePath));
        }

        public static void ExportToHeaderFile(ExcelSheetParser packet)
        {
            string dirPath = "./cpp";
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            string outputPath = $"{OutputDirectory}{packet.Name}.h";

            try
            {
                Scripting.FileSystemObject fso = new Scripting.FileSystemObject();
                TextStream outStream = fso.CreateTextFile(outputPath, true, true);

                Header(outStream);
                ExcelUtil.MyPrint(outStream, "\tstruct " + packet.Name + " // " + packet.Endian);
                ExcelUtil.MyPrint(outStream, "\t{");
                List<ExcelRow> items = packet.GetItems();
                foreach (ExcelRow item in items)
                {
                    string line;
                    
                    if (item.Type == "string")
                    {
                        line = $"\t\tchar {item.Name}[{item.Size*item.Count}]; // {item.Lsb} # {item.KorName} # {item.Comment}";
                    }
                    else if(item.Count == 1)
                    {
                        line = $"\t\t{item.Type} {item.Name}; // {item.Lsb} # {item.KorName} # {item.Comment}";
                    }
                    else
                    {
                        line = $"\t\t{item.Type} {item.Name}[{item.Size * item.Count}]; // {item.Lsb} # {item.KorName} # {item.Comment}";
                    }

                    ExcelUtil.MyPrint(outStream, line);
                }
                ExcelUtil.MyPrint(outStream, "\t};");
                Footer(outStream);
                outStream.Close();

                ExcelUtil.ConvertUTF16LEtoUTF8(outputPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting header file: {ex.Message}");
            }
        }

        public static void ExportToInitializeFile(ExcelSheetParser packet)
        {
            string outputPath = $"{OutputDirectory}{packet.Name}.cpp";

            try
            {
                Scripting.FileSystemObject fso = new Scripting.FileSystemObject();
                TextStream outStream = fso.CreateTextFile(outputPath, true, true);

                List<ExcelRow> items = packet.GetItems();
                foreach (ExcelRow item in items)
                {
                    string line = $"pkt{packet.Name}.{item.Name} = 0;";
                    ExcelUtil.MyPrint(outStream, line);
                }
                outStream.Close();

                ExcelUtil.ConvertUTF16LEtoUTF8(outputPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting initialize file: {ex.Message}");
            }
        }
    }
}