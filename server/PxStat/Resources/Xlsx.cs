﻿using API;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using PxStat.Data;
using PxStat.System.Settings;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using static PxStat.Data.Matrix;

namespace PxStat.Resources
{
    internal class XlsxValue
    {
        internal string Value { get; set; }
        internal EnumValue<CellValues> DataType { get; set; }
        internal int StyleId { get; set; }

        internal XlsxValue()
        {
            DataType = CellValues.String;
        }

    }
    internal class Xlsx
    {

        /// <summary>
        /// Get a CSV representation of the matrix table. 
        /// The quote parameter is optional, if it isn't sent then it will not be implemented
        /// </summary>
        /// <param name="theMatrix"></param>
        /// <param name="rowLists"></param>
        /// <param name="quote"></param>
        /// <returns></returns>
        internal string GetCsv(List<List<XlsxValue>> rowLists, string quote = "")
        {

            string lngIsoCode = Utility.GetUserAcceptLanguage();

            //This will be language dependent
            string sep = Label.Get("default.csv.separator");

            if (String.IsNullOrEmpty(lngIsoCode))
                lngIsoCode = Utility.GetCustomConfig("APP_DEFAULT_LANGUAGE");

            StringBuilder sb = new StringBuilder();
            foreach (var row in rowLists)
            {
                int counter = 1;
                string line = "";
                foreach (XlsxValue column in row)
                {

                    //check if we need to reformat this by language
                    string word = column.Value;

                    //Doubles will vary by language. However, this should only be applied to the last column, i.e. the VALUE
                    if (Double.TryParse(word, out double result) && counter == row.Count)
                    {

                        word = result.ToString(CultureInfo.CreateSpecificCulture(lngIsoCode));
                    }


                    line = line + quote + word + quote + (counter < row.Count ? sep : "");
                    counter++;

                }
                sb.AppendLine(line);
            }

            return sb.ToString();
        }

