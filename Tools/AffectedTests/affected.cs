#:property TargetFramework=net10.0
#:property PublishAot=false

// 受影響測試挑選器：依目前分支的變更，找出最可能受影響的 NUnit 測試類別，產生只跑這些測試的 dotnet test 指令。
// 用來在本機先跑一輪快速、聚焦的測試，最後仍要跑完整的 dotnet test。
//
// 用法（在這個資料夾執行，需要 .NET 10 SDK 以上）：
//   dotnet run affected.cs                     列出變更檔案、挑中的測試類別與原因，印出 dotnet test 指令
//   dotnet run affected.cs -- --run            直接執行挑出來的 dotnet test 指令，回傳它的結束代碼
//   dotnet run affected.cs -- --list           只印出測試類別的完整名稱（一行一個）
//   dotnet run affected.cs -- --base <ref>     指定比較基準（預設是 HEAD 與 origin/master 的 merge-base，沒有 origin/master 就用 master）
//   dotnet run affected.cs -- --base <a> --head <b>   只看兩個提交之間的變更（不含工作目錄），用來檢查某個 PR 或提交影響哪些測試
//   dotnet run affected.cs -- --help           顯示說明
//
// 變更檔案 = 基準到 HEAD 的差異 + 尚未提交的修改 + 未追蹤的新檔案。
// 挑選規則：
//   1. InvolverTest 底下改到的測試檔 → 檔案裡宣告的測試類別。
//   2. 其他 .cs 檔 → 取出宣告的型別名稱；測試類別名稱是「型別名稱 + Tests」的排最前面，其次是測試裡用到這個型別名稱的。
//      頁面模型（IndexModel、DetailsModel…）這類重複的名稱，要測試同時參考到命名空間（using 或完整名稱）才算。
//   3. InvolverTest/TestData/<類別名稱>/ 底下的檔案 → 那個測試類別。
//   4. 改到 *.csproj、Directory.*.props、Program.cs、ApplicationDbContext、Migrations、InvolverTest/Infrastructure、.editorconfig
//      → 建議跑完整的 dotnet test（仍會列出聚焦的測試類別）。
//   5. cshtml、js、css 等非 C# 檔案沒有單元測試涵蓋，請改用瀏覽器確認。
// 這支工具只讀取 git 與原始碼，不會修改任何檔案。

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

Console.OutputEncoding = Encoding.UTF8;

// ── 參數 ──────────────────────────────

var run = false;
var listOnly = false;
string? baseRef = null;
string? headRef = null;
for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--run":
            run = true;
            break;
        case "--list":
            listOnly = true;
            break;
        case "--base":
            if (i + 1 >= args.Length)
            {
                Console.Error.WriteLine("--base 後面要接 git 參照，例如 --base master 或 --base 3f22996。");
                return 1;
            }
            baseRef = args[++i];
            break;
        case "--head":
            if (i + 1 >= args.Length)
            {
                Console.Error.WriteLine("--head 後面要接 git 參照，例如 --head 163bea8。");
                return 1;
            }
            headRef = args[++i];
            break;
        case "--help":
        case "-h":
        case "/?":
            PrintHelp();
            return 0;
        default:
            Console.Error.WriteLine($"不認得的參數「{args[i]}」，請用 --help 查看用法。");
            return 1;
    }
}

if (run && listOnly)
{
    Console.Error.WriteLine("--run 與 --list 不能同時使用。");
    return 1;
}

// ── 找出儲存庫根目錄 ──────────────────────────────

var root = FindRepoRoot(Directory.GetCurrentDirectory());
if (root is null)
{
    Console.Error.WriteLine("往上層找不到 Involver.sln，請在儲存庫裡面執行。");
    return 1;
}

// ── 變更檔案 ──────────────────────────────

