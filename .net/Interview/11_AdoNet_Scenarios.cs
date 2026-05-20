// =====================================================================
//  11) ADO.NET — Q&A and Scenarios
// =====================================================================
//  ADO.NET is the LOW-LEVEL data-access API in .NET. EF Core is built on
//  top of it. Knowing ADO.NET still matters for: perf-critical paths,
//  legacy code, bulk operations, stored procedures, custom mapping.
// =====================================================================
using System;
using System.Data;
using Microsoft.Data.SqlClient;   // modern provider (NuGet)

namespace Interview.AdoNet
{
    // ---------------------------------------------------------------------
    // Q1: What is ADO.NET?
    // A : A set of classes (System.Data + provider-specific assemblies)
    //     for connecting to data sources, executing commands, reading
    //     results. Providers: Microsoft.Data.SqlClient (SQL Server),
    //     Npgsql (Postgres), Oracle.ManagedDataAccess, MySqlConnector, etc.
    // ---------------------------------------------------------------------

    // Q2: Core objects you must know.
    // A : SqlConnection  - opens a connection to the DB.
    //     SqlCommand     - represents a SQL statement or stored proc.
    //     SqlParameter   - parameter for a command (prevents SQL injection).
    //     SqlDataReader  - forward-only, read-only, FAST stream of rows.
    //     SqlDataAdapter - fills a DataSet/DataTable (older, batchy).
    //     DataSet/DataTable - in-memory disconnected cache.
    //     SqlTransaction - explicit transaction scope.
    //     SqlBulkCopy    - high-speed bulk insert.

    // Q3: Connected vs Disconnected architecture?
    // A : Connected    : open connection, use DataReader, close. Low memory.
    //     Disconnected : load into DataSet/DataTable, close, mutate, push back.
    //                    Useful in WinForms / offline scenarios.

    // ---------------------------------------------------------------------
    //  Q4: Minimal "SELECT" example with DataReader.
    // ---------------------------------------------------------------------
    public static class AdoSelectExample
    {
        public static void Demo(string connStr)
        {
            using var conn = new SqlConnection(connStr);
            conn.Open();

            using var cmd = new SqlCommand(
                "SELECT Id, Name FROM Products WHERE CategoryId = @cat", conn);

            // ALWAYS parameterize — prevents SQL injection.
            cmd.Parameters.Add("@cat", SqlDbType.Int).Value = 5;

            using var reader = cmd.ExecuteReader();
            while (reader.Read())                  // forward-only stream
            {
                int    id   = reader.GetInt32(0);
                string name = reader.GetString(1);
                Console.WriteLine($"{id} {name}");
            }
        }                                          // using -> auto Dispose closes connection
    }

    // ---------------------------------------------------------------------
    //  Q5: ExecuteReader vs ExecuteNonQuery vs ExecuteScalar?
    //   - ExecuteReader   : SELECT -> returns rows via DataReader.
    //   - ExecuteNonQuery : INSERT/UPDATE/DELETE/DDL -> returns rows affected.
    //   - ExecuteScalar   : returns first column of first row (e.g., COUNT).
    // ---------------------------------------------------------------------

