#:property TargetFramework=net10.0
#:property PublishAot=false

// Migration 部署前檢查：模擬 deploy.yml 在正式環境執行的 idempotent script，確認新的 migration 不會讓部署失敗。
//
// 背景：EF Core 10 的 `dotnet ef migrations script --idempotent` 把每個 migration 放在同一個批次，不會用 EXEC 包住
// migrationBuilder.Sql。SQL Server 會先編譯整個批次再執行，所以手寫 SQL 若參照同一個 migration 才改名、刪除或新增的欄位，
// 就會出現 Invalid column name；而且 IF NOT EXISTS 的略過發生在編譯之後，之後每次部署都會失敗。
//
// 用法（在這個資料夾執行，需要 .NET 10 SDK、dotnet-ef（dotnet tool restore）與 sqlcmd）：
//   dotnet run migcheck.cs                       靜態檢查＋產生 script＋兩個情境（已套用全部 migration 的資料庫重跑、停在前一版的資料庫補跑）
//   dotnet run migcheck.cs -- --static           只做靜態檢查：這個分支新增的 migration 裡沒有包 EXEC 的 migrationBuilder.Sql
//   dotnet run migcheck.cs -- --with-data        另外多一個情境：複製一份本機 Involver（含資料）來跑
//   dotnet run migcheck.cs -- --from <名稱>      指定「前一版」的 migration（預設：origin/master 上最新的 migration）
//   dotnet run migcheck.cs -- --all              靜態檢查所有 migration（舊的 migration 可能有已知、已部署過的寫法）
//   dotnet run migcheck.cs -- --keep             保留測試用資料庫（InvolverMigCheck_*）方便除錯
//   dotnet run migcheck.cs -- --no-build         產生 script 時不重新建置
//
// 伺服器：環境變數 INVOLVER_SQL_SERVER，預設 .\SQLEXPRESS；只能是本機。測試用資料庫一律以 InvolverMigCheck_ 開頭，結束時刪除。
// 結束代碼：0 全部通過；1 有失敗或靜態檢查有問題；2 用法錯誤或環境問題。

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;


Console.OutputEncoding = Encoding.UTF8;

var server = Environment.GetEnvironmentVariable("INVOLVER_SQL_SERVER") ?? @".\SQLEXPRESS";
var lowerServer = server.Trim().ToLowerInvariant();
var isLocal = lowerServer.StartsWith('.') || lowerServer.StartsWith("(local") || lowerServer.StartsWith("localhost") || lowerServer.StartsWith("127.0.0.1")
    || lowerServer.StartsWith(Environment.MachineName.ToLowerInvariant());
if (!isLocal)
{
    Console.Error.WriteLine($"只能連本機資料庫，目前的主機是「{server}」。");
    return 2;
}

if (args.Contains("--help") || args.Contains("-h"))
{
    Console.WriteLine("用法：dotnet run migcheck.cs -- [--static] [--with-data] [--from <migration>] [--all] [--keep] [--no-build]");
    return 0;
}

var staticOnly = args.Contains("--static");
var withData = args.Contains("--with-data");
var scanAll = args.Contains("--all");
var keep = args.Contains("--keep");
var noBuild = args.Contains("--no-build");
var fromIndex = Array.IndexOf(args, "--from");
var fromOption = fromIndex >= 0 && fromIndex + 1 < args.Length ? args[fromIndex + 1] : null;

var root = FindRepoRoot();
if (root is null)
{
    Console.Error.WriteLine("找不到 Involver.sln，請在儲存庫內執行。");
    return 2;
}

const string MigrationsDir = "DataAccess/Migrations";
const string DbPrefix = "InvolverMigCheck_";
var workDir = Directory.CreateTempSubdirectory("migcheck-").FullName;
var failed = false;

// ── 1. 靜態檢查 ──────────────────────────────

var baseRef = Git("rev-parse --verify --quiet origin/master").ExitCode == 0 ? "origin/master" : "master";
var mergeBase = Git($"merge-base HEAD {baseRef}").Output.Trim();
var allMigrations = ListMigrations(Directory.GetFiles(Path.Combine(root, MigrationsDir), "*.cs").Select(Path.GetFileName)!);
var baseMigrations = ListMigrations(Git($"ls-tree --name-only {mergeBase} {MigrationsDir}/").Output.Split('\n').Select(l => l.Trim().Split('/').Last()));
var newMigrations = allMigrations.Except(baseMigrations).ToList();

