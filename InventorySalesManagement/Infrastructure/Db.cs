using System.Data;
using System.Security.Claims;
using Microsoft.Data.SqlClient;

namespace InventorySalesManagement.Infrastructure;

/// <summary>Small helpers used by the sales, dashboard and report pages.</summary>
public static class Db
{
    public static string ConnectionString(IConfiguration config) =>
        config.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection.");

    /// <summary>Runs a stored procedure and returns every result set.</summary>
    public static DataSet Query(string connectionString, string procedure, params SqlParameter[] parameters)
    {
        using var con = new SqlConnection(connectionString);
        using var cmd = new SqlCommand(procedure, con) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddRange(parameters);
        using var adapter = new SqlDataAdapter(cmd);
        var ds = new DataSet();
        adapter.Fill(ds);
        return ds;
    }

    public static SqlParameter Param(string name, object? value) =>
        new(name, value ?? DBNull.Value);

    /// <summary>Converts a table to a list of dictionaries (for JSON).</summary>
    public static List<Dictionary<string, object?>> ToRows(DataTable table)
    {
        var rows = new List<Dictionary<string, object?>>();
        foreach (DataRow r in table.Rows)
        {
            var d = new Dictionary<string, object?>();
            foreach (DataColumn c in table.Columns)
                d[c.ColumnName] = r[c] is DBNull ? null : r[c];
            rows.Add(d);
        }
        return rows;
    }

    /// <summary>
    /// Gets Users.UserId of the logged-in user. Tries the NameIdentifier / UserId claim first,
    /// then falls back to looking the user up by user name.
    /// </summary>
    public static int? CurrentUserId(ClaimsPrincipal user, string connectionString)
    {
        foreach (var type in new[] { ClaimTypes.NameIdentifier, "UserId", "userId", "sub" })
        {
            var value = user.FindFirst(type)?.Value;
            if (int.TryParse(value, out var id)) return id;
        }

        var name = user.Identity?.Name ?? user.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrWhiteSpace(name)) return null;

        using var con = new SqlConnection(connectionString);
        using var cmd = new SqlCommand("SELECT UserId FROM dbo.Users WHERE UserName = @n OR FullName = @n", con);
        cmd.Parameters.AddWithValue("@n", name);
        con.Open();
        var result = cmd.ExecuteScalar();
        return result is int i ? i : null;
    }
}