    // Q6: Calling a stored procedure.
    public static class StoredProc
    {
        public static int CreateOrder(string connStr, int customerId, decimal total)
        {
            using var conn = new SqlConnection(connStr);
            conn.Open();

            using var cmd = new SqlCommand("dbo.usp_CreateOrder", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.Add("@CustomerId", SqlDbType.Int).Value = customerId;
            cmd.Parameters.Add("@Total", SqlDbType.Decimal).Value = total;

            // Output parameter — sproc returns new OrderId.
            var outId = new SqlParameter("@OrderId", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(outId);

            cmd.ExecuteNonQuery();
            return (int)outId.Value!;
        }
    }

    // ---------------------------------------------------------------------
    //  Q7: SqlParameter — why critical?
    //   A : Prevents SQL injection AND lets the server cache the execution
    //       plan (parameterized queries are reusable across values).
    //   BAD :  "WHERE Name = '" + userInput + "'"
    //   GOOD:  cmd.Parameters.Add("@n", SqlDbType.NVarChar, 100).Value = userInput;
    // ---------------------------------------------------------------------

    // Q8: Connection pooling — what is it?
    // A : Connections are pooled by connection string. Open()/Dispose() don't
    //     really create/destroy TCP connections; they just check out/return
    //     to the pool. So always use `using` and close ASAP.
    //     Pool size: "Max Pool Size=200" in the connection string.

    // Q9: How to avoid "connection pool exhausted"?
    // A : - Wrap connections/commands/readers in `using`.
    //     - Don't hold connections across HTTP requests.
    //     - Watch for forgotten DataReaders (each holds the connection).
    //     - Increase Max Pool Size only after fixing the leak.

    // ---------------------------------------------------------------------
    //  Q10: Transactions in ADO.NET.
    // ---------------------------------------------------------------------
    public static class TxExample
    {
        public static void Transfer(string connStr, int fromId, int toId, decimal amount)
        {
            using var conn = new SqlConnection(connStr);
            conn.Open();
            using var tx = conn.BeginTransaction(IsolationLevel.ReadCommitted);
            try
            {
                using (var debit = new SqlCommand(
                    "UPDATE Accounts SET Balance = Balance - @a WHERE Id = @id", conn, tx))
                {
                    debit.Parameters.AddWithValue("@a", amount);
                    debit.Parameters.AddWithValue("@id", fromId);
                    debit.ExecuteNonQuery();
                }
                using (var credit = new SqlCommand(
                    "UPDATE Accounts SET Balance = Balance + @a WHERE Id = @id", conn, tx))
                {
                    credit.Parameters.AddWithValue("@a", amount);
                    credit.Parameters.AddWithValue("@id", toId);
                    credit.ExecuteNonQuery();
                }
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
    }

    // Q11: TransactionScope vs SqlTransaction?
    // A : SqlTransaction    : one connection, lightweight, no MSDTC.
    //     TransactionScope  : ambient transaction across multiple connections
    //                         and even resource managers; may escalate to
    //                         distributed (MSDTC). Use only if needed.

    // Q12: Isolation levels (quick).
    // A : ReadUncommitted - dirty reads.
    //     ReadCommitted   - default; no dirty reads.
    //     RepeatableRead  - same row read twice = same result.
    //     Serializable    - strictest; range locks.
    //     Snapshot        - row-versioned, no readers block writers.

    // ---------------------------------------------------------------------
    //  Q13: SqlBulkCopy — fastest way to insert many rows.
    // ---------------------------------------------------------------------
    public static class BulkExample
    {
        public static void BulkInsert(string connStr, DataTable rows)
        {
            using var conn = new SqlConnection(connStr);
            conn.Open();

            using var bulk = new SqlBulkCopy(conn)
            {
                DestinationTableName = "dbo.Products",
                BatchSize = 5000,
                BulkCopyTimeout = 60
            };
            // Optional column mappings:
            // bulk.ColumnMappings.Add("Id",   "Id");
            // bulk.ColumnMappings.Add("Name", "Name");

            bulk.WriteToServer(rows);
        }
    }

    // ---------------------------------------------------------------------
    //  Q14: DataSet vs DataTable vs DataReader?
    //   - DataReader : forward-only, fastest, low memory, one row at a time.
    //   - DataTable  : in-memory single table.
    //   - DataSet    : collection of DataTables + relationships; heavy.
    //   Use DataReader for performance; DataTable/Set for disconnected ops.
    // ---------------------------------------------------------------------

    // Q15: SqlDataAdapter — what does it do?
    // A : Bridges between SqlCommand and DataSet/DataTable.
    //     Fill() loads data, Update() pushes changes back using the
    //     InsertCommand/UpdateCommand/DeleteCommand you provide (or
    //     SqlCommandBuilder auto-generates them).

    // Q16: Async ADO.NET?
    // A : Every Execute*/Read has *Async variants:
    //     await conn.OpenAsync();
    //     await cmd.ExecuteReaderAsync();
    //     while (await reader.ReadAsync()) { ... }
    //     Crucial in ASP.NET to avoid blocking thread pool.

    // Q17: Handling SqlException?
    // A : ex.Number gives SQL error code; ex.Errors enumerates all.
    //     Common codes: 2627 unique-key, 547 FK violation, -2 timeout,
    //     1205 deadlock victim (retry candidate).

    // Q18: Multiple result sets (MARS)?
    // A : - One command returning multiple SELECTs: reader.NextResult().
    //     - Multiple Active Result Sets on one connection: add
    //       "MultipleActiveResultSets=true" to connection string.

    // Q19: Where to store connection strings?
    // A : appsettings.json -> "ConnectionStrings": { "Default": "..." }
    //     Read via IConfiguration.GetConnectionString("Default").
    //     Use Key Vault / env vars in production.

    // Q20: ADO.NET vs Dapper vs EF Core — which when?
    // A : ADO.NET - max control / perf, more boilerplate.
    //     Dapper  - tiny mapper over ADO.NET; fast + clean.
    //     EF Core - full ORM, change tracking, migrations. Slower than Dapper
    //               for raw reads but much more productive for CRUD.

    // ---------------------------------------------------------------------
    //  SCENARIOS
    // ---------------------------------------------------------------------

    // [Scenario] Q21: App randomly times out under load with
    //   "Timeout expired. The timeout period elapsed prior to obtaining
    //   a connection from the pool."
    // A : Connection leak. Audit for missing `using` or unclosed DataReaders.
    //     Tools: SQL Server Activity Monitor, sp_who2, dotnet-counters.

    // [Scenario] Q22: A query is slow only when called from .NET but fast
    //   in SSMS with the same SQL.
    // A : Parameter sniffing / wrong types. e.g., AddWithValue("@n", "abc")
    //     creates an nvarchar(3), causing implicit conversion + index miss.
    //     Fix: declare exact SqlDbType + Size. Or use OPTION (RECOMPILE).

    // [Scenario] Q23: You must insert 1M rows from a CSV nightly.
    // A : SqlBulkCopy (batch size 5k-50k) + minimal logging + disable
    //     non-clustered indexes during load and rebuild after.

    // [Scenario] Q24: A deadlock is raised intermittently.
    // A : Catch SqlException with Number == 1205 and retry with backoff.
    //     Long-term: consistent locking order, shorter transactions,
    //     consider Snapshot isolation.

    // [Scenario] Q25: How would you implement audit logging at the DB layer?
    // A : Triggers on tables OR a CommandInterceptor (EF) OR wrap ADO.NET
    //     calls in a decorator that logs SQL + params + duration.

    // [Scenario] Q26: Need to return both a list and a count from one round-trip.
    // A : Use a sproc that returns two result sets (SELECT count; SELECT data).
    //     Read first with ExecuteReader, then reader.NextResult() for second.

    // [Scenario] Q27: How do you safely pass a list of IDs to a sproc?
    // A : Use a Table-Valued Parameter (TVP):
    //     - CREATE TYPE dbo.IntList AS TABLE (Id INT PRIMARY KEY);
    //     - SqlParameter { SqlDbType=Structured, TypeName="dbo.IntList",
    //                      Value = dataTableOfIds }
    //     Avoid string-concatenated IN-lists.

    // [Scenario] Q28: Connection string contains a password — how to secure?
    // A : Use Managed Identity (Azure SQL: "Authentication=Active Directory Default")
    //     or store in Key Vault. Avoid plain passwords in config.

    // [Scenario] Q29: How would you migrate ADO.NET code to Dapper?
    // A : Replace SqlDataReader loops with conn.Query<T>(sql, new { id });
    //     Replace ExecuteNonQuery with conn.Execute(...). Keep the same
    //     connection / transaction patterns.

    // [Scenario] Q30: Why does AddWithValue have a bad reputation?
    // A : It infers type/size from the .NET value at runtime, causing
    //     mismatches with the column type -> implicit conversions, index
    //     scans, plan-cache bloat. Prefer Parameters.Add("@n", SqlDbType.X).

    internal static class _Ado { }
}
