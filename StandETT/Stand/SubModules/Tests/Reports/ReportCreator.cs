using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;
using Syncfusion.XlsIO;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using ExcelHorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment;
using ExcelVerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment;


namespace StandETT;

public class ReportCreator
{
    public ReportCreator()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    // private ExcelWorksheet excelWorksheet;
    private string pathReport = "";
    private string pathErrorReport = "";

    public async Task CreateHeadersReport(HeaderReport hd)
    {
        using var excelPackage = new ExcelPackage("Протокол образец.xlsx", "Zuzlik");
        ExcelWorkbook excelWorkBook = excelPackage.Workbook;
        ExcelWorksheet excelWorksheet = excelWorkBook.Worksheets.First(x => x.Name.Contains("Лист1"));

        excelWorksheet.Cells["C1"].Value =
            $"ПРОТОКОЛ № {hd.ReportNum} {hd.ReportData}г.\nЦеховых испытаний\n{hd.TypeVip} {hd.Specifications} ТУ";
        excelWorksheet.Cells["C1:R1"].Merge = true;
        excelWorksheet.Column(3).Style.Font.Size = 12;
        excelWorksheet.Column(3).Style.WrapText = true;
        excelWorksheet.Column(3).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        excelWorksheet.Column(3).Style.VerticalAlignment = ExcelVerticalAlignment.Center;

        excelWorksheet.Cells["G3"].Value = $"номер ВИП{hd.TypeVip}";
        excelWorksheet.Cells["C1:R1"].Merge = true;
        excelWorksheet.Column(3).Style.Font.Size = 12;
        excelWorksheet.Column(3).Style.WrapText = true;
        excelWorksheet.Column(3).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        excelWorksheet.Column(3).Style.VerticalAlignment = ExcelVerticalAlignment.Center;

        //TODO зачем это?
        //excelWorksheet.Cells["G1"].Value = $"{hd.VipId}";

        pathReport = @$"Протокол № {hd.ReportNum} от {hd.ReportData}.xlsx";
        await excelPackage.SaveAsAsync(pathReport, "");
    }

    public async Task CreateReport(Vip vip)
    {
        using var excelPackage = new ExcelPackage(pathReport);
        ExcelWorkbook excelWorkBook = excelPackage.Workbook;
        ExcelWorksheet excelWorksheet = excelWorkBook.Worksheets.First(x => x.Name.Contains("Лист1"));

        var addr = GetChannelAddrReport(vip);

        excelWorksheet.Cells[addr.addrName].Value = vip.Name;
        excelWorksheet.Cells[addr.addrChannel1].Value =
            vip.VoltageOut1; //vip.VoltageOut1 == 0 ? vip.VoltageOut1 : null;
        excelWorksheet.Cells[addr.addrChannel2].Value =
            vip.VoltageOut2; //vip.VoltageOut2 == 0 ? vip.VoltageOut2 : null;

        await excelPackage.SaveAsync();
    }

    // public async Task CreateHeadersErrorReport(HeaderReport hd)
    // {
    //     using var excelPackage = new ExcelPackage(pathReport);
    //     ExcelWorkbook excelWorkBook = excelPackage.Workbook;
    //     ExcelWorksheet excelWorksheet = excelWorkBook.Worksheets.First(x => x.Name.Contains("Ошибки"));
    //     
    //     excelWorksheet.Cells[addr.addrName].Value = vip.Name;
    //     excelWorksheet.Cells[addr.addrChannel1].Value =
    //         vip.VoltageOut1; //vip.VoltageOut1 == 0 ? vip.VoltageOut1 : null;
    //     excelWorksheet.Cells[addr.addrChannel2].Value =
    //         vip.VoltageOut2; //vip.VoltageOut2 == 0 ? vip.VoltageOut2 : null;
    // }

    public async Task CreateErrorReport(Vip vip)
    {
        using var excelPackage = new ExcelPackage(pathReport);
        var addr = GetChannelAddrReport(vip);

        await excelPackage.SaveAsync();
    }