Console.WriteLine($"比較基準：{baseRef}（{mergeBase[..Math.Min(10, mergeBase.Length)]}）");
Console.WriteLine(newMigrations.Count == 0 ? "這個分支沒有新增 migration。" : $"這個分支新增的 migration：{string.Join("、", newMigrations)}");
Console.WriteLine();

var staticTargets = scanAll ? allMigrations : newMigrations;
var staticIssues = staticTargets.SelectMany(m => CheckUnwrappedSql(Path.Combine(root, MigrationsDir, m + ".cs"))).ToList();
Console.WriteLine($"【靜態檢查】{(scanAll ? "所有" : "新增的")} migration 共 {staticTargets.Count} 個");
if (staticIssues.Count == 0)
{
    Console.WriteLine("  通過：Up() 裡的 migrationBuilder.Sql 都有用 EXEC 包住");
}
else
{
    failed = true;
    foreach (var issue in staticIssues)
    {
        Console.WriteLine($"  {issue}");
    }

    Console.WriteLine("  修正方式：migrationBuilder.Sql($\"EXEC(N'{sql.Replace(\"'\", \"''\")}');\")，或沿用 UnifyTags migration 的 Exec(...) 輔助方法。");
}

Console.WriteLine();
if (staticOnly)
{
    return Finish();
}

// ── 2. 產生 script ──────────────────────────────

if (Run("dotnet", "tool restore", root).ExitCode != 0)
{
    Console.Error.WriteLine("dotnet tool restore 失敗。");
    return 2;
}

var fromMigration = fromOption ?? baseMigrations.LastOrDefault();
if (fromMigration is not null && fromMigration == allMigrations.LastOrDefault())
{
    // 分支沒有新的 migration：改用倒數第二個，至少驗證最後一個 migration 的補跑
    fromMigration = allMigrations.Count >= 2 ? allMigrations[^2] : null;
}

if (fromMigration is not null && !allMigrations.Contains(fromMigration))
{
    Console.Error.WriteLine($"找不到 migration「{fromMigration}」。");
    return 2;
}

var fullScript = Path.Combine(workDir, "full.sql");
Console.WriteLine("【產生 script】dotnet ef migrations script --idempotent（和 deploy.yml 相同）");
var efBase = $"ef migrations script --project DataAccess --startup-project Involver --idempotent";
var efResult = Run("dotnet", $"{efBase}{(noBuild ? " --no-build" : string.Empty)} --output \"{fullScript}\"", root);
if (efResult.ExitCode != 0)
{
    Console.Error.WriteLine(efResult.Output);
    return 2;
}

Console.WriteLine($"  {fullScript}");
Console.WriteLine();

// ── 3. 情境 ──────────────────────────────

// 基準資料庫用 dotnet ef database update 逐一套用 migration 建立（每個指令分開執行，不受單一批次編譯的影響）；
// 注意：舊的 20250922090820_ArticleIntegrationDataTable 沒有包 EXEC，所以「從空資料庫跑 idempotent script」本來就會失敗，
// 這只影響建立全新環境，正式資料庫已經套用過，不受影響。
var scenarios = new List<(string Name, string Database, Func<string, string?> Prepare)>
{
    ("已套用全部 migration 的資料庫再跑一次完整 script（模擬沒有新 migration 時的部署）", $"{DbPrefix}Latest", db => UpdateDatabase(db, allMigrations[^1])),
};
if (fromMigration is not null)
{
    scenarios.Add(($"停在 {fromMigration} 的資料庫補跑完整 script（模擬正式資料庫）", $"{DbPrefix}Previous", db => UpdateDatabase(db, fromMigration)));
}

if (withData)
{
    scenarios.Add(("複製本機 Involver（含資料）跑完整 script", $"{DbPrefix}Data", CopyLocalDatabase));
}

foreach (var (name, database, prepare) in scenarios)
{
    Console.WriteLine($"【情境】{name}");
    var stopwatch = Stopwatch.StartNew();
    DropDatabase(database);
    var error = database.EndsWith("Data", StringComparison.Ordinal) ? null : Sql($"CREATE DATABASE [{database}];");
    error ??= prepare(database);
    error ??= RunScript(database, fullScript);
    Console.WriteLine(error is null ? $"  通過（{stopwatch.Elapsed.TotalSeconds:0.0} 秒）" : $"  失敗：{Environment.NewLine}{Indent(Tail(error, 30))}");
    failed |= error is not null;
    if (!keep)
    {
        DropDatabase(database);
    }
    else
    {
        Console.WriteLine($"  保留資料庫 {database}");
    }

    Console.WriteLine();
}