string mergeBase;
if (baseRef is null)
{
    var defaultBase = Git(root, "rev-parse", "--verify", "--quiet", "origin/master").ExitCode == 0 ? "origin/master" : "master";
    var mb = Git(root, "merge-base", "HEAD", defaultBase);
    if (mb.ExitCode != 0)
    {
        Console.Error.WriteLine($"找不到 HEAD 與 {defaultBase} 的 merge-base：{mb.Error.Trim()}");
        return 1;
    }
    baseRef = defaultBase;
    mergeBase = mb.Output.Trim();
}
else
{
    var mb = Git(root, "rev-parse", "--verify", "--quiet", $"{baseRef}^{{commit}}");
    if (mb.ExitCode != 0)
    {
        Console.Error.WriteLine($"找不到 git 參照「{baseRef}」。");
        return 1;
    }
    mergeBase = mb.Output.Trim();
}

string? headCommit = null;
if (headRef is not null)
{
    var head = Git(root, "rev-parse", "--verify", "--quiet", $"{headRef}^{{commit}}");
    if (head.ExitCode != 0)
    {
        Console.Error.WriteLine($"找不到 git 參照「{headRef}」。");
        return 1;
    }
    headCommit = head.Output.Trim();
}

var diffCommands = headCommit is not null
    ? new[] { new[] { "diff", "--name-only", mergeBase, headCommit } }   // 只看兩個提交之間
    : new[]
    {
        new[] { "diff", "--name-only", mergeBase },                       // 基準到工作目錄（含已提交與未提交）
        new[] { "diff", "--name-only", "--cached", mergeBase },           // 已暫存
        new[] { "ls-files", "--others", "--exclude-standard" },           // 未追蹤
    };

var changed = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
foreach (var gitArgs in diffCommands)
{
    var result = Git(root, gitArgs);
    if (result.ExitCode != 0)
    {
        Console.Error.WriteLine($"git {string.Join(' ', gitArgs)} 失敗：{result.Error.Trim()}");
        return 1;
    }
    foreach (var line in result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        changed.Add(line.Replace('\\', '/'));
    }
}

// ── 掃描測試類別與方案中的型別 ──────────────────────────────

var testClasses = ScanTestClasses(root);
var typeIndex = ScanSolutionTypes(root);

// 頁面模型等常見的重複名稱；另外在方案中宣告超過一次的型別名稱也視為重複。
var ambiguousNames = new HashSet<string>(StringComparer.Ordinal)
{
    "IndexModel", "DetailsModel", "CreateModel", "EditModel", "DeleteModel", "ManageModel",
    "ListModel", "ErrorModel", "PrivacyModel", "LoginModel", "LogoutModel", "RegisterModel",
};
foreach (var (name, namespaces) in typeIndex)
{
    if (namespaces.Count > 1) ambiguousNames.Add(name);
}

// ── 分類變更檔案 ──────────────────────────────

var chosen = new Dictionary<string, Selection>(StringComparer.Ordinal);   // key = 完整類別名稱
var fullRunReasons = new List<string>();
var webAssets = new List<string>();
var otherFiles = new List<string>();
var unmatched = new List<string>();
var ignored = new List<string>();

void Choose(TestClass test, int rank, string reason)
{
    if (!chosen.TryGetValue(test.FullName, out var existing))
    {
        chosen[test.FullName] = new Selection(test, rank, [(rank, reason)]);
        return;
    }
    if (!existing.Reasons.Contains((rank, reason))) existing.Reasons.Add((rank, reason));
    if (rank < existing.Rank) chosen[test.FullName] = existing with { Rank = rank };
}

