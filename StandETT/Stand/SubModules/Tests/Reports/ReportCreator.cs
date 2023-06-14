using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;
using ExcelHorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment;
using ExcelVerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment;

namespace StandETT;

public class ReportCreator
{
    public ReportCreator()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    private string pathReport = "";
    private string pathErrorReport = "";

    public bool CheckHeadersReport(HeaderReport hd)
    {
        var path = @$"Протокол № {hd.ReportNum} от {hd.ReportData}.xlsx";
        return File.Exists(path);
    }

    public async Task CreateHeadersReport(HeaderReport hd)
    {
        using var excelPackage = new ExcelPackage("Протокол образец.xlsx", "Zuzlik");
        ExcelWorkbook excelWorkBook = excelPackage.Workbook;
        ExcelWorksheet excelWorksheet = excelWorkBook.Worksheets.First(x => x.Name.Contains("Лист1"));

        excelWorksheet.Cells["C1"].Value =
            $"ПРОТОКОЛ № {hd.ReportNum} {hd.ReportData}г.\nЦеховых испытаний\n{hd.TypeVip} {hd.Specifications}";
        excelWorksheet.Cells["C1:R1"].Merge = true;
        excelWorksheet.Column(3).Style.Font.Size = 12;
        excelWorksheet.Column(3).Style.WrapText = true;
        excelWorksheet.Column(3).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        excelWorksheet.Column(3).Style.VerticalAlignment = ExcelVerticalAlignment.Center;

        excelWorksheet.Cells["G3"].Value = $"Номер {hd.TypeVip}";
        excelWorksheet.Cells["C1:R1"].Merge = true;
        excelWorksheet.Column(3).Style.Font.Size = 12;
        excelWorksheet.Column(3).Style.WrapText = true;
        excelWorksheet.Column(3).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        excelWorksheet.Column(3).Style.VerticalAlignment = ExcelVerticalAlignment.Center;

        pathReport = @$"Протокол № {hd.ReportNum} от {hd.ReportData}.xlsx";
        await excelPackage.SaveAsAsync(pathReport, "");
    }

    public async Task CreateReport(Vip vip, bool isError = false)
    {
        using var excelPackage = new ExcelPackage(pathReport, "");
        ExcelWorkbook excelWorkBook = excelPackage.Workbook;
        ExcelWorksheet excelWorksheet = excelWorkBook.Worksheets.First(x => x.Name.Contains("Лист1"));

        var addr = GetChannelAddrReport(vip);

        excelWorksheet.Cells[addr.nameAddr].Value = vip.Name;

        if (isError)
        {
            if (vip.Type.PrepareMaxVoltageOut1 > 0)
            {
                var onlyLetters1 = new String(addr.channel1Addr.Where(Char.IsLetter).ToArray()).ToUpper();
                string addrChannel1 = $"{addr.channel1Addr}:{onlyLetters1}22";
                excelWorksheet.Cells[addrChannel1].Value = 0;
                excelWorksheet.Cells[addr.channel1Addr].Value = vip.VoltageOut1;
            }

            if (vip.Type.PrepareMaxVoltageOut2 > 0)
            {
                var onlyLetters2 = new String(addr.channel2Addr.Where(Char.IsLetter).ToArray()).ToUpper();
                string addChannel2 = $"{addr.channel2Addr}:{onlyLetters2}43";
                excelWorksheet.Cells[addChannel2].Value = 0;
                excelWorksheet.Cells[addr.channel2Addr].Value = vip.VoltageOut2;
            }
        }
        else
        {
            excelWorksheet.Cells[addr.channel1Addr].Value = vip.VoltageOut1;

            if (vip.Type.PrepareMaxVoltageOut2 > 0)
            {
                excelWorksheet.Cells[addr.channel2Addr].Value = vip.VoltageOut2;
            }
        }

        await excelPackage.SaveAsync();
    }

