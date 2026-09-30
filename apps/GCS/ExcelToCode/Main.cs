using ClosedXML.Excel;
using Scripting = IWshRuntimeLibrary;

namespace ExcelToCode
{
    class Program
    {
        static List<string> packetNames = new ();
        static string namespaceName = "DroneMapGCS";
        static string destPath = "../DroneMapGCS/packets/";
        static string vmname = "";
        static string workPath= "PacketData";
         
        static void CopyAll()
        {
            foreach (var packetname in packetNames)
            {
                foreach (var csfile in Directory.GetFiles($"./csharp/", $"{packetname}Pkt*.cs"))
                {
                    var filename = Path.GetFileName(csfile);
                    File.Copy(csfile, $"{destPath}{filename}", true);
                    Console.WriteLine($"{destPath}{filename}");
                }
            }

            //File.Copy($"./csharp/{vmfilename}", $"{destPath}{vmfilename}", true);
        }

        static void Run()
        {
            Directory.SetCurrentDirectory(workPath);

            string currentPath = Directory.GetCurrentDirectory();

            Scripting.FileSystemObject FileSystem = new Scripting.FileSystemObject();
            Scripting.Folder Folder = FileSystem.GetFolder(currentPath);

            foreach (Scripting.File File in Folder.Files)
            {
                if (File.Name.Contains("xlsx") && !File.Name.Contains('~'))
                {
                    string targetFilename = Path.Combine(currentPath, File.Name);
                    Console.WriteLine(targetFilename);
                    XLWorkbook? wb = ExcelUtil.OpenWorkbookSafely(targetFilename);
                    if (wb != null)
                    {
                        foreach (IXLWorksheet ws in wb.Worksheets)
                        {
                            if (ws.Name.Contains("Reserved"))
                                continue;
                            Console.WriteLine(ws.Name, "-------------------------------");
                            ExcelSheetParser packet = new (ws);

                            MakeCCode.ExportToHeaderFile(packet);
                            MakeCCode.ExportToInitializeFile(packet);
                            MakeCsCode.MakeCsAll(namespaceName, packet);
                            packetNames.Add(packet.Name);
                        }
                        ExcelUtil.CloseExcelWorkbook(wb);
                    }

                }
            }

            MakeVm.MakeCs(namespaceName, vmname, packetNames);

            //CopyAll();
        }
        static void Main(string[] args)
        {
 
            Run();
            CopyAll();
        }
}
}