foreach (var path in changed)
{
    var fullRunReason = FullRunReason(path);
    if (fullRunReason is not null)
    {
        fullRunReasons.Add($"{path}（{fullRunReason}）");
    }

    // TestData/<類別名稱>/...
    var testDataMatch = Regex.Match(path, @"^InvolverTest/TestData/([^/]+)/", RegexOptions.IgnoreCase);
    if (testDataMatch.Success)
    {
        var className = testDataMatch.Groups[1].Value;
        var owners = testClasses.Where(t => t.Name.Equals(className, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var owner in owners) Choose(owner, 0, $"測試資料 {path}");
        if (owners.Count == 0) unmatched.Add(path);
        continue;
    }

    if (path.StartsWith("Tools/", StringComparison.OrdinalIgnoreCase))
    {
        ignored.Add(path);
        continue;
    }

    if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
    {
        if (fullRunReason is not null) continue;
        if (IsWebAsset(path)) webAssets.Add(path);
        else otherFiles.Add(path);
        continue;
    }

    // 測試檔本身
    if (path.StartsWith("InvolverTest/", StringComparison.OrdinalIgnoreCase))
    {
        var declared = testClasses.Where(t => t.Path.Equals(path, StringComparison.OrdinalIgnoreCase)).ToList();
        if (declared.Count > 0)
        {
            foreach (var test in declared) Choose(test, 0, $"測試檔有修改 {path}");
            continue;
        }
        // 不是測試類別的檔案（例如共用的輔助類別）就當一般型別處理。
    }

    var source = ReadSource(root, path, mergeBase, headCommit);
    if (source is null)
    {
        unmatched.Add(path);
        continue;
    }

    var ns = ParseNamespace(source);
    var types = ParseTypeNames(source);
    var matchedAny = false;
    foreach (var type in types)
    {
        foreach (var test in testClasses)
        {
            if (test.Name.Equals($"{type}Tests", StringComparison.Ordinal))
            {
                Choose(test, 1, $"同名測試：{type} ← {path}");
                matchedAny = true;
                continue;
            }
            if (test.Path.Equals(path, StringComparison.OrdinalIgnoreCase)) continue;

            var references = ambiguousNames.Contains(type)
                ? ReferencesQualified(test.Source, ns, type)
                : Regex.IsMatch(test.Source, $@"\b{Regex.Escape(type)}\b");
            if (references)
            {
                var label = ambiguousNames.Contains(type) && ns is not null ? $"{ns}.{type}" : type;
                Choose(test, 2, $"測試用到 {label} ← {path}");
                matchedAny = true;
            }
        }
    }
    if (!matchedAny && fullRunReason is null) unmatched.Add(path);
}

var selections = chosen.Values
    .OrderBy(s => s.Rank)
    .ThenBy(s => s.Test.FullName, StringComparer.Ordinal)
    .ToList();

// ── 輸出 ──────────────────────────────

var filter = string.Join('|', selections.Select(s => $"FullyQualifiedName~{s.Test.FullName}."));
var testArgs = new List<string> { "test", "InvolverTest" };
if (selections.Count > 0)
{
    testArgs.Add("--filter");
    testArgs.Add(filter);
}
var command = selections.Count > 0
    ? $"dotnet test InvolverTest --filter \"{filter}\""
    : "dotnet test InvolverTest";

if (listOnly)
{
    foreach (var s in selections) Console.WriteLine(s.Test.FullName);
    if (fullRunReasons.Count > 0) Console.Error.WriteLine("提醒：改到共用設定或基礎設施，建議跑完整的 dotnet test。");
    return 0;
}

Console.WriteLine($"比較基準：{baseRef}（{mergeBase[..10]}）→ {(headCommit is null ? "工作目錄" : $"{headRef}（{headCommit[..10]}）")}");
Console.WriteLine($"變更檔案：{changed.Count} 個");
foreach (var path in changed) Console.WriteLine($"  {path}");
Console.WriteLine();

if (changed.Count == 0)
{
    Console.WriteLine("沒有任何變更，沒有受影響的測試。");
    return 0;
}

if (selections.Count > 0)
{
    Console.WriteLine($"受影響的測試類別：{selections.Count} 個");
    foreach (var s in selections)
    {
        var tag = s.Rank switch { 0 => "直接", 1 => "同名", _ => "參考" };
        Console.WriteLine($"  [{tag}] {s.Test.FullName}");
        foreach (var (_, reason) in s.Reasons.OrderBy(r => r.Rank).Take(3)) Console.WriteLine($"         {reason}");
        if (s.Reasons.Count > 3) Console.WriteLine($"         …另外還有 {s.Reasons.Count - 3} 個原因");
    }
    Console.WriteLine();
}
else
{
    Console.WriteLine("沒有找到受影響的測試類別。");
    Console.WriteLine();
}

if (unmatched.Count > 0)
{
    Console.WriteLine("沒有對應測試的 C# 檔案（可能需要補測試）：");
    foreach (var path in unmatched) Console.WriteLine($"  {path}");
    Console.WriteLine();
}

if (webAssets.Count > 0)
{
    Console.WriteLine("頁面、樣式與前端檔案沒有單元測試涵蓋，請改用瀏覽器確認（淺色、深色模式與 375 px 手機寬度）：");
    foreach (var path in webAssets) Console.WriteLine($"  {path}");
    Console.WriteLine();
}

if (otherFiles.Count > 0)
{
    Console.WriteLine("其他檔案（文件、設定等）不對應單元測試：");
    foreach (var path in otherFiles) Console.WriteLine($"  {path}");
    Console.WriteLine();
}

if (ignored.Count > 0)
{
    Console.WriteLine("開發工具（Tools/）不在測試範圍：");
    foreach (var path in ignored) Console.WriteLine($"  {path}");
    Console.WriteLine();
}

if (fullRunReasons.Count > 0)
{
    Console.WriteLine("改到共用設定或基礎設施，建議跑完整的測試：dotnet test");
    foreach (var reason in fullRunReasons) Console.WriteLine($"  {reason}");
    Console.WriteLine();
}

if (selections.Count == 0)
{
    if (fullRunReasons.Count == 0) Console.WriteLine("沒有受影響的單元測試，不需要跑聚焦測試。");
    return 0;
}

Console.WriteLine("聚焦測試指令（在儲存庫根目錄執行）：");
Console.WriteLine($"  {command}");

if (!run) return 0;

Console.WriteLine();
Console.WriteLine("執行中…");
var psi = new ProcessStartInfo("dotnet") { WorkingDirectory = root, UseShellExecute = false };
foreach (var a in testArgs) psi.ArgumentList.Add(a);
using (var process = Process.Start(psi)!)
{
    process.WaitForExit();
    if (fullRunReasons.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("提醒：這次改到共用設定或基礎設施，請再跑一次完整的 dotnet test。");
    }
    return process.ExitCode;
}

// ── 函式 ──────────────────────────────

static void PrintHelp()
{
    Console.WriteLine("""
        受影響測試挑選器：依目前分支的變更挑出最可能受影響的 NUnit 測試類別。

        用法（在 Tools/AffectedTests 資料夾執行）：
          dotnet run affected.cs                     列出變更、挑中的測試類別與原因，印出 dotnet test 指令
          dotnet run affected.cs -- --run            直接執行挑出來的 dotnet test 指令
          dotnet run affected.cs -- --list           只印出測試類別的完整名稱
          dotnet run affected.cs -- --base <ref>     指定比較基準（預設：HEAD 與 origin/master 的 merge-base）
          dotnet run affected.cs -- --base <a> --head <b>   只看兩個提交之間的變更（不含工作目錄）
          dotnet run affected.cs -- --help           顯示這段說明

        聚焦測試只是快速確認，送出前仍要跑完整的 dotnet test。
        """);
}

static string? FindRepoRoot(string start)
{
    for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
    {
        if (File.Exists(Path.Combine(dir.FullName, "Involver.sln"))) return dir.FullName;
    }
    return null;
}

static (int ExitCode, string Output, string Error) Git(string workingDirectory, params string[] gitArgs)
{
    var psi = new ProcessStartInfo("git")
    {
        WorkingDirectory = workingDirectory,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        StandardOutputEncoding = Encoding.UTF8,
        StandardErrorEncoding = Encoding.UTF8,
    };
    psi.ArgumentList.Add("-c");
    psi.ArgumentList.Add("core.quotepath=false");   // 中文檔名不要被跳脫成 \xxx
    foreach (var a in gitArgs) psi.ArgumentList.Add(a);
    using var process = Process.Start(psi)!;
    var errorTask = process.StandardError.ReadToEndAsync();
    var output = process.StandardOutput.ReadToEnd();
    process.WaitForExit();
    return (process.ExitCode, output, errorTask.Result);
}

static bool IsWebAsset(string path)
{
    var extension = Path.GetExtension(path).ToLowerInvariant();
    return extension is ".cshtml" or ".razor" or ".js" or ".mjs" or ".css" or ".scss" or ".html" or ".htm"
            or ".png" or ".jpg" or ".jpeg" or ".gif" or ".svg" or ".webp" or ".ico" or ".woff" or ".woff2"
        || path.Contains("/wwwroot/", StringComparison.OrdinalIgnoreCase)
        || Path.GetFileName(path).Equals("libman.json", StringComparison.OrdinalIgnoreCase);
}

static string? FullRunReason(string path)
{
    var fileName = Path.GetFileName(path);
    if (fileName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)) return "專案檔";
    if (fileName.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase)) return "方案檔";
    if (Regex.IsMatch(fileName, @"^Directory\.[^/]+\.(props|targets)$", RegexOptions.IgnoreCase)) return "共用建置設定";
    if (fileName.Equals("global.json", StringComparison.OrdinalIgnoreCase)) return "SDK 設定";
    if (fileName.Equals(".editorconfig", StringComparison.OrdinalIgnoreCase)) return "程式碼風格與分析器設定";
    if (fileName.Equals("Program.cs", StringComparison.OrdinalIgnoreCase)) return "啟動與相依性注入設定";
    if (Regex.IsMatch(path, @"^DataAccess/Data/ApplicationDbContext[^/]*\.cs$", RegexOptions.IgnoreCase)) return "資料庫結構";
    if (path.StartsWith("DataAccess/Migrations/", StringComparison.OrdinalIgnoreCase)) return "資料庫 migration";
    if (path.StartsWith("InvolverTest/Infrastructure/", StringComparison.OrdinalIgnoreCase)) return "共用的測試基礎設施";
    return null;
}