    public async Task CreateErrorReport(Vip vip, bool isReset = false)
    {
        using var excelPackage = new ExcelPackage(pathReport, "");

        ExcelWorkbook excelWorkBook = excelPackage.Workbook;
        ExcelWorksheet excelWorksheet = excelWorkBook.Worksheets.First(x => x.Name.Contains("Ошибки"));

        excelWorksheet.Cells["D1"].Value = $"номер {vip.Type.Name}";

        var addr = GetChannelAddrErrorReport(vip);

        excelWorksheet.Cells[addr.nameAddr].Value = vip.Name;

        var timeNow = $"/\n{DateTime.Now:HH:mm:ss}";

        if (!isReset)
        {
            excelWorksheet.Cells[addr.channel1Addr].Value =
                vip.ErrorVip.VoltageOut1High || vip.ErrorVip.VoltageOut1Low ? $"{vip.VoltageOut1}{timeNow}" : null;
            excelWorksheet.Cells[addr.channel2Addr].Value =
                vip.ErrorVip.VoltageOut2High || vip.ErrorVip.VoltageOut2Low ? $"{vip.VoltageOut2}{timeNow}" : null;

            excelWorksheet.Cells[addr.currentInAddr].Value =
                vip.ErrorVip.CurrentInErr ? $"{vip.CurrentIn}{timeNow}" : null;
            excelWorksheet.Cells[addr.tempAddr].Value =
                vip.ErrorVip.TemperatureIn ? $"{vip.TemperatureIn}{timeNow}" : null;
            excelWorksheet.Cells[addr.tempAddrOut].Value =
                vip.ErrorVip.TemperatureOut ? $"{vip.TemperatureOut}{timeNow}" : null;
            excelWorksheet.Cells[addr.errConnectAddr].Value =
                vip.Relay.AllDeviceError.CheckIsUnselectError() ? $"X{timeNow}" : null;
        }
        else
        {
            if (vip.CurrentTestVip == TypeOfTestRun.AvailabilityCheckVip)
            {
                excelWorksheet.Cells[addr.errResetAddr ].Value = $"Сброс ПИ{timeNow}";
            }

            else if (vip.CurrentTestVip == TypeOfTestRun.MeasurementZero)
            {
                excelWorksheet.Cells[addr.errResetAddr].Value = $"Сброс НКУ{timeNow}";
            }

            else if (vip.CurrentTestVip is TypeOfTestRun.CycleMeasurement or TypeOfTestRun.CycleCheck)
            {
                excelWorksheet.Cells[addr.errResetAddr].Value = $"Сброс ЦИ{timeNow}";
            }
            else
            {
                excelWorksheet.Cells[addr.errResetAddr].Value =
                    vip.Relay.AllDeviceError.CheckIsUnselectError() ? $"Сброс устр.{timeNow}" : null;
            }
        }

        await excelPackage.SaveAsync();
    }

    (string nameAddr, string channel1Addr, string channel2Addr) GetChannelAddrReport(Vip vipReport)
    {
        var channel1AddrNum = vipReport.Channel1AddrNum;
        var channel2AddrNum = vipReport.Channel2AddrNum;

        var nameAddr = $"G5";

        var channel1Addr = $"G{channel1AddrNum}";
        var channel2Addr = $"G{channel2AddrNum}";

        switch (vipReport.Id)
        {
            case 1:
                nameAddr = $"H5";
                channel1Addr = $"H{channel1AddrNum}";
                channel2Addr = $"H{channel2AddrNum}";
                break;
            case 2:
                nameAddr = $"I5";
                channel1Addr = $"I{channel1AddrNum}";
                channel2Addr = $"I{channel2AddrNum}";
                break;
            case 3:
                nameAddr = $"J5";
                channel1Addr = $"J{channel1AddrNum}";
                channel2Addr = $"J{channel2AddrNum}";
                break;
            case 4:
                nameAddr = $"K5";
                channel1Addr = $"K{channel1AddrNum}";
                channel2Addr = $"K{channel2AddrNum}";
                break;
            case 5:
                nameAddr = $"L5";
                channel1Addr = $"L{channel1AddrNum}";
                channel2Addr = $"L{channel2AddrNum}";
                break;
            case 6:
                nameAddr = $"M5";
                channel1Addr = $"M{channel1AddrNum}";
                channel2Addr = $"M{channel2AddrNum}";
                break;
            case 7:
                nameAddr = $"N5";
                channel1Addr = $"N{channel1AddrNum}";
                channel2Addr = $"N{channel2AddrNum}";
                break;
            case 8:
                nameAddr = $"O5";
                channel1Addr = $"O{channel1AddrNum}";
                channel2Addr = $"O{channel2AddrNum}";
                break;
            case 9:
                nameAddr = $"P5";
                channel1Addr = $"P{channel1AddrNum}";
                channel2Addr = $"P{channel2AddrNum}";
                break;
            case 10:
                nameAddr = $"Q5";
                channel1Addr = $"Q{channel1AddrNum}";
                channel2Addr = $"Q{channel2AddrNum}";
                break;
            case 11:
                nameAddr = $"R5";
                channel1Addr = $"R{channel1AddrNum}";
                channel2Addr = $"R{channel2AddrNum}";
                break;
        }

        return (nameAddr, channel1Addr, channel2Addr);
    }

