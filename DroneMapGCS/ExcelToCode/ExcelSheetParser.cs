using System;
using System.Collections.Generic;

using ClosedXML.Excel;

namespace ExcelToCode
{
    public class ExcelSheetParser
    {
        private readonly List<ExcelRow> items = new List<ExcelRow>();

        public string Name { get; private set; } = "";
        public string Endian { get; private set; } = "LittleEndian";
        public int TotalLength { get; private set; } = 0;

        public ExcelSheetParser(IXLWorksheet ws)
        {
            Name = ws.Name;
            string excelEndian = ExcelFile.GetCellAsString(ws, 1, 11);
            if (excelEndian.ToLower().Contains("big"))
                Endian = "BigEndian";
            string excelListType = ExcelFile.GetCellAsString(ws, 1, 12);

            string name = "init";
            int row = 2;
            for (; ; row++)
            {
                name = ExcelFile.GetCellAsString(ws, row, 1);
                if (string.IsNullOrEmpty(name))
                    break;
                try
                {
                    string korName = ExcelFile.GetCellAsString(ws, row, 2);
                    int byteStart = ExcelFile.GetCellAsInt(ws, row, 3);
                    int size = ExcelFile.GetCellAsInt(ws, row, 4);
                    string type = ExcelFile.GetCellAsString(ws, row, 5).ToLower();
                    string fullScale = ExcelFile.GetCellAsString(ws, row, 6);
                    string lsb = ExcelFile.GetCellAsString(ws, row, 7);
                    string defaultValue = ExcelFile.GetCellAsString(ws, row, 8);
                    string unit = ExcelFile.GetCellAsString(ws, row, 9);
                    string comment = ExcelFile.GetCellAsString(ws, row, 10);

                    ExcelRow item = new ExcelRow(name, korName, byteStart, size, type, fullScale, lsb, defaultValue, unit, comment);

                    if (item.Count == 1)
                    {
                        items.Add(item);
                        Console.WriteLine($"Item {item.Name} ByteStart {item.ByteStart}");
                    }
                    else
                    {
                        int bytestart = item.ByteStart;
                        int unitsize = item.Size;

                        for (int i = 0; i < item.Count; i++)
                        {
                            string arrayItemName = $"{item.Name}_{i}";
                            ExcelRow arrayitem = new ExcelRow(arrayItemName, korName, bytestart, unitsize, type, fullScale, lsb, defaultValue, unit, comment);
                            
                            Console.WriteLine($"Item {arrayitem.Name} ByteStart {arrayitem.ByteStart}");
                            bytestart += unitsize;

                            items.Add(arrayitem);
                        }
                    }
                }
                catch (Exception)
                {
                    ErrorHelper.ShowFatalAndExit("PaserError", $"Invalid {Name} Item {name}");
                }
            }
            try
            {
                TotalLength = ExcelFile.GetCellAsInt(ws, row, 3);
            }
            catch (Exception)
            {
                ErrorHelper.ShowFatalAndExit("PaserError", "Total Length is not defined");
            }
        }

        public List<ExcelRow> GetItems()
        {
            return items;
        }
    }


}