static IEnumerable<string> EnumerateSourceFiles(string directory)
{
    if (!Directory.Exists(directory)) yield break;
    var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true };
    foreach (var file in Directory.EnumerateFiles(directory, "*.cs", options))
    {
        var normalized = file.Replace('\\', '/');
        if (normalized.Contains("/bin/") || normalized.Contains("/obj/")) continue;
        yield return file;
    }
}

static string RelativePath(string root, string file) => Path.GetRelativePath(root, file).Replace('\\', '/');

/// 去掉註解與字串內容，避免註解裡的文字被當成型別宣告或參考。
static string StripCommentsAndStrings(string source)
{
    var withoutBlock = Regex.Replace(source, @"/\*.*?\*/", " ", RegexOptions.Singleline);
    var withoutLine = Regex.Replace(withoutBlock, @"//[^\n]*", " ");
    return Regex.Replace(withoutLine, @"@?""(?:[^""\\\n]|\\.)*""", "\"\"");
}

static string? ParseNamespace(string source)
{
    var match = Regex.Match(StripCommentsAndStrings(source), @"^\s*namespace\s+([\w.]+)", RegexOptions.Multiline);
    return match.Success ? match.Groups[1].Value : null;
}

static List<string> ParseTypeNames(string source)
{
    var code = StripCommentsAndStrings(source);
    var pattern = @"^[ \t]*(?:\[[^\]\n]*\][ \t]*)*(?:(?:public|internal|private|protected|static|sealed|abstract|partial|readonly|ref|unsafe|file|new)\s+)*(?:class|struct|interface|enum|record(?:\s+(?:class|struct))?)\s+([A-Za-z_]\w*)";
    return Regex.Matches(code, pattern, RegexOptions.Multiline)
        .Select(m => m.Groups[1].Value)
        .Distinct(StringComparer.Ordinal)
        .ToList();
}