    (string addrName, string addrChannel1, string addrChannel2) GetChannelAddrReport(Vip vipReport)
    {
        string nameAddr = $"G5";
        string channel1Addr = $"G6";
        string channel2Addr = $"G14";

        switch (vipReport.Id)
        {
            case 1:
                nameAddr = $"H5";
                channel1Addr = $"H6";
                channel2Addr = $"H14";
                break;
            case 2:
                nameAddr = $"I5";
                channel1Addr = $"I6";
                channel2Addr = $"I14";
                break;
            case 3:
                nameAddr = $"J5";
                channel1Addr = $"J6";
                channel2Addr = $"J14";
                break;
            case 4:
                nameAddr = $"K5";
                channel1Addr = $"K6";
                channel2Addr = $"K14";
                break;
            case 5:
                nameAddr = $"L5";
                channel1Addr = $"L6";
                channel2Addr = $"L14";
                break;
            case 6:
                nameAddr = $"M5";
                channel1Addr = $"M6";
                channel2Addr = $"M14";
                break;
            case 7:
                nameAddr = $"N5";
                channel1Addr = $"N6";
                channel2Addr = $"N14";
                break;
            case 8:
                nameAddr = $"O5";
                channel1Addr = $"O6";
                channel2Addr = $"O14";
                break;
            case 9:
                nameAddr = $"P5";
                channel1Addr = $"P6";
                channel2Addr = $"P14";
                break;
            case 10:
                nameAddr = $"Q5";
                channel1Addr = $"Q6";
                channel2Addr = $"Q14";
                break;
            case 11:
                nameAddr = $"R5";
                channel1Addr = $"R6";
                channel2Addr = $"R14";
                break;
        }

        return (nameAddr, channel1Addr, channel2Addr);
    }

    // (string addrName, string addrChannel1, string addrChannel2) GetChannelAddrErrorReport(Vip vipReport)
    // {
    //     string nameAddr = $"D5";
    //     string channel1Addr = $"G6";
    //     string channel2Addr = $"G14";
    //     
    //     if (vipReport.Id == 1)
    //     {
    //         nameAddr = $"E4";
    //         channel1Addr = $"H6";
    //         channel2Addr = $"H14";
    //     }
    //
    //     if (vipReport.Id == 2)
    //     {
    //         nameAddr = $"F4";
    //         channel1Addr = $"I6";
    //         channel2Addr = $"I14";
    //     }
    //
    //     if (vipReport.Id == 3)
    //     {
    //         nameAddr = $"G4";
    //         channel1Addr = $"J6";
    //         channel2Addr = $"J14";
    //     }
    //
    //     if (vipReport.Id == 4)
    //     {
    //         nameAddr = $"H4";
    //         channel1Addr = $"K6";
    //         channel2Addr = $"K14";
    //     }
    //
    //     if (vipReport.Id == 5)
    //     {
    //         nameAddr = $"J4";
    //         channel1Addr = $"L6";
    //         channel2Addr = $"L14";
    //     }
    //
    //     if (vipReport.Id == 6)
    //     {
    //         nameAddr = $"K4";
    //         channel1Addr = $"M6";
    //         channel2Addr = $"M14";
    //     }
    //
    //     if (vipReport.Id == 7)
    //     {
    //         nameAddr = $"L4";
    //         channel1Addr = $"N6";
    //         channel2Addr = $"N14";
    //     }
    //
    //     if (vipReport.Id == 8)
    //     {
    //         nameAddr = $"M4";
    //         channel1Addr = $"O6";
    //         channel2Addr = $"O14";
    //     }
    //
    //     if (vipReport.Id == 9)
    //     {
    //         nameAddr = $"N4";
    //         channel1Addr = $"P6";
    //         channel2Addr = $"P14";
    //     }
    //
    //     if (vipReport.Id == 10)
    //     {
    //         nameAddr = $"O4";
    //         channel1Addr = $"Q6";
    //         channel2Addr = $"Q14";
    //     }
    //
    //     if (vipReport.Id == 11)
    //     {
    //         nameAddr = $"P4";
    //         channel1Addr = $"R6";
    //         channel2Addr = $"R14";
    //     }
    // }
}

public class HeaderReport
{
    public HeaderReport(int reportNum, TypeVip v)
    {
        ReportNum = reportNum;
        ReportData = DateTime.Now.ToString("dd-MM-yyyy");
        TypeVip = v.Type;
        Specifications = v.Specifications;
    }
    
    public int ReportNum { get; set; }
    public string ReportData { get; set; }
    public string TypeVip { get; set; }
    public string Specifications { get; set; }
}