return Finish();

// ── 結束 ──────────────────────────────

int Finish()
{
    if (!keep)
    {
        Directory.Delete(workDir, true);
    }

    Console.WriteLine(failed ? "結果：有問題，修正後再部署。" : "結果：全部通過。");
    return failed ? 1 : 0;
}

// ── 靜態檢查 ──────────────────────────────

static List<string> ListMigrations(IEnumerable<string> fileNames) => fileNames
    .Where(f => Regex.IsMatch(f, @"^\d{14}_[^.]+\.cs$"))
    .Select(f => f[..^3])
    .Order(StringComparer.Ordinal)
    .ToList();

static IEnumerable<string> CheckUnwrappedSql(string path)
{
    var text = File.ReadAllText(path);
    var upStart = text.IndexOf("void Up(", StringComparison.Ordinal);
    var downStart = text.IndexOf("void Down(", StringComparison.Ordinal);
    if (upStart < 0)
    {
        yield break;
    }

    var upEnd = downStart > upStart ? downStart : text.Length;
    var name = Path.GetFileNameWithoutExtension(path);
    foreach (Match match in Regex.Matches(text[upStart..upEnd], @"migrationBuilder\s*\.\s*Sql\s*\("))
    {
        var start = upStart + match.Index + match.Length;
        var argument = ReadBalanced(text, start);
        if (IsWrapped(argument, text))
        {
            continue;
        }

        var line = text[..start].Count(c => c == '\n') + 1;
        var preview = Regex.Replace(argument, @"\s+", " ").Trim();
        yield return $"{MigrationsDir}/{name}.cs:{line}  migrationBuilder.Sql 沒有用 EXEC 包住：{(preview.Length > 80 ? preview[..80] + "…" : preview)}";
    }
}

// 讀到對應的右括號為止（略過字串內容）
static string ReadBalanced(string text, int start)
{
    var depth = 1;
    var i = start;
    while (i < text.Length && depth > 0)
    {
        var c = text[i];
        if (c == '"')
        {
            var verbatim = i > 0 && (text[i - 1] == '@' || (text[i - 1] == '$' && i > 1 && text[i - 2] == '@'));
            if (i + 2 < text.Length && text[i + 1] == '"' && text[i + 2] == '"')
            {
                var end = text.IndexOf("\"\"\"", i + 3, StringComparison.Ordinal);
                i = end < 0 ? text.Length : end + 3;
                continue;
            }

            i++;
            while (i < text.Length)
            {
                if (verbatim && text[i] == '"' && i + 1 < text.Length && text[i + 1] == '"')
                {
                    i += 2;
                    continue;
                }

                if (!verbatim && text[i] == '\\')
                {
                    i += 2;
                    continue;
                }

                if (text[i] == '"')
                {
                    break;
                }

                i++;
            }
        }
        else if (c == '(')
        {
            depth++;
        }
        else if (c == ')')
        {
            depth--;
        }

        i++;
    }

    return text[start..Math.Max(start, i - 1)];
}