/// 重複的型別名稱：測試要用完整名稱，或 using 該命名空間後再用到型別名稱，才算參考。
static bool ReferencesQualified(string testSource, string? ns, string type)
{
    if (ns is null) return false;
    var escapedNs = Regex.Escape(ns);
    var escapedType = Regex.Escape(type);
    if (Regex.IsMatch(testSource, $@"\b{escapedNs}\.{escapedType}\b")) return true;
    return Regex.IsMatch(testSource, $@"^\s*using\s+{escapedNs}\s*;", RegexOptions.Multiline)
        && Regex.IsMatch(testSource, $@"(?<![\w.]){escapedType}\b");
}

static string? ReadSource(string root, string path, string mergeBase, string? headCommit)
{
    if (headCommit is not null)
    {
        var atHead = Git(root, "show", $"{headCommit}:{path}");
        if (atHead.ExitCode == 0) return atHead.Output;
    }
    else
    {
        var full = Path.Combine(root, path);
        if (File.Exists(full)) return File.ReadAllText(full);
    }
    // 已刪除的檔案：從比較基準讀回原本的內容，找出原本宣告的型別。
    var shown = Git(root, "show", $"{mergeBase}:{path}");
    return shown.ExitCode == 0 ? shown.Output : null;
}

static List<TestClass> ScanTestClasses(string root)
{
    var result = new List<TestClass>();
    foreach (var file in EnumerateSourceFiles(Path.Combine(root, "InvolverTest")))
    {
        var rel = RelativePath(root, file);
        if (rel.StartsWith("InvolverTest/Infrastructure/", StringComparison.OrdinalIgnoreCase)) continue;
        var raw = File.ReadAllText(file);
        var code = StripCommentsAndStrings(raw);
        if (!Regex.IsMatch(code, @"\[(?:TestFixture|Test|TestCase|TestCaseSource|Theory)\b")) continue;
        var ns = ParseNamespace(raw);
        foreach (Match m in Regex.Matches(code, @"^[ \t]*(?:\[[^\]\n]*\][ \t]*)*(?:(?:public|internal|static|sealed|abstract|partial)\s+)*class\s+([A-Za-z_]\w*)", RegexOptions.Multiline))
        {
            var name = m.Groups[1].Value;
            var fullName = ns is null ? name : $"{ns}.{name}";
            result.Add(new TestClass(name, fullName, rel, code));
        }
    }
    return result;
}

static Dictionary<string, HashSet<string>> ScanSolutionTypes(string root)
{
    var index = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
    foreach (var project in new[] { "Involver", "DataAccess", "InvolverRoutineWork" })
    {
        foreach (var file in EnumerateSourceFiles(Path.Combine(root, project)))
        {
            var rel = RelativePath(root, file);
            if (rel.StartsWith("DataAccess/Migrations/", StringComparison.OrdinalIgnoreCase)) continue;
            var source = File.ReadAllText(file);
            var ns = ParseNamespace(source) ?? string.Empty;
            foreach (var type in ParseTypeNames(source))
            {
                if (!index.TryGetValue(type, out var set)) index[type] = set = new HashSet<string>(StringComparer.Ordinal);
                set.Add(ns);
            }
        }
    }
    return index;
}

record TestClass(string Name, string FullName, string Path, string Source);

record Selection(TestClass Test, int Rank, List<(int Rank, string Text)> Reasons);