        internal string GetXlsx(Matrix theMatrix, List<List<XlsxValue>> rowLists, string lngIsoCode = null)
        {

            //A null lngIsoCode - set lngIsoCode to default language
            lngIsoCode = lngIsoCode ?? Utility.GetCustomConfig("APP_DEFAULT_LANGUAGE");

            //language does not exist in the system - set lngIsoCode to default language 
            lngIsoCode = new Language_BSO().Read(lngIsoCode).LngIsoCode == null ? Utility.GetCustomConfig("APP_DEFAULT_LANGUAGE") : lngIsoCode;

            Specification spec = theMatrix.GetSpecFromLanguage(lngIsoCode);

            spec = spec ?? theMatrix.MainSpec;

            MemoryStream documentStream = new MemoryStream();
            // Create a spreadsheet document by supplying the filepath.
            // By default, AutoSave = true, Editable = true, and Type = xlsx.
            SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Create(documentStream, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook);

            // Add a WorkbookPart to the document.
            WorkbookPart workbookpart = spreadsheetDocument.AddWorkbookPart();
            workbookpart.Workbook = new Workbook();

            // Add a WorksheetPart to the WorkbookPart.
            WorksheetPart worksheetPart = workbookpart.AddNewPart<WorksheetPart>();

            worksheetPart.Worksheet = CreateColumnWidth(new Worksheet(new SheetData()), 1, 1, 35, 100);
            worksheetPart.Worksheet.Save();



            // Add Sheets to the Workbook.
            Sheets sheets = spreadsheetDocument.WorkbookPart.Workbook.AppendChild<Sheets>(new Sheets());

            // Append a new worksheet and associate it with the workbook.
            Sheet sheet = new Sheet() { Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "About" };
            sheets.Append(sheet);



            if (spreadsheetDocument.WorkbookPart.WorkbookStylesPart == null)
            {
                WorkbookStylesPart stylesPart = spreadsheetDocument.WorkbookPart.AddNewPart<WorkbookStylesPart>();

                stylesPart.Stylesheet = GenerateStyleSheet();


            }

            //prepare the About sheet
            for (int i = 1; i <= 40; i++)
            {
                worksheetPart = InsertAboutDataRow(worksheetPart, i, null, null, 20);
            }
            XlsxValue table = new XlsxValue() { DataType = CellValues.String, Value = Label.Get("xlsx.table", lngIsoCode), StyleId = 8 };
            XlsxValue tableValue = new XlsxValue() { DataType = CellValues.String, Value = "", StyleId = 8 };
            InsertAboutDataRow(worksheetPart, 0, table, tableValue, null, 8);

            XlsxValue mtrCode = new XlsxValue() { DataType = CellValues.String, Value = Label.Get("xlsx.code", lngIsoCode), StyleId = 9 };
            XlsxValue mtrCodeVal = new XlsxValue() { DataType = CellValues.String, Value = theMatrix.Code, StyleId = 7 };
            InsertAboutDataRow(worksheetPart, 0, mtrCode, mtrCodeVal, null, 9);

            XlsxValue mtrName = new XlsxValue() { DataType = CellValues.String, Value = Label.Get("xlsx.name", lngIsoCode), StyleId = 9 };
            XlsxValue mtrNameVal = new XlsxValue() { DataType = CellValues.String, Value = spec.Contents, StyleId = 7 };
            InsertAboutDataRow(worksheetPart, 0, mtrName, mtrNameVal, null, 10);

            string lngCulture = Utility.GetUserAcceptLanguage();

            DateTime dtLast;
            string dateString = "";
            if (theMatrix.CreationDate.Length > 15)
            {
                dtLast = new DateTime(Convert.ToInt32(theMatrix.CreationDate.Substring(0, 4)), Convert.ToInt32(theMatrix.CreationDate.Substring(4, 2)), Convert.ToInt32(theMatrix.CreationDate.Substring(6, 2)), Convert.ToInt32(theMatrix.CreationDate.Substring(9, 2)), Convert.ToInt32(theMatrix.CreationDate.Substring(12, 2)), 0);
                dateString = dtLast.ToString(CultureInfo.InvariantCulture);
            }
            else
                dateString = theMatrix.CreationDate;

            XlsxValue lastUpdated = new XlsxValue() { DataType = CellValues.String, Value = Label.Get("xlsx.last-updated", lngIsoCode), StyleId = 9 };
            XlsxValue lastUpdatedVal = new XlsxValue() { DataType = CellValues.String, Value = dateString, StyleId = 7 };
            InsertAboutDataRow(worksheetPart, 0, lastUpdated, lastUpdatedVal, null, 11);

            XlsxValue note = new XlsxValue() { DataType = CellValues.String, Value = Label.Get("xlsx.note", lngIsoCode), StyleId = 9 };
            XlsxValue noteVal = new XlsxValue() { DataType = CellValues.String, Value = spec.Notes != null ? String.Join(" ", theMatrix.MainSpec.Notes) : "", StyleId = 7 };
            InsertAboutDataRow(worksheetPart, 0, note, noteVal, null, 12);

            XlsxValue url = new XlsxValue() { DataType = CellValues.String, Value = Label.Get("xlsx.url", lngIsoCode), StyleId = 9 };
            XlsxValue urlVal = new XlsxValue() { DataType = CellValues.String, Value = ConfigurationManager.AppSettings["APP_URL"] + "/" + Utility.GetCustomConfig("APP_COOKIELINK_RELEASE") + '/' + theMatrix.Release.RlsCode.ToString(), StyleId = 7 };
            InsertAboutDataRow(worksheetPart, 0, url, urlVal, null, 13);

            XlsxValue blank = new XlsxValue() { DataType = CellValues.String, Value = "", StyleId = 9 };
            XlsxValue blankVal = new XlsxValue() { DataType = CellValues.String, Value = "", StyleId = 7 };
            InsertAboutDataRow(worksheetPart, 0, blank, blankVal, null, 14);

            XlsxValue product = new XlsxValue() { DataType = CellValues.String, Value = Label.Get("xlsx.product", lngIsoCode), StyleId = 8 };
            XlsxValue productValue = new XlsxValue() { DataType = CellValues.String, Value = "", StyleId = 8 };
            InsertAboutDataRow(worksheetPart, 0, product, productValue, null, 15);

            XlsxValue pCode = new XlsxValue() { DataType = CellValues.String, Value = Label.Get("xlsx.code", lngIsoCode), StyleId = 9 };
            XlsxValue pCodeVal = new XlsxValue() { DataType = CellValues.String, Value = theMatrix.Release.PrcCode, StyleId = 7 };
            InsertAboutDataRow(worksheetPart, 0, pCode, pCodeVal, null, 16);

            XlsxValue pName = new XlsxValue() { DataType = CellValues.String, Value = Label.Get("xlsx.name", lngIsoCode), StyleId = 9 };
            XlsxValue pNameVal = new XlsxValue() { DataType = CellValues.String, Value = theMatrix.Release.PrcValue, StyleId = 7 };
            InsertAboutDataRow(worksheetPart, 0, pName, pNameVal, null, 17);

            InsertAboutDataRow(worksheetPart, 0, blank, blankVal, null, 18);

            XlsxValue contacts = new XlsxValue() { DataType = CellValues.String, Value = Label.Get("xlsx.contacts", lngIsoCode), StyleId = 8 };
            XlsxValue contactsValue = new XlsxValue() { DataType = CellValues.String, Value = "", StyleId = 8 };
            InsertAboutDataRow(worksheetPart, 0, contacts, contactsValue, null, 19);

            XlsxValue cName = new XlsxValue() { DataType = CellValues.String, Value = Label.Get("xlsx.name", lngIsoCode), StyleId = 9 };
            XlsxValue cNameVal = new XlsxValue() { DataType = CellValues.String, Value = theMatrix.Release.GrpContactName, StyleId = 7 };
            InsertAboutDataRow(worksheetPart, 0, cName, cNameVal, null, 20);

            XlsxValue cEmail = new XlsxValue() { DataType = CellValues.String, Value = Label.Get("xlsx.email", lngIsoCode), StyleId = 9 };
            XlsxValue cEmailVal = new XlsxValue() { DataType = CellValues.String, Value = theMatrix.Release.GrpContactEmail, StyleId = 7 };
            InsertAboutDataRow(worksheetPart, 0, cEmail, cEmailVal, null, 21);

            XlsxValue cPhone = new XlsxValue() { DataType = CellValues.String, Value = Label.Get("xlsx.phone", lngIsoCode), StyleId = 9 };
            XlsxValue cPhoneVal = new XlsxValue() { DataType = CellValues.String, Value = theMatrix.Release.GrpContactPhone, StyleId = 7 };
            InsertAboutDataRow(worksheetPart, 0, cPhone, cPhoneVal, null, 22);

            InsertAboutDataRow(worksheetPart, 0, blank, blankVal, null, 23);

            XlsxValue copyright = new XlsxValue() { DataType = CellValues.String, Value = Label.Get("xlsx.copyright", lngIsoCode), StyleId = 8 };
            XlsxValue copyrightValue = new XlsxValue() { DataType = CellValues.String, Value = "", StyleId = 8 };
            InsertAboutDataRow(worksheetPart, 0, copyright, copyrightValue, null, 24);

            XlsxValue cpCode = new XlsxValue() { DataType = CellValues.String, Value = Label.Get("xlsx.code", lngIsoCode), StyleId = 9 };
            XlsxValue cpCodeVal = new XlsxValue() { DataType = CellValues.String, Value = theMatrix.Copyright.CprCode, StyleId = 7 };
            InsertAboutDataRow(worksheetPart, 0, cpCode, cpCodeVal, null, 25);

            XlsxValue cpName = new XlsxValue() { DataType = CellValues.String, Value = Label.Get("xlsx.name", lngIsoCode), StyleId = 9 };
            XlsxValue cpNameVal = new XlsxValue() { DataType = CellValues.String, Value = theMatrix.Copyright.CprValue, StyleId = 7 };
            InsertAboutDataRow(worksheetPart, 0, cpName, cpNameVal, null, 26);

            XlsxValue cpUrl = new XlsxValue() { DataType = CellValues.String, Value = Label.Get("xlsx.url", lngIsoCode), StyleId = 9 };
            XlsxValue cpUrlVal = new XlsxValue() { DataType = CellValues.String, Value = theMatrix.Copyright.CprUrl, StyleId = 7 };
            InsertAboutDataRow(worksheetPart, 0, cpUrl, cpUrlVal, null, 27);

            InsertAboutDataRow(worksheetPart, 0, blank, blankVal, null, 28);

            XlsxValue properties = new XlsxValue() { DataType = CellValues.String, Value = Label.Get("xlsx.properties", lngIsoCode), StyleId = 8 };
            XlsxValue propertiesValue = new XlsxValue() { DataType = CellValues.String, Value = "", StyleId = 8 };
            InsertAboutDataRow(worksheetPart, 0, properties, propertiesValue, null, 29);

            XlsxValue official = new XlsxValue() { DataType = CellValues.String, Value = Label.Get("xlsx.official-statistics", lngIsoCode), StyleId = 9 };
            XlsxValue officialVal = new XlsxValue() { DataType = CellValues.String, Value = theMatrix.IsOfficialStatistic ? Label.Get("xlsx.yes", lngIsoCode) : Label.Get("xlsx.no", lngIsoCode), StyleId = 7 };
            InsertAboutDataRow(worksheetPart, 0, official, officialVal, null, 30);

            XlsxValue emergency = new XlsxValue() { DataType = CellValues.String, Value = Label.Get("xlsx.emergency", lngIsoCode), StyleId = 9 };
            XlsxValue emergencyVal = new XlsxValue() { DataType = CellValues.String, Value = theMatrix.Release.RlsEmergencyFlag ? Label.Get("xlsx.yes", lngIsoCode) : Label.Get("xlsx.no", lngIsoCode), StyleId = 7 };
            InsertAboutDataRow(worksheetPart, 0, emergency, emergencyVal, null, 31);

            XlsxValue archived = new XlsxValue() { DataType = CellValues.String, Value = Label.Get("xlsx.archived", lngIsoCode), StyleId = 9 };
            XlsxValue archivedVal = new XlsxValue() { DataType = CellValues.String, Value = theMatrix.Release.RlsArchiveFlag ? Label.Get("xlsx.yes", lngIsoCode) : Label.Get("xlsx.no", lngIsoCode), StyleId = 7 };
            InsertAboutDataRow(worksheetPart, 0, archived, archivedVal, null, 32);

            XlsxValue analytical = new XlsxValue() { DataType = CellValues.String, Value = Label.Get("xlsx.analytical", lngIsoCode), StyleId = 9 };
            XlsxValue analyticalVal = new XlsxValue() { DataType = CellValues.String, Value = theMatrix.Release.RlsAnalyticalFlag ? Label.Get("xlsx.yes", lngIsoCode) : Label.Get("xlsx.no", lngIsoCode), StyleId = 7 };
            InsertAboutDataRow(worksheetPart, 0, analytical, analyticalVal, null, 33);

            XlsxValue dependency = new XlsxValue() { DataType = CellValues.String, Value = Label.Get("xlsx.dependency", lngIsoCode), StyleId = 9 };
            XlsxValue dependencyVal = new XlsxValue() { DataType = CellValues.String, Value = theMatrix.Release.RlsDependencyFlag ? Label.Get("xlsx.yes", lngIsoCode) : Label.Get("xlsx.no", lngIsoCode), StyleId = 7 };
            InsertAboutDataRow(worksheetPart, 0, dependency, dependencyVal, null, 34);

            InsertAboutDataRow(worksheetPart, 0, blank, blankVal, null, 35);

            XlsxValue language = new XlsxValue() { DataType = CellValues.String, Value = Label.Get("xlsx.language", lngIsoCode), StyleId = 8 };
            XlsxValue languageValue = new XlsxValue() { DataType = CellValues.String, Value = "", StyleId = 8 };
            InsertAboutDataRow(worksheetPart, 0, language, languageValue, null, 36);

            XlsxValue isoCode = new XlsxValue() { DataType = CellValues.String, Value = Label.Get("xlsx.iso-code", lngIsoCode), StyleId = 9 };
            XlsxValue isoCodeVal = new XlsxValue() { DataType = CellValues.String, Value = spec.Language, StyleId = 7 };
            InsertAboutDataRow(worksheetPart, 0, isoCode, isoCodeVal, null, 37);

            XlsxValue isoName = new XlsxValue() { DataType = CellValues.String, Value = Label.Get("xlsx.iso-name", lngIsoCode), StyleId = 9 };
            XlsxValue isoNameVal = new XlsxValue() { DataType = CellValues.String, Value = new Language_BSO().Read(spec.Language).LngIsoName, StyleId = 7 };
            InsertAboutDataRow(worksheetPart, 0, isoName, isoNameVal, null, 38);



            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "PxStat.Resources.image.png";


            Stream st = assembly.GetManifestResourceStream(resourceName);

            XlsxUtilities.AddImage(worksheetPart, st, "logo", 1, 1);



            WorksheetPart newWorksheetPart = spreadsheetDocument.WorkbookPart.AddNewPart<WorksheetPart>();
            newWorksheetPart.Worksheet = new Worksheet(new SheetData());

            Sheets sheets2 = spreadsheetDocument.WorkbookPart.Workbook.GetFirstChild<Sheets>();
            string relationshipId = spreadsheetDocument.WorkbookPart.GetIdOfPart(newWorksheetPart);

            // Get a unique ID for the new worksheet.
            uint sheetId = 1;
            if (sheets2.Elements<Sheet>().Count() > 0)
            {
                sheetId = sheets2.Elements<Sheet>().Select(s => s.SheetId.Value).Max() + 1;
            }


            // Append the new worksheet and associate it with the workbook.
            Sheet sheet2 = new Sheet() { Id = relationshipId, SheetId = sheetId, Name = theMatrix.Code };
            sheets2.Append(sheet2);

            int rowcounter = 1;
            foreach (List<XlsxValue> rowList in rowLists)
            {

                newWorksheetPart = InsertDataRow(newWorksheetPart, rowList, rowcounter, spreadsheetDocument, rowcounter == 1);
                rowcounter++;
            }



            // Close the document.
            spreadsheetDocument.Close();

            documentStream.Seek(0, SeekOrigin.Begin);


            //Tester - not normally invoked
            //FileStream fs = new FileStream(@"C:\nok\Schemas\" + theMatrix.Code + ".xlsx", FileMode.CreateNew);
            //documentStream.WriteTo(fs);
            //fs.Close();

            byte[] bytes;
            using (MemoryStream ms = new MemoryStream())
            {
                documentStream.CopyTo(ms);
                bytes = ms.ToArray();

            }

            return Utility.EncodeBase64FromByteArray(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }





        /// <summary>
        /// add the bespoke columns for the list spreadsheet
        /// </summary>
        internal Worksheet CreateColumnWidth(Worksheet sheetCol, uint startIndex, uint endIndex, double widthOne, double widthTwo)
        {
            Columns cols = new Columns();

            if (sheetCol.Where(x => x.LocalName == "cols").Count() > 0)
                sheetCol.RemoveChild<Columns>(cols);

            // Create the column
            Column column = new Column
            {
                Min = startIndex,
                Max = endIndex,
                Width = widthOne,
                CustomWidth = true
            };
            cols.Append(column); // Add it to the list of columns

            Column col2 = new Column
            {
                Min = startIndex + 1,
                Max = endIndex + 1,
                Width = widthTwo,
                CustomWidth = true
            };
            cols.Append(col2);

            // Make sure that the column info is inserted *before* the sheetdata
            sheetCol.InsertBefore<Columns>(cols, sheetCol.Where(x => x.LocalName == "sheetData").First());
            return sheetCol;
        }

        private Row GetRow(ref SheetData wsData, UInt32 rowIndex)
        {

            var row = wsData.Elements<Row>().ToList<Row>()[(int)rowIndex];
            if (row == null)
            {
                row = new Row();
                row.RowIndex = rowIndex;
                wsData.Append(row);
            }
            return row;
        }


        internal WorksheetPart InsertAboutDataRow(WorksheetPart worksheetPart, int rownum, XlsxValue key = null, XlsxValue value = null, int? colCount = null, int? startRow = null)
        {
            SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

            // Add a row to the cell table.
            Row row;
            if (startRow == null)
            {
                row = new Row();

                sheetData.Append(row);
            }
            else
                row = GetRow(ref sheetData, (uint)startRow - 1);


            // In the new row, find the column location to insert a cell in A1.  
            Cell refCell = null;
            //var v = row.Elements<Cell>();


            var cellList = row.Elements<Cell>().ToList<Cell>();
            if (cellList.Count > 0)
                refCell = cellList[0];
            else
                foreach (Cell cell in row.Elements<Cell>())
                {
                    if (string.Compare(cell.CellReference.Value, "A" + rownum.ToString(), true) > 0)
                    {
                        refCell = cell;
                        break;
                    }
                }


            if (key != null && value != null)
            {
                Cell addCell = new Cell();
                row.InsertAt(addCell, 0);
                addCell.CellValue = new CellValue(key.Value);

                addCell.DataType = key.DataType;

                addCell.StyleIndex = Convert.ToUInt32(key.StyleId);
                refCell = addCell;

                addCell = new Cell();
                row.InsertAfter(addCell, refCell);

                addCell.CellValue = new CellValue(value.Value);

                addCell.DataType = value.DataType;

                addCell.StyleIndex = Convert.ToUInt32(value.StyleId);
            }

            //This section is only used for preparing the sheet before data insertion
            if (colCount != null)
            {
                for (int i = 0; i < colCount; i++)
                {
                    Cell addCell = new Cell();
                    row.InsertAfter(addCell, refCell);
                    addCell.CellValue = new CellValue("");

                    addCell.DataType = CellValues.String;

                    addCell.StyleIndex = Convert.ToUInt32(7);
                    refCell = addCell;
                }
            }


            return worksheetPart;
        }

        internal WorksheetPart InsertDataRow(WorksheetPart worksheetPart, List<XlsxValue> rowList, int rownum, SpreadsheetDocument spreadsheetDocument, bool boldRow = false)
        {


            // Get the sheetData cell table.
            SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

            // Add a row to the cell table.
            Row row;
            row = new Row();

            sheetData.Append(row);

            // In the new row, find the column location to insert a cell in A1.  
            Cell refCell = null;
            foreach (Cell cell in row.Elements<Cell>())
            {
                if (string.Compare(cell.CellReference.Value, "A" + rownum.ToString(), true) > 0)
                {
                    refCell = cell;
                    break;
                }
            }

            foreach (XlsxValue str in rowList)
            {
                Cell addCell = new Cell();
                row.InsertAfter(addCell, refCell);
                addCell.CellValue = new CellValue(str.Value);
                //If the value was flagged as a potential number, then check if it parses as a number. If so, it will be a number in Xlsx
                if (str.DataType == CellValues.Number)
                {
                    if (!Double.TryParse(str.Value, out double result))
                        addCell.DataType = new EnumValue<CellValues>(CellValues.String);
                }
                else //otherwise we just allow it to be whatever type it was created as (default is CellValues.String)
                    addCell.DataType = str.DataType;
                if (boldRow)
                    addCell.StyleIndex = Convert.ToUInt32(1);
                else
                    addCell.StyleIndex = Convert.ToUInt32(0);
                refCell = addCell;
            }



            return worksheetPart;
        }

        private Stylesheet GenerateStyleSheet()
        {
            return new Stylesheet(
                new Fonts(
                    new Font(                                                               // Index 0 - The default font.
                        new FontSize() { Val = 11 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "000000" } },
                        new FontName() { Val = "Calibri" }),
                    new Font(                                                               // Index 1 - The bold font.
                        new Bold(),
                        new FontSize() { Val = 11 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "000000" } },
                        new FontName() { Val = "Calibri" }),
                    new Font(                                                               // Index 2 - The Italic font.
                        new Italic(),
                        new FontSize() { Val = 11 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "000000" } },
                        new FontName() { Val = "Calibri" }),
                    new Font(                                                               // Index 3 - The Times Roman font. with 16 size
                        new FontSize() { Val = 16 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "000000" } },
                        new FontName() { Val = "Times New Roman" }),
                    new Font(
                        new Bold(),                                                           // Index 4 - The Calibri font, bold with 13 size
                        new FontSize() { Val = 13 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "000000" } },
                        new FontName() { Val = "Calibri" })
                ),
                new Fills(
                    new Fill(                                                           // Index 0 - The default fill.
                        new PatternFill() { PatternType = PatternValues.None }),
                    new Fill(                                                           // Index 1 - The default fill of gray 125 (required)
                        new PatternFill() { PatternType = PatternValues.Gray125 }),
                    new Fill(                                                           // Index 2 - The yellow fill.
                        new PatternFill(
                            new ForegroundColor() { Rgb = new HexBinaryValue() { Value = "FFFFFF00" } }
                        )
                        { PatternType = PatternValues.Solid })
                ),
                new Borders(
                    new Border(                                                         // Index 0 - The default border.
                        new LeftBorder(),
                        new RightBorder(),
                        new TopBorder(),
                        new BottomBorder(),
                        new DiagonalBorder()),
                    new Border(                                                         // Index 1 - Applies a Left, Right, Top, Bottom border to a cell
                        new LeftBorder(
                            new Color() { Auto = true }
                        )
                        { Style = BorderStyleValues.Thin },
                        new RightBorder(
                            new Color() { Auto = true }
                        )
                        { Style = BorderStyleValues.Thin },
                        new TopBorder(
                            new Color() { Auto = true }
                        )
                        { Style = BorderStyleValues.Thin },
                        new BottomBorder(
                            new Color() { Auto = true }
                        )
                        { Style = BorderStyleValues.Thin },
                        new DiagonalBorder()),
                    new Border(                                                         // Index 2 - White (normally invisible) Border on all sides
                        new LeftBorder(
                            new Color() { Rgb = new HexBinaryValue() { Value = "FFFFFFFF" } }
                        )
                        { Style = BorderStyleValues.Thin },
                        new RightBorder(
                            new Color() { Rgb = new HexBinaryValue() { Value = "FFFFFFFF" } }
                        )
                        { Style = BorderStyleValues.Thin },
                        new TopBorder(
                            new Color() { Rgb = new HexBinaryValue() { Value = "FFFFFFFF" } }
                        )
                        { Style = BorderStyleValues.Thin },
                        new BottomBorder(
                            new Color() { Rgb = new HexBinaryValue() { Value = "FFFFFFFF" } }
                        )
                        { Style = BorderStyleValues.Thin },
                        new DiagonalBorder()),
                    new Border(                                                         // Index 3 - White (normally invisible) Border on all sides except bottom - heavy blue border
                        new LeftBorder(
                            new Color() { Rgb = new HexBinaryValue() { Value = "FFFFFFFF" } }
                        )
                        { Style = BorderStyleValues.Thin },
                        new RightBorder(
                            new Color() { Rgb = new HexBinaryValue() { Value = "FFFFFFFF" } }
                        )
                        { Style = BorderStyleValues.Thin },
                        new TopBorder(
                            new Color() { Rgb = new HexBinaryValue() { Value = "FFFFFFFF" } }
                        )
                        { Style = BorderStyleValues.Thin },
                        new BottomBorder(
                            new Color() { Rgb = new HexBinaryValue() { Value = "A2B8E1" } }
                        )
                        { Style = BorderStyleValues.Thick },
                        new DiagonalBorder())
                ),
                new CellFormats(
                    new CellFormat() { FontId = 0, FillId = 0, BorderId = 0 },                          // Index 0 - The default cell style.  If a cell does not have a style index applied it will use this style combination instead
                    new CellFormat() { FontId = 1, FillId = 0, BorderId = 0, ApplyFont = true },       // Index 1 - Bold 
                    new CellFormat() { FontId = 2, FillId = 0, BorderId = 0, ApplyFont = true },       // Index 2 - Italic
                    new CellFormat() { FontId = 3, FillId = 0, BorderId = 0, ApplyFont = true },       // Index 3 - Times Roman
                    new CellFormat() { FontId = 0, FillId = 2, BorderId = 0, ApplyFill = true },       // Index 4 - Yellow Fill
                    new CellFormat(                                                                   // Index 5 - Alignment
                        new Alignment() { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center }
                    )
                    { FontId = 0, FillId = 0, BorderId = 0, ApplyAlignment = true },
                    new CellFormat() { FontId = 0, FillId = 0, BorderId = 1, ApplyBorder = true },      // Index 6 - Border
                    new CellFormat() { FontId = 0, FillId = 0, BorderId = 2, ApplyBorder = true },       //Index 7 - White (normally invisible) Border, standard fonts
                    new CellFormat() { FontId = 4, FillId = 0, BorderId = 3, ApplyBorder = true },       //Index 8 - White (normally invisible) Border, Calibri 13, Bold 
                    new CellFormat() { FontId = 1, FillId = 0, BorderId = 2, ApplyBorder = true }       //Index 9 - White (normally invisible) Border, Bold
                )
            );
        }



    }
    internal static class XlsxImageHandler
    {
        private static byte[] Image;

        internal static Stream GetImageStream(string url)
        {
            byte[] b = null;
            HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create(url);
            try
            {
                WebResponse myResp = myReq.GetResponse();

                Stream s = myResp.GetResponseStream();

                //using (Stream stream = myResp.GetResponseStream())
                MemoryStream ms = new MemoryStream();

                int count = 0;
                do
                {
                    byte[] buf = new byte[1024];
                    count = s.Read(buf, 0, 1024);
                    ms.Write(buf, 0, count);
                } while (s.CanRead && count > 0);
                b = ms.ToArray();

                Stream stream = new MemoryStream(b);
                return stream;
            }
            catch { return null; }
            //return b;

        }

        private static byte[] GetImageBytes(string url)
        {
            byte[] b = null;
            HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create(url);
            try
            {
                WebResponse myResp = myReq.GetResponse();

                using (Stream stream = myResp.GetResponseStream())
                using (MemoryStream ms = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        byte[] buf = new byte[1024];
                        count = stream.Read(buf, 0, 1024);
                        ms.Write(buf, 0, count);
                    } while (stream.CanRead && count > 0);
                    b = ms.ToArray();
                }


            }
            catch { }
            return b;

        }

        //Get a byte array based on the image at location url
        internal static byte[] Read(string url)
        {
            if (Image == null)
                Image = GetImageBytes(url);
            return Image;
        }
    }

}