// 參數本身含 EXEC(，或呼叫 Exec(...) 輔助方法，或是常數且常數內容以 EXEC 開頭
static bool IsWrapped(string argument, string fileText)
{
    if (Regex.IsMatch(argument, @"\bEXEC\s*\(", RegexOptions.IgnoreCase))
    {
        return true;
    }

    var identifier = Regex.Match(argument.Trim(), @"^[A-Za-z_]\w*$");
    if (!identifier.Success)
    {
        return false;
    }

    var declaration = Regex.Match(fileText, $@"\b{identifier.Value}\s*=\s*(@|\$|""|"""""")");
    return declaration.Success && Regex.IsMatch(fileText[declaration.Index..Math.Min(fileText.Length, declaration.Index + 200)], @"EXEC\s*\(", RegexOptions.IgnoreCase);
}

// ── 資料庫 ──────────────────────────────

string? CopyLocalDatabase(string database)
{
    var backupDir = SqlScalar("SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000))");
    var dataDir = SqlScalar("SELECT CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS nvarchar(4000))");
    if (backupDir is null || dataDir is null)
    {
        return "讀不到 SQL Server 的預設資料夾。";
    }

    var backup = Path.Combine(backupDir, $"{DbPrefix}copy.bak");
    var error = Sql($"BACKUP DATABASE [Involver] TO DISK = N'{backup}' WITH COPY_ONLY, INIT;");
    if (error is not null)
    {
        return error;
    }

    var files = SqlRows($"""
        DECLARE @files TABLE (LogicalName nvarchar(128), PhysicalName nvarchar(260), Type char(1), FileGroupName nvarchar(128), Size numeric(20,0), MaxSize numeric(20,0),
            FileId bigint, CreateLSN numeric(25,0), DropLSN numeric(25,0), UniqueId uniqueidentifier, ReadOnlyLSN numeric(25,0), ReadWriteLSN numeric(25,0),
            BackupSizeInBytes bigint, SourceBlockSize int, FileGroupId int, LogGroupGUID uniqueidentifier, DifferentialBaseLSN numeric(25,0),
            DifferentialBaseGUID uniqueidentifier, IsReadOnly bit, IsPresent bit, TDEThumbprint varbinary(32), SnapshotUrl nvarchar(360));
        INSERT INTO @files EXEC (N'RESTORE FILELISTONLY FROM DISK = N''{backup}''');
        SELECT LogicalName, Type FROM @files;
        """);
    var moves = files.Select(f => f.Split('\t'))
        .Select(c => $"MOVE N'{c[0]}' TO N'{Path.Combine(dataDir, database + (c[1] == "L" ? "_log.ldf" : ".mdf"))}'");
    return Sql($"RESTORE DATABASE [{database}] FROM DISK = N'{backup}' WITH REPLACE, {string.Join(", ", moves)};");
}

string? UpdateDatabase(string database, string migration)
{
    var connection = $"Server={server};Database={database};Trusted_Connection=True;TrustServerCertificate=True";
    var result = Run("dotnet", $"ef database update {migration} --project DataAccess --startup-project Involver --no-build --connection \"{connection}\"", root);
    return result.ExitCode == 0 ? null : result.Output;
}

void DropDatabase(string database) => Sql($"""
    IF DB_ID(N'{database}') IS NOT NULL
    BEGIN
        ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
        DROP DATABASE [{database}];
    END
    """);

// 和 deploy.yml 一樣，任何錯誤就停止（-b）；-x 關閉 $(變數) 取代，避免資料內容被誤認
string? RunScript(string database, string scriptPath)
{
    var result = Run("sqlcmd", $"-S \"{server}\" -E -d {database} -b -I -x -f 65001 -i \"{scriptPath}\"", root);
    return result.ExitCode == 0 ? null : result.Output;
}

string? Sql(string sql)
{
    var file = Path.Combine(workDir, $"q-{Guid.NewGuid():N}.sql");
    File.WriteAllText(file, sql, new UTF8Encoding(false));
    var result = Run("sqlcmd", $"-S \"{server}\" -E -b -I -x -f 65001 -i \"{file}\"", root);
    File.Delete(file);
    return result.ExitCode == 0 ? null : result.Output;
}

List<string> SqlRows(string sql)
{
    var file = Path.Combine(workDir, $"q-{Guid.NewGuid():N}.sql");
    File.WriteAllText(file, $"SET NOCOUNT ON;\n{sql}", new UTF8Encoding(false));
    var result = Run("sqlcmd", $"-S \"{server}\" -E -b -I -x -f 65001 -h -1 -W -s \"\t\" -i \"{file}\"", root);
    File.Delete(file);
    return result.Output.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Length > 0).ToList();
}

string? SqlScalar(string sql) => SqlRows(sql).FirstOrDefault();

// ── 共用 ──────────────────────────────

static string? FindRepoRoot()
{
    for (var dir = new DirectoryInfo(Environment.CurrentDirectory); dir is not null; dir = dir.Parent)
    {
        if (File.Exists(Path.Combine(dir.FullName, "Involver.sln")))
        {
            return dir.FullName;
        }
    }

    return null;
}

(int ExitCode, string Output) Git(string arguments) => Run("git", arguments, root!);

static (int ExitCode, string Output) Run(string fileName, string arguments, string workingDirectory)
{
    var info = new ProcessStartInfo(fileName, arguments)
    {
        WorkingDirectory = workingDirectory,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        StandardOutputEncoding = Encoding.UTF8,
        StandardErrorEncoding = Encoding.UTF8,
    };
    using var process = Process.Start(info)!;
    var stdout = process.StandardOutput.ReadToEndAsync();
    var stderr = process.StandardError.ReadToEndAsync();
    process.WaitForExit();
    return (process.ExitCode, stdout.Result + stderr.Result);
}

static string Tail(string text, int lines) => string.Join(Environment.NewLine, text.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Length > 0).TakeLast(lines));

static string Indent(string text) => string.Join(Environment.NewLine, text.Split(Environment.NewLine).Select(l => "    " + l));
