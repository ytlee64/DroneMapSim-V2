using System;
using System.IO;
using System.Runtime.InteropServices;
using IWshRuntimeLibrary;
using ClosedXML.Excel;

namespace ExcelToCode
{
    public static class ExcelUtil
    {
        public static XLWorkbook? OpenWorkbookSafely(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
                return null;

            try
            {
                return new XLWorkbook(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error opening workbook: " + ex.Message);
                return null;
            }
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
                    Console.WriteLine("File not found: " + filePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Can't open " + filePath + ": " + ex.Message);
            }
            return text;
        }

        public static void MyPrint(TextStream output, string outputString)
        {
            outputString = outputString.Replace("\u202C", ""); // U+202C 문자를 제거
            output.WriteLine(outputString);
        }

        public static void ConvertUTF16LEtoUTF8(string filePath)
        {
            string sourceFilePath = filePath;
            string fileContent = "";

            try
            {
                fileContent = System.IO.File.ReadAllText(sourceFilePath, System.Text.Encoding.Unicode);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading file: " + ex.Message);
                return;
            }

            try
            {
                System.IO.File.WriteAllText(sourceFilePath, fileContent, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing file: " + ex.Message);
                return;
            }
        }
 

        public static void CloseExcelWorkbook(XLWorkbook? wb)
        {
            if (wb == null) return;
            wb.Dispose();
        }
    }
}