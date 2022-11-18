﻿using System;
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

        excelWorksheet.Cells["G3"].Value = $"номер {hd.TypeVip}";
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

        if (isError)
        {
            var onlyLetters1 = new String(addr.channel1Addr.Where(Char.IsLetter).ToArray()).ToUpper();
            var onlyLetters2 = new String(addr.channel2Addr.Where(Char.IsLetter).ToArray()).ToUpper();
            string addrErrors1 = $"{addr.channel1Addr}:{onlyLetters1}11";
            string addrErrors2 = $"{addr.channel2Addr}:{onlyLetters2}19";
            excelWorksheet.Cells[addr.nameAddr].Value = vip.Name;

            excelWorksheet.Cells[addrErrors1].Value =
                0; //vip.VoltageOut1 == 0 ? vip.VoltageOut1 : null;
            excelWorksheet.Cells[addrErrors2].Value =
                0; //vip.VoltageOut2 == 0 ? vip.VoltageOut2 : null;

            excelWorksheet.Cells[addr.channel1Addr].Value =
                vip.VoltageOut1;
            excelWorksheet.Cells[addr.channel2Addr].Value =
                vip.VoltageOut2;
        }
        else
        {
            excelWorksheet.Cells[addr.nameAddr].Value = vip.Name;
            excelWorksheet.Cells[addr.channel1Addr].Value =
                vip.VoltageOut1;
            excelWorksheet.Cells[addr.channel2Addr].Value =
                vip.VoltageOut2;
        }

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
        using var excelPackage = new ExcelPackage(pathReport, "");

        ExcelWorkbook excelWorkBook = excelPackage.Workbook;
        ExcelWorksheet excelWorksheet = excelWorkBook.Worksheets.First(x => x.Name.Contains("Ошибки"));

        excelWorksheet.Cells["D1"].Value = $"номер {vip.Type.Name}";

        var addr = GetChannelAddrErrorReport(vip);

        excelWorksheet.Cells[addr.nameAddr].Value = vip.Name;

        var timeNow = $"\n{DateTime.Now:HH:mm:ss}";

        excelWorksheet.Cells[addr.channel1Addr].Value =
            vip.ErrorVip.VoltageOut1High || vip.ErrorVip.VoltageOut1Low  ? $"{vip.VoltageOut1}{timeNow}" : null;
        excelWorksheet.Cells[addr.channel2Addr].Value =
            vip.ErrorVip.VoltageOut2High ||  vip.ErrorVip.VoltageOut2Low ? $"{vip.VoltageOut2}{timeNow}" : null;

        excelWorksheet.Cells[addr.currentInAddr].Value =
            vip.ErrorVip.CurrentInHigh ? $"{vip.CurrentIn}{timeNow}" : null;
        excelWorksheet.Cells[addr.tempAddr].Value =
            vip.ErrorVip.TemperatureHigh ? $"{vip.Temperature}{timeNow}" : null;
        excelWorksheet.Cells[addr.errConnectAddr].Value =
            vip.Relay.AllDeviceError.CheckIsUnselectError() ? $"Ошибка{timeNow}" : null;

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
        errConnectAddr)
        GetChannelAddrErrorReport(
            Vip vip)
    {
        var nameAddr = $"D4";

        var channel1AddrErrNum = 5;
        if (vip.ErrorVip.VoltageOut1Low)
        {
            channel1AddrErrNum = 6;
        }

        var channel2AddrErrNum = 7;
        if (vip.ErrorVip.VoltageOut2Low)
        {
            channel1AddrErrNum = 8;
        }

        var currentInAddrNum = 9;
        var tempAddrNum = 10;
        var errConnectAddrNum = 11;

        var channel1ErrAddr = $"D{channel1AddrErrNum}";
        var channel2ErrAddr = $"D{channel2AddrErrNum}";
        var currentErrAddr = $"D{currentInAddrNum}";
        var tempAddr = $"D{tempAddrNum}";

        var errConnectAdd = $"D{errConnectAddrNum}";
        switch (vip.Id)
        {
            case 1:
                nameAddr = $"H4";
                channel1ErrAddr = $"E{channel1AddrErrNum}";
                channel2ErrAddr = $"E{channel2AddrErrNum}";

                currentErrAddr = $"E{currentInAddrNum}";
                tempAddr = $"E{tempAddrNum}";
                errConnectAdd = $"E{errConnectAddrNum}";
                break;
            case 2:
                nameAddr = $"I4";
                channel1ErrAddr = $"F{channel1AddrErrNum}";
                channel2ErrAddr = $"F{channel2AddrErrNum}";

                currentErrAddr = $"F{currentInAddrNum}";
                tempAddr = $"F{tempAddrNum}";
                errConnectAdd = $"F{errConnectAddrNum}";
                break;
            case 3:
                nameAddr = $"J4";
                channel1ErrAddr = $"G{channel1AddrErrNum}";
                channel2ErrAddr = $"G{channel2AddrErrNum}";

                currentErrAddr = $"G{currentInAddrNum}";
                tempAddr = $"G{tempAddrNum}";
                errConnectAdd = $"G{errConnectAddrNum}";
                break;
            case 4:
                nameAddr = $"K4";
                channel1ErrAddr = $"H{channel1AddrErrNum}";
                channel2ErrAddr = $"H{channel2AddrErrNum}";

                currentErrAddr = $"H{currentInAddrNum}";
                tempAddr = $"H{tempAddrNum}";
                errConnectAdd = $"H{errConnectAddrNum}";
                break;
            case 5:
                nameAddr = $"L4";
                channel1ErrAddr = $"I{channel1AddrErrNum}";
                channel2ErrAddr = $"I{channel2AddrErrNum}";

                currentErrAddr = $"I{currentInAddrNum}";
                tempAddr = $"I{tempAddrNum}";
                errConnectAdd = $"I{errConnectAddrNum}";
                break;
            case 6:
                nameAddr = $"M4";
                channel1ErrAddr = $"J{channel1AddrErrNum}";
                channel2ErrAddr = $"J{channel2AddrErrNum}";

                currentErrAddr = $"J{currentInAddrNum}";
                tempAddr = $"J{tempAddrNum}";
                errConnectAdd = $"J{errConnectAddrNum}";
                break;
            case 7:
                nameAddr = $"N4";
                channel1ErrAddr = $"K{channel1AddrErrNum}";
                channel2ErrAddr = $"K{channel2AddrErrNum}";

                currentErrAddr = $"K{currentInAddrNum}";
                tempAddr = $"K{tempAddrNum}";
                errConnectAdd = $"K{errConnectAddrNum}";
                break;
            case 8:
                nameAddr = $"O4";
                channel1ErrAddr = $"L{channel1AddrErrNum}";
                channel2ErrAddr = $"L{channel2AddrErrNum}";

                currentErrAddr = $"L{currentInAddrNum}";
                tempAddr = $"L{tempAddrNum}";
                errConnectAdd = $"L{errConnectAddrNum}";
                break;
            case 9:
                nameAddr = $"P4";
                channel1ErrAddr = $"M{channel1AddrErrNum}";
                channel2ErrAddr = $"M{channel2AddrErrNum}";

                currentErrAddr = $"M{currentInAddrNum}";
                tempAddr = $"M{tempAddrNum}";
                errConnectAdd = $"M{errConnectAddrNum}";
                break;
            case 10:
                nameAddr = $"Q4";
                channel1ErrAddr = $"N{channel1AddrErrNum}";
                channel2ErrAddr = $"N{channel2AddrErrNum}";

                currentErrAddr = $"N{currentInAddrNum}";
                tempAddr = $"N{tempAddrNum}";
                errConnectAdd = $"N{errConnectAddrNum}";
                break;
            case 11:
                nameAddr = $"R4";
                channel1ErrAddr = $"O{channel1AddrErrNum}";
                channel2ErrAddr = $"O{channel2AddrErrNum}";

                currentErrAddr = $"O{currentInAddrNum}";
                tempAddr = $"O{tempAddrNum}";
                errConnectAdd = $"O{errConnectAddrNum}";
                break;
        }

        return (nameAddr, channel1ErrAddr, channel2ErrAddr, currentErrAddr, tempAddr, errConnectAdd);
    }
}

public class HeaderReport
{
    public HeaderReport(int reportNum, TypeVip v)
    {
        ReportNum = reportNum;
        ReportData = DateTime.Now.ToString("dd-MM-yyyy");
        TypeVip = v.Name;
        Specifications = v.Specifications;
    }

    public int ReportNum { get; set; }
    public string ReportData { get; set; }
    public string TypeVip { get; set; }
    public string Specifications { get; set; }
}