    (string nameAddr, string channel1Addr, string channel2Addr, string currentInAddr, string tempAddr, string
        tempAddrOut, string errConnectAddr, string errResetAddr)
        GetChannelAddrErrorReport(Vip vip)
    {
        var channel1AddrErrNum = 5;
        if (vip.ErrorVip.VoltageOut1Low)
        {
            channel1AddrErrNum = 6;
        }

        var channel2AddrErrNum = 7;
        if (vip.ErrorVip.VoltageOut2Low)
        {
            channel2AddrErrNum = 8;
        }

        var currentInAddrNum = 9;
        var tempAddrNumIn = 10;
        var tempAddrNumOut = 11;
        var errConnectAddrNum = 12;
        var errResetAddrNum = 13;

        var nameAddr = $"D4";

        var channel1ErrAddr = $"D{channel1AddrErrNum}";
        var channel2ErrAddr = $"D{channel2AddrErrNum}";

        var currentErrAddr = $"D{currentInAddrNum}";
        var tempAddr = $"D{tempAddrNumIn}";
        var tempAddrOut = $"D{tempAddrNumOut}";
        var errConnectAdd = $"D{errConnectAddrNum}";
        var errResetAdd = $"D{errResetAddrNum}";

        switch (vip.Id)
        {
            case 1:
                nameAddr = $"E4";

                channel1ErrAddr = $"E{channel1AddrErrNum}";
                channel2ErrAddr = $"E{channel2AddrErrNum}";

                currentErrAddr = $"E{currentInAddrNum}";

                tempAddr = $"E{tempAddrNumIn}";
                tempAddrOut = $"E{tempAddrNumOut}";

                errConnectAdd = $"E{errConnectAddrNum}";
                errResetAdd = $"E{errResetAddrNum}";
                break;
            case 2:
                nameAddr = $"F4";

                channel1ErrAddr = $"F{channel1AddrErrNum}";
                channel2ErrAddr = $"F{channel2AddrErrNum}";

                currentErrAddr = $"F{currentInAddrNum}";

                tempAddr = $"F{tempAddrNumIn}";
                tempAddrOut = $"F{tempAddrNumOut}";

                errConnectAdd = $"F{errConnectAddrNum}";
                errResetAdd = $"F{errResetAddrNum}";

                break;
            case 3:
                nameAddr = $"G4";

                channel1ErrAddr = $"G{channel1AddrErrNum}";
                channel2ErrAddr = $"G{channel2AddrErrNum}";

                currentErrAddr = $"G{currentInAddrNum}";

                tempAddr = $"G{tempAddrNumIn}";
                tempAddrOut = $"G{tempAddrNumOut}";

                errConnectAdd = $"G{errConnectAddrNum}";
                errResetAdd = $"G{errResetAddrNum}";
                break;
            case 4:
                nameAddr = $"H4";

                channel1ErrAddr = $"H{channel1AddrErrNum}";
                channel2ErrAddr = $"H{channel2AddrErrNum}";

                currentErrAddr = $"H{currentInAddrNum}";

                tempAddr = $"H{tempAddrNumIn}";
                tempAddrOut = $"H{tempAddrNumOut}";

                errConnectAdd = $"H{errConnectAddrNum}";
                errResetAdd = $"H{errResetAddrNum}";
                break;
            case 5:
                nameAddr = $"I4";

                channel1ErrAddr = $"I{channel1AddrErrNum}";
                channel2ErrAddr = $"I{channel2AddrErrNum}";

                currentErrAddr = $"I{currentInAddrNum}";

                tempAddr = $"I{tempAddrNumIn}";
                tempAddrOut = $"I{tempAddrNumOut}";

                errConnectAdd = $"I{errConnectAddrNum}";
                errResetAdd = $"I{errResetAddrNum}";
                break;
            case 6:
                nameAddr = $"J4";

                channel1ErrAddr = $"J{channel1AddrErrNum}";
                channel2ErrAddr = $"J{channel2AddrErrNum}";

                currentErrAddr = $"J{currentInAddrNum}";

                tempAddr = $"J{tempAddrNumIn}";
                tempAddrOut = $"J{tempAddrNumOut}";

                errConnectAdd = $"J{errConnectAddrNum}";
                errResetAdd = $"J{errResetAddrNum}";
                break;
            case 7:
                nameAddr = $"K4";

                channel1ErrAddr = $"K{channel1AddrErrNum}";
                channel2ErrAddr = $"K{channel2AddrErrNum}";

                currentErrAddr = $"K{currentInAddrNum}";

                tempAddr = $"K{tempAddrNumIn}";
                tempAddrOut = $"K{tempAddrNumOut}";

                errConnectAdd = $"K{errConnectAddrNum}";
                errResetAdd = $"K{errResetAddrNum}";
                break;
                
            case 8:
                nameAddr = $"L4";

                channel1ErrAddr = $"L{channel1AddrErrNum}";
                channel2ErrAddr = $"L{channel2AddrErrNum}";

                currentErrAddr = $"L{currentInAddrNum}";

                tempAddr = $"L{tempAddrNumIn}";
                tempAddrOut = $"L{tempAddrNumOut}";

                errConnectAdd = $"L{errConnectAddrNum}";
                errResetAdd = $"L{errResetAddrNum}";
                break;
            case 9:
                nameAddr = $"M4";

                channel1ErrAddr = $"M{channel1AddrErrNum}";
                channel2ErrAddr = $"M{channel2AddrErrNum}";

                currentErrAddr = $"M{currentInAddrNum}";

                tempAddr = $"M{tempAddrNumIn}";
                tempAddrOut = $"M{tempAddrNumOut}";

                errConnectAdd = $"M{errConnectAddrNum}";
                errResetAdd = $"M{errResetAddrNum}";
                break;
            case 10:
                nameAddr = $"N4";

                channel1ErrAddr = $"N{channel1AddrErrNum}";
                channel2ErrAddr = $"N{channel2AddrErrNum}";

                currentErrAddr = $"N{currentInAddrNum}";

                tempAddr = $"N{tempAddrNumIn}";
                tempAddrOut = $"N{tempAddrNumOut}";

                errConnectAdd = $"N{errConnectAddrNum}";
                errResetAdd = $"N{errResetAddrNum}";
                break;
            case 11:
                nameAddr = $"O4";

                channel1ErrAddr = $"O{channel1AddrErrNum}";
                channel2ErrAddr = $"O{channel2AddrErrNum}";

                currentErrAddr = $"O{currentInAddrNum}";

                tempAddr = $"O{tempAddrNumIn}";
                tempAddrOut = $"O{tempAddrNumOut}";

                errConnectAdd = $"O{errConnectAddrNum}";
                errResetAdd = $"O{errResetAddrNum}";
                break;
        }

        return (nameAddr, channel1ErrAddr, channel2ErrAddr, currentErrAddr, tempAddr, tempAddrOut, errConnectAdd, errResetAdd);
    }
}

public class HeaderReport
{
    public HeaderReport(string reportNum, TypeVip v)
    {
        ReportNum = reportNum;
        ReportData = DateTime.Now.ToString("dd-MM-yyyy");
        TypeVip = v.Name;
        Specifications = v.Specifications;
    }

    public string ReportNum { get; set; }
    public string ReportData { get; set; }
    public string TypeVip { get; set; }
    public string Specifications { get; set; }
}