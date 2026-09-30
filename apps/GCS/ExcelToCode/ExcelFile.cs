using System;

using ClosedXML.Excel;

using IWshRuntimeLibrary;

namespace ExcelToCode
{
    public static class ExcelFile
    {
        public static IXLWorkbook? OpenWorkbookSafely(string filePath)
        {
            IXLWorkbook? wb = null;
            try
            {
                if (System.IO.File.Exists(filePath))
                    wb = new XLWorkbook(filePath);
                else
                {
                    Console.WriteLine("OpenWorkbookSafely File not found: " + filePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("OpenWorkbookSafely Can't open " + filePath + ": " + ex.Message);
            }
            return wb;
        }

        public static string GetFileText(string filePath)
        {
            string text = "";
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    text = System.IO.File.ReadAllText(filePath);
                }
                else
                {
                    Console.WriteLine("GetFileText File not found: " + filePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetFileText Can't open " + filePath + ": " + ex.Message);
            }
            return text;
        }


        public static void MyPrint(TextStream output, string outputString)
        {
            //outputString = outputString.Replace("\n", "\\\\"); // \n 문자를 \\로 변환
            outputString = outputString.Replace("\u202C", ""); // U+202C 문자를 제거
            output.WriteLine(outputString);
        }

        public static void ConvertUTF16LEtoUTF8(string filePath)
        {
            string sourceFilePath = filePath;
            string fileContent = "";

            // UTF-16 LE 파일 읽기
            try
            {
                fileContent = System.IO.File.ReadAllText(sourceFilePath, System.Text.Encoding.Unicode);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ConvertUTF16LEtoUTF8 Error reading file: " + ex.Message);
                return;
            }

            // UTF-8로 변환하여 파일 저장
            try
            {
                System.IO.File.WriteAllText(sourceFilePath, fileContent, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ConvertUTF16LEtoUTF8 Error writing file: " + ex.Message);
                return;
            }
        }

        public static string GetCellAsString(IXLWorksheet ws, int row, int column)
        {
            var cell = ws.Cell(row, column);
            return cell?.GetValue<string>().Trim() ?? "";
        }

        public static int GetCellAsInt(IXLWorksheet ws, int row, int column)
        {
            var cell = ws.Cell(row, column);
            return cell?.GetValue<int>() ?? 0;
        }

        public static double GetCellAsDouble(IXLWorksheet ws, int row, int column)
        {
            var cell = ws.Cell(row, column);
            return cell?.GetValue<double>() ?? 0.0;
        }
    }
}