using System.Data;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace InventorySalesManagement.Infrastructure;

/// <summary>Turns a table into a CSV file that Excel can open.</summary>
public static class CsvExport
{
    public static FileContentResult ToFile(DataTable table, string fileName)
    {
        var sb = new StringBuilder();
        sb.Append("sep=,\r\n"); // tells Excel to split columns on commas
        sb.Append(string.Join(",", table.Columns.Cast<DataColumn>().Select(c => Escape(c.ColumnName)))).Append("\r\n");
        foreach (DataRow row in table.Rows)
            sb.Append(string.Join(",", table.Columns.Cast<DataColumn>().Select(c => Escape(row[c])))).Append("\r\n");

        var utf8 = new UTF8Encoding(true);
        var bytes = utf8.GetPreamble().Concat(utf8.GetBytes(sb.ToString())).ToArray();
        return new FileContentResult(bytes, "text/csv") { FileDownloadName = fileName };
    }

    private static string Escape(object? value)
    {
        string text = value switch
        {
            null => "",
            DBNull => "",
            DateTime d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? ""
        };
        if (text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0)
            text = "\"" + text.Replace("\"", "\"\"") + "\"";
        return text;
    }
}