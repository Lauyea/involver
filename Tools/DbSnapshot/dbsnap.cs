#:property TargetFramework=net10.0
#:property PublishAot=false

// 本機資料庫快照：備份與還原 .\SQLEXPRESS 的 Involver 資料庫。
// 用在測試 migration、跑會寫入資料的工具（devseed、recsim）或大改資料前，先留一份可以回復的狀態。
//
// 用法（在這個資料夾執行，需要 .NET 10 SDK 與 sqlcmd）：
//   dotnet run dbsnap.cs -- backup [名稱]               備份 Involver（COPY_ONLY，不影響其他備份鏈）。名稱預設為時間，例如 20261120-153000
//   dotnet run dbsnap.cs -- list                        列出這支工具做過的快照
//   dotnet run dbsnap.cs -- restore <名稱>              還原快照並覆蓋 Involver（會中斷所有連線，請先停掉 dev server）
//   dotnet run dbsnap.cs -- restore <名稱> --as <資料庫>  還原成另一個資料庫（名稱必須以 Involver_ 開頭），不動 Involver
//   dotnet run dbsnap.cs -- drop <資料庫>               刪除還原出來的資料庫（只允許 Involver_ 開頭）
//
// 伺服器：環境變數 INVOLVER_SQL_SERVER，預設 .\SQLEXPRESS；只能是本機。資料庫：環境變數 INVOLVER_SQL_DATABASE，預設 Involver。
// 快照存在 SQL Server 的預設備份資料夾（SERVERPROPERTY('InstanceDefaultBackupPath')），檔名為 Involver-snap-<名稱>.bak。

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;


Console.OutputEncoding = Encoding.UTF8;

var server = Environment.GetEnvironmentVariable("INVOLVER_SQL_SERVER") ?? @".\SQLEXPRESS";
var database = Environment.GetEnvironmentVariable("INVOLVER_SQL_DATABASE") ?? "Involver";
var lowerServer = server.Trim().ToLowerInvariant();
var isLocal = lowerServer.StartsWith('.') || lowerServer.StartsWith("(local") || lowerServer.StartsWith("localhost") || lowerServer.StartsWith("127.0.0.1")
    || lowerServer.StartsWith(Environment.MachineName.ToLowerInvariant());
if (!isLocal)
{
    Console.Error.WriteLine($"只能連本機資料庫，目前的主機是「{server}」。");
    return 1;
}

const string FilePrefix = "Involver-snap-";
const string RestoredPrefix = "Involver_";
var mode = args.FirstOrDefault() ?? string.Empty;

