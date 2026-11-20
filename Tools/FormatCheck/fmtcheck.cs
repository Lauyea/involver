#:property TargetFramework=net10.0
#:property PublishAot=false

// C# 格式與命名檢查：只看這個分支改到的 C# 行，用 dotnet format 檢查空白格式（WHITESPACE）與命名規則（IDE1006，規則在 .editorconfig）。
// 舊程式碼有不少既有的格式問題，所以只回報改到的那幾行，不會要求整份檔案重排。
//
// 用法（在儲存庫任一位置執行，需要 .NET 10 SDK）：
//   dotnet run fmtcheck.cs                    檢查目前分支（相對於 origin/master 的 merge-base，含未提交與未追蹤的檔案）
//   dotnet run fmtcheck.cs -- --base <ref>    指定比較基準（CI 用 origin/master）
//   dotnet run fmtcheck.cs -- --help          顯示說明
//
// 不檢查：DataAccess/Migrations（EF 產生）、*.Designer.cs、Tools/（單檔程式不在方案裡）。
// 檔尾換行（FINALNEWLINE）不檢查：既有檔案有一半有、一半沒有，.editorconfig 的設定和實際不一致。
// 結束代碼：0 通過；1 有問題；2 用法錯誤或 dotnet format 無法執行。

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;


Console.OutputEncoding = Encoding.UTF8;

if (args.Contains("--help") || args.Contains("-h"))
{
    Console.WriteLine("用法：dotnet run fmtcheck.cs -- [--base <ref>]");
    return 0;
}

var root = FindRepoRoot();
if (root is null)
{
    Console.Error.WriteLine("找不到 Involver.sln，請在儲存庫內執行。");
    return 2;
}

var baseIndex = Array.IndexOf(args, "--base");
var baseRef = baseIndex >= 0 && baseIndex + 1 < args.Length
    ? args[baseIndex + 1]
    : Run("git", "rev-parse --verify --quiet origin/master").ExitCode == 0 ? "origin/master" : "master";
var mergeBase = Run("git", $"merge-base HEAD {baseRef}").Output.Trim();
if (mergeBase.Length == 0)
{
    Console.Error.WriteLine($"找不到 HEAD 與 {baseRef} 的 merge-base。");
    return 2;
}

// ── 改到的行 ──────────────────────────────

var changedLines = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);
string? current = null;
foreach (var line in Run("git", $"diff -U0 --no-color --diff-filter=AMR {mergeBase} -- *.cs").Output.Split('\n'))
{
    if (line.StartsWith("+++ "))
    {
        current = line.StartsWith("+++ b/") ? line[6..].Trim() : null;
        if (current is not null)
        {
            changedLines.TryAdd(current, []);
        }
    }
    else if (current is not null && line.StartsWith("@@"))
    {
        var hunk = Regex.Match(line, @"\+(\d+)(?:,(\d+))?");
        var start = int.Parse(hunk.Groups[1].Value);
        var count = hunk.Groups[2].Success ? int.Parse(hunk.Groups[2].Value) : 1;
        for (var i = 0; i < count; i++)
        {
            changedLines[current].Add(start + i);
        }
    }
}

foreach (var file in Run("git", "ls-files --others --exclude-standard -- *.cs").Output.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0))
{
    changedLines[file] = [.. Enumerable.Range(1, File.ReadAllLines(Path.Combine(root, file)).Length)];
}

var targets = changedLines
    .Where(kv => kv.Value.Count > 0)
    .Where(kv => !kv.Key.StartsWith("Tools/", StringComparison.OrdinalIgnoreCase)
        && !kv.Key.Contains("/Migrations/", StringComparison.OrdinalIgnoreCase)
        && !kv.Key.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
    .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

Console.WriteLine($"比較基準：{baseRef}（{mergeBase[..10]}）");
if (targets.Count == 0)
{
    Console.WriteLine("沒有改到需要檢查的 C# 檔案。");
    return 0;
}

Console.WriteLine($"檢查 {targets.Count} 個 C# 檔案改到的行");

// ── dotnet format ──────────────────────────────

var findings = new List<string>();
foreach (var group in targets.Keys.GroupBy(f => f.Split('/')[0]))
{
    var project = Path.Combine(root, group.Key, $"{group.Key}.csproj");
    if (!File.Exists(project))
    {
        continue;
    }

    // --include 的路徑是相對於目前目錄（儲存庫根目錄），不是相對於專案
    var include = string.Join(' ', group.Select(f => $"\"{f}\""));
    foreach (var mode in new[] { "whitespace", "style --severity info --diagnostics IDE1006" })
    {
        var result = Run("dotnet", $"format \"{project}\" {mode} --verify-no-changes --no-restore --include {include}");
        foreach (var line in result.Output.Split('\n').Select(l => l.Trim()))
        {
            var match = Regex.Match(line, @"^(?<file>.+?\.cs)\((?<line>\d+),(?<col>\d+)\): (?:error|warning|info) (?<id>[A-Z0-9]+): (?<message>.*?)(?: \[.*\])?$");
            if (!match.Success || match.Groups["id"].Value == "FINALNEWLINE")
            {
                continue;
            }

            var relative = Path.GetRelativePath(root, match.Groups["file"].Value).Replace('\\', '/');
            var lineNumber = int.Parse(match.Groups["line"].Value);
            if (targets.TryGetValue(relative, out var lines) && lines.Contains(lineNumber))
            {
                findings.Add($"{relative}:{lineNumber}  {match.Groups["id"].Value}  {match.Groups["message"].Value}");
            }
        }

        if (result.ExitCode != 0 && !result.Output.Contains(".cs("))
        {
            Console.Error.WriteLine($"dotnet format 執行失敗（{group.Key}）：{Environment.NewLine}{result.Output}");
            return 2;
        }
    }
}

foreach (var finding in findings.Distinct()
    .OrderBy(f => f[..f.IndexOf(':')], StringComparer.Ordinal)
    .ThenBy(f => int.Parse(f[(f.IndexOf(':') + 1)..f.IndexOf(' ')])))
{
    Console.WriteLine(finding);
}

Console.WriteLine(findings.Count == 0
    ? "通過：改到的行沒有格式或命名問題。"
    : $"共 {findings.Distinct().Count()} 個問題。WHITESPACE 可以用 IDE 的格式化文件修正；IDE1006 是命名規則（私有欄位 _camelCase、非同步方法以 Async 結尾等，見 .editorconfig）。");
return findings.Count == 0 ? 0 : 1;

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

(int ExitCode, string Output) Run(string fileName, string arguments)
{
    var info = new ProcessStartInfo(fileName, arguments)
    {
        WorkingDirectory = root ?? Environment.CurrentDirectory,
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