try
{
    return mode switch
    {
        "backup" => Backup(args.Skip(1).FirstOrDefault() ?? DateTime.Now.ToString("yyyyMMdd-HHmmss")),
        "list" => List(),
        "restore" when args.Length >= 2 => Restore(args[1], ReadOption("--as") ?? database),
        "drop" when args.Length >= 2 => Drop(args[1]),
        _ => Usage(),
    };
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

int Usage()
{
    Console.WriteLine("用法：dotnet run dbsnap.cs -- backup [名稱] | list | restore <名稱> [--as <資料庫>] | drop <資料庫>");
    return 2;
}

string? ReadOption(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

// ── 指令 ──────────────────────────────

int Backup(string label)
{
    EnsureSafeName(label, "快照名稱");
    var path = Path.Combine(Query("SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000))").Single(), $"{FilePrefix}{label}.bak");
    Execute($"BACKUP DATABASE {Quote(database)} TO DISK = N'{Escape(path)}' WITH COPY_ONLY, INIT, NAME = N'{FilePrefix}{label}';");
    Console.WriteLine($"已備份 {database} → {path}");
    return 0;
}

int List()
{
    var rows = Query($"""
        SELECT REPLACE(REPLACE(f.physical_device_name, LEFT(f.physical_device_name, LEN(f.physical_device_name) - CHARINDEX('\', REVERSE(f.physical_device_name)) + 1), ''), '.bak', ''),
               s.database_name, CONVERT(nvarchar(19), s.backup_finish_date, 120), CAST(s.backup_size / 1048576.0 AS decimal(10, 1)), f.physical_device_name
        FROM msdb.dbo.backupset s
        JOIN msdb.dbo.backupmediafamily f ON f.media_set_id = s.media_set_id
        WHERE f.physical_device_name LIKE N'%\{FilePrefix}%'
        ORDER BY s.backup_finish_date DESC
        """);
    if (rows.Count == 0)
    {
        Console.WriteLine("還沒有快照。用 backup 建立一份。");
        return 0;
    }

    Console.WriteLine("| 名稱 | 資料庫 | 時間 | 大小 (MB) | 檔案 |");
    Console.WriteLine("|---|---|---|---|---|");
    var seen = new HashSet<string>();
    foreach (var columns in rows.Select(r => r.Split('\t')))
    {
        // 同名快照被 INIT 覆蓋過時，msdb 會留下多筆紀錄，只顯示最新的一筆
        if (seen.Add(columns[4]))
        {
            Console.WriteLine($"| {columns[0][FilePrefix.Length..]} | {columns[1]} | {columns[2]} | {columns[3]} | {columns[4]} |");
        }
    }

    return 0;
}

int Restore(string label, string target)
{
    EnsureSafeName(label, "快照名稱");
    if (!target.Equals(database, StringComparison.OrdinalIgnoreCase) && !target.StartsWith(RestoredPrefix, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException($"還原目標只能是 {database}，或以 {RestoredPrefix} 開頭的資料庫。");
    }

    EnsureSafeName(target, "資料庫名稱");
    var backupPath = Path.Combine(Query("SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000))").Single(), $"{FilePrefix}{label}.bak");
    var dataPath = Query("SELECT CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS nvarchar(4000))").Single();

    // 還原成其他名稱時要把檔案搬到新的實體檔名，避免和原本的資料庫衝突
    var moves = new List<string>();
    if (!target.Equals(database, StringComparison.OrdinalIgnoreCase))
    {
        var files = Query($"""
            DECLARE @files TABLE (LogicalName nvarchar(128), PhysicalName nvarchar(260), Type char(1), FileGroupName nvarchar(128), Size numeric(20,0), MaxSize numeric(20,0),
                FileId bigint, CreateLSN numeric(25,0), DropLSN numeric(25,0), UniqueId uniqueidentifier, ReadOnlyLSN numeric(25,0), ReadWriteLSN numeric(25,0),
                BackupSizeInBytes bigint, SourceBlockSize int, FileGroupId int, LogGroupGUID uniqueidentifier, DifferentialBaseLSN numeric(25,0),
                DifferentialBaseGUID uniqueidentifier, IsReadOnly bit, IsPresent bit, TDEThumbprint varbinary(32), SnapshotUrl nvarchar(360));
            INSERT INTO @files EXEC (N'RESTORE FILELISTONLY FROM DISK = N''{Escape(Escape(backupPath))}''');
            SELECT LogicalName, Type FROM @files;
            """);
        foreach (var columns in files.Select(f => f.Split('\t')))
        {
            var extension = columns[1] == "L" ? "_log.ldf" : ".mdf";
            moves.Add($"MOVE N'{Escape(columns[0])}' TO N'{Escape(Path.Combine(dataPath, target + extension))}'");
        }
    }

    var exists = Query($"SELECT COUNT(*) FROM sys.databases WHERE name = N'{Escape(target)}'").Single() == "1";
    var sql = new StringBuilder();
    if (exists)
    {
        sql.AppendLine($"ALTER DATABASE {Quote(target)} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;");
    }

    sql.AppendLine($"RESTORE DATABASE {Quote(target)} FROM DISK = N'{Escape(backupPath)}' WITH REPLACE{string.Concat(moves.Select(m => ", " + m))};");
    sql.AppendLine($"ALTER DATABASE {Quote(target)} SET MULTI_USER;");
    Execute(sql.ToString());
    Console.WriteLine($"已還原 {label} → {target}");
    return 0;
}

int Drop(string target)
{
    if (!target.StartsWith(RestoredPrefix, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException($"只能刪除以 {RestoredPrefix} 開頭的資料庫，不會刪除 {database}。");
    }

    EnsureSafeName(target, "資料庫名稱");
    Execute($"""
        IF DB_ID(N'{Escape(target)}') IS NOT NULL
        BEGIN
            ALTER DATABASE {Quote(target)} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            DROP DATABASE {Quote(target)};
        END
        """);
    Console.WriteLine($"已刪除 {target}");
    return 0;
}

// ── sqlcmd ──────────────────────────────

static void EnsureSafeName(string value, string what)
{
    if (!Regex.IsMatch(value, @"^[A-Za-z0-9_\-]+$"))
    {
        throw new InvalidOperationException($"{what}只能使用英數字、底線與減號：{value}");
    }
}

static string Quote(string name) => $"[{name.Replace("]", "]]")}]";

static string Escape(string value) => value.Replace("'", "''");

void Execute(string sql)
{
    var (exitCode, output) = RunSqlcmd(sql, false);
    if (exitCode != 0)
    {
        throw new InvalidOperationException($"sqlcmd 失敗：{Environment.NewLine}{output}");
    }
}

List<string> Query(string sql)
{
    var (exitCode, output) = RunSqlcmd($"SET NOCOUNT ON;{Environment.NewLine}{sql}", true);
    if (exitCode != 0)
    {
        throw new InvalidOperationException($"sqlcmd 失敗：{Environment.NewLine}{output}");
    }

    return output.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Length > 0).ToList();
}

(int ExitCode, string Output) RunSqlcmd(string sql, bool tabular)
{
    // 透過暫存檔傳 SQL：避免命令列長度與引號問題，並以 UTF-8 讀寫
    var file = Path.Combine(Path.GetTempPath(), $"dbsnap-{Guid.NewGuid():N}.sql");
    File.WriteAllText(file, sql, new UTF8Encoding(false));
    try
    {
        var info = new ProcessStartInfo("sqlcmd")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var arg in new[] { "-S", server, "-E", "-b", "-I", "-x", "-f", "65001", "-i", file })
        {
            info.ArgumentList.Add(arg);
        }

        if (tabular)
        {
            foreach (var arg in new[] { "-h", "-1", "-W", "-s", "\t" })
            {
                info.ArgumentList.Add(arg);
            }
        }

        using var process = Process.Start(info) ?? throw new InvalidOperationException("無法執行 sqlcmd，請確認已安裝並在 PATH 中。");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        return (process.ExitCode, stdout.Result + stderr.Result);
    }
    finally
    {
        File.Delete(file);
    }
}
