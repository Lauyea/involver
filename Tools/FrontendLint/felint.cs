#:property TargetFramework=net10.0

// 前端規範檢查（AGENTS.md「前端開發規範」）：在開 PR 前檢查 wwwroot 的 CSS 與 JS 有沒有違反常被忽略的規則。
// 不用 eslint/stylelint/npm，只用 .NET 內建的 Regex 與簡單的 tokenizer，不會修改任何檔案。
//
// 用法（在這個資料夾執行，需要 .NET 10 SDK 以上）：
//   dotnet run felint.cs                       只檢查這個分支改到的行（相對於 origin/master 的 merge-base，含未提交與未追蹤的檔案）
//   dotnet run felint.cs -- --all              檢查所有檔案的所有行
//   dotnet run felint.cs -- --rule js-no-var   只跑一條規則（可以和 --all 一起用）
//   dotnet run felint.cs -- --self-test        用內建的範例驗證規則本身有沒有寫對
//   dotnet run felint.cs -- --help             顯示說明與規則清單
//
// 檢查範圍：Involver/wwwroot/css/**/*.css、Involver/wwwroot/js/**/*.js（略過 *.min.*，不檢查 wwwroot/lib 的第三方套件）。
// 規則：
//   css-hardcoded-color     一般屬性寫死顏色（#hex、rgb()/rgba()/hsl()/hsla() 帶數字）；自訂屬性 --* 裡定義顏色沒關係
//   css-property-order      同一個區塊的屬性要依字母排序（忽略 -webkit- 等前綴，--* 不檢查）
//   js-no-var               不要用 var
//   js-double-quote         字串用單引號（字串本身含單引號時可以用雙引號）
//   js-loose-equality       用 === / !==，不要用 == / !=
//   js-global-vue           不要用全域 Vue（window.Vue、Vue.createApp、const { … } = Vue）
//   js-import-map           import 套件名稱（非 ./、../、/ 開頭）必須在 import map（Involver/Helpers/ImportMapHelper.cs）裡
//   importmap-missing-file  import map 指到的檔案要存在於 wwwroot（不論是否只檢查變更都會報）
// 輸出：每個問題一行「路徑:行號  規則  訊息」，最後是各規則的數量。有問題時結束代碼為 1，沒有為 0，參數錯誤為 2。

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

Console.OutputEncoding = Encoding.UTF8;

var all = false;
string? onlyRule = null;
var selfTest = false;
for (var a = 0; a < args.Length; a++)
{
    switch (args[a])
    {
        case "--all":
            all = true;
            break;
        case "--rule":
            if (a + 1 >= args.Length)
            {
                Console.Error.WriteLine("--rule 後面要接規則名稱。");
                Linter.PrintHelp(Console.Error);
                return 2;
            }
            onlyRule = args[++a];
            if (!Linter.Rules.Contains(onlyRule))
            {
                Console.Error.WriteLine($"沒有「{onlyRule}」這條規則。");
                Linter.PrintHelp(Console.Error);
                return 2;
            }
            break;
        case "--self-test":
            selfTest = true;
            break;
        case "--help" or "-h":
            Linter.PrintHelp(Console.Out);
            return 0;
        default:
            Console.Error.WriteLine($"不認得的參數「{args[a]}」。");
            Linter.PrintHelp(Console.Error);
            return 2;
    }
}

if (selfTest)
{
    return Linter.RunSelfTest();
}

// ── 找專案根目錄 ──────────────────────────────

var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Involver.sln")))
{
    dir = dir.Parent;
}
if (dir is null)
{
    Console.Error.WriteLine("找不到 Involver.sln，請在專案資料夾內執行。");
    return 2;
}

var root = dir.FullName;
var wwwroot = Path.Combine(root, "Involver", "wwwroot");
const string ImportMapRelPath = "Involver/Helpers/ImportMapHelper.cs";
var importMapFile = Path.Combine(root, ImportMapRelPath);
var importMap = File.Exists(importMapFile)
    ? Linter.ParseImportMap(File.ReadAllText(importMapFile))
    : [];
if (importMap.Count == 0)
{
    Console.Error.WriteLine($"警告：讀不到 {ImportMapRelPath} 的 import map，js-import-map 會把所有套件名稱都當成錯誤。");
}

var files = Linter.FindFiles(wwwroot)
    .Select(f => Path.GetRelativePath(root, f).Replace('\\', '/'))
    .OrderBy(f => f, StringComparer.Ordinal)
    .ToList();

// ── 變更的行 ──────────────────────────────

Dictionary<string, HashSet<int>?>? changed = null;
var modeNote = "檢查所有檔案的所有行（--all）";
if (!all)
{
    var (map, note, error) = Linter.GetChangedLines(root);
    if (map is null)
    {
        Console.Error.WriteLine(error);
        return 2;
    }
    changed = map;
    modeNote = note;
}

// ── 檢查 ──────────────────────────────

var findings = new List<Finding>();
var scanned = 0;
foreach (var rel in files)
{
    HashSet<int>? lines = null;
    if (changed is not null && !changed.TryGetValue(rel, out lines))
    {
        continue;
    }

    scanned++;
    var text = File.ReadAllText(Path.Combine(root, rel));
    var fileFindings = rel.EndsWith(".css", StringComparison.OrdinalIgnoreCase)
        ? Linter.LintCss(rel, text)
        : Linter.LintJs(rel, text, importMap);
    findings.AddRange(fileFindings.Where(f =>
        (onlyRule is null || f.Rule == onlyRule) && Linter.IsOnChangedLines(f, lines)));
}

if (onlyRule is null or "importmap-missing-file")
{
    findings.AddRange(Linter.CheckImportMapFiles(importMap, wwwroot, ImportMapRelPath));
}

// ── 輸出 ──────────────────────────────

foreach (var f in findings.OrderBy(f => f.Path, StringComparer.Ordinal).ThenBy(f => f.Line).ThenBy(f => f.Rule, StringComparer.Ordinal))
{
    Console.WriteLine($"{f.Path}:{f.Line}  {f.Rule}  {f.Message}");
}

if (findings.Count > 0)
{
    Console.WriteLine();
}
Console.WriteLine($"{modeNote}，檢查了 {scanned} 個檔案。");
if (findings.Count == 0)
{
    Console.WriteLine("沒有發現問題。");
    return 0;
}

Console.WriteLine($"共 {findings.Count} 個問題：");
foreach (var group in findings.GroupBy(f => f.Rule).OrderBy(g => Array.IndexOf(Linter.Rules, g.Key)))
{
    Console.WriteLine($"  {group.Key,-24}{group.Count()}");
}
return 1;

// ── 型別 ──────────────────────────────

/// <summary>一個檢查結果。</summary>
/// <param name="Path">相對於專案根目錄的路徑（/ 分隔）</param>
/// <param name="Line">行號（從 1 開始）</param>
/// <param name="Rule">規則名稱</param>
/// <param name="Message">說明</param>
/// <param name="BlockLines">整個區塊涵蓋的行（css-property-order 用：區塊中任何一行有變更就回報）</param>
record Finding(string Path, int Line, string Rule, string Message, IReadOnlyCollection<int>? BlockLines = null);

/// <summary>import map 的一筆對應。</summary>
/// <param name="Specifier">import 的名稱</param>
/// <param name="Target">對應到的路徑</param>
/// <param name="Line">在 ImportMapHelper.cs 的行號</param>
record ImportMapEntry(string Specifier, string Target, int Line);

/// <summary>JS token 的種類。</summary>
enum TokKind { Ident, Number, Punct, String, Template, Regex, Comment }

/// <summary>JS token。</summary>
/// <param name="Kind">種類</param>
/// <param name="Start">起始位置</param>
/// <param name="Text">原始文字</param>
/// <param name="Quote">字串的引號（只有 String 有）</param>
record Token(TokKind Kind, int Start, string Text, char Quote = '\0');

/// <summary>CSS 宣告（property: value）。</summary>
/// <param name="Property">屬性名稱</param>
/// <param name="PropOffset">屬性名稱的位置</param>
/// <param name="ValueOffset">值的起始位置</param>
/// <param name="EndOffset">宣告結束的位置</param>
/// <param name="Value">值（註解已換成空白）</param>
record CssDecl(string Property, int PropOffset, int ValueOffset, int EndOffset, string Value);

/// <summary>把字元位置換成行號。</summary>
sealed class LineIndex
{
    private readonly List<int> _starts = [0];

    /// <summary>建立行號索引。</summary>
    /// <param name="text">檔案內容</param>
    public LineIndex(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                _starts.Add(i + 1);
            }
        }
    }

    /// <summary>取得位置所在的行號（從 1 開始）。</summary>
    /// <param name="offset">字元位置</param>
    /// <returns>行號</returns>
    public int LineOf(int offset)
    {
        var index = _starts.BinarySearch(offset);
        return index >= 0 ? index + 1 : ~index;
    }
}

/// <summary>前端規範檢查的規則實作。</summary>
static class Linter
{
    /// <summary>所有規則名稱（輸出摘要依這個順序）。</summary>
    public static readonly string[] Rules =
    [
        "css-hardcoded-color",
        "css-property-order",
        "js-no-var",
        "js-double-quote",
        "js-loose-equality",
        "js-global-vue",
        "js-import-map",
        "importmap-missing-file",
    ];

    private static readonly Regex HexColor = new(@"(?<![\w-])#(?:[0-9a-fA-F]{8}|[0-9a-fA-F]{6}|[0-9a-fA-F]{3,4})(?![\w-])", RegexOptions.Compiled);
    private static readonly Regex FunctionColor = new(@"(?<![\w-])(?:rgba?|hsla?)\(\s*[-+]?\.?\d[^)]*\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PropertyName = new(@"^(?:--[\w-]+|-?[A-Za-z][\w-]*)$", RegexOptions.Compiled);
    private static readonly Regex VendorPrefix = new(@"^-(?:webkit|moz|ms|o)-", RegexOptions.Compiled);
    private static readonly Regex UrlOrString = new(@"url\([^)]*\)|""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*'", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex Hunk = new(@"^@@ -\d+(?:,\d+)? \+(\d+)(?:,(\d+))? @@", RegexOptions.Compiled);
    private static readonly Regex ImportMapLine = new(@"\[\s*""([^""]+)""\s*\]\s*=\s*""([^""]+)""", RegexOptions.Compiled);

    private static readonly string[] Puncts =
    [
        ">>>=", "...", "===", "!==", "**=", "<<=", ">>=", ">>>", "&&=", "||=", "??=",
        "=>", "==", "!=", "<=", ">=", "&&", "||", "??", "?.", "++", "--",
        "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "**", "<<", ">>",
    ];

    // 這些關鍵字後面的 / 是正規表示式的開頭，不是除號
    private static readonly HashSet<string> RegexKeywords =
    [
        "return", "typeof", "instanceof", "in", "of", "new", "delete", "void", "throw",
        "case", "do", "else", "yield", "await",
    ];

    /// <summary>顯示說明。</summary>
    /// <param name="writer">輸出的位置</param>
    public static void PrintHelp(TextWriter writer)
    {
        writer.WriteLine("""
            前端規範檢查（AGENTS.md「前端開發規範」）

            用法（在 Tools/FrontendLint 執行）：
              dotnet run felint.cs                       只檢查這個分支改到的行（相對於 origin/master 的 merge-base，含未提交與未追蹤的檔案）
              dotnet run felint.cs -- --all              檢查所有檔案的所有行
              dotnet run felint.cs -- --rule <規則>      只跑一條規則（可以和 --all 一起用）
              dotnet run felint.cs -- --self-test        用內建的範例驗證規則本身
              dotnet run felint.cs -- --help             顯示這段說明

            規則：
              css-hardcoded-color     一般屬性寫死顏色，請改用 var(--primary-accent) 等主題變數
              css-property-order      同一個區塊的屬性要依字母排序（忽略 -webkit- 等前綴，--* 不檢查）
              js-no-var               不要用 var
              js-double-quote         字串用單引號（字串本身含單引號時可以用雙引號）
              js-loose-equality       用 === / !==，不要用 == / !=
              js-global-vue           不要用全域 Vue，請 import { createApp } from 'vue'
              js-import-map           import 的套件名稱必須在 Involver/Helpers/ImportMapHelper.cs 的 import map 裡
              importmap-missing-file  import map 指到的檔案要存在於 wwwroot

            結束代碼：有問題為 1，沒有為 0，參數錯誤為 2。
            """);
    }

    // ── 檔案與 git ──────────────────────────────

    /// <summary>列出要檢查的 CSS 與 JS 檔（略過 *.min.* 與 wwwroot/lib）。</summary>
    /// <param name="wwwroot">wwwroot 的完整路徑</param>
    /// <returns>檔案的完整路徑</returns>
    public static IEnumerable<string> FindFiles(string wwwroot)
    {
        foreach (var (folder, ext) in new[] { ("css", ".css"), ("js", ".js") })
        {
            var path = Path.Combine(wwwroot, folder);
            if (!Directory.Exists(path))
            {
                continue;
            }
            foreach (var file in Directory.EnumerateFiles(path, $"*{ext}", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(file);
                if (name.EndsWith($".min{ext}", StringComparison.OrdinalIgnoreCase)
                    || !name.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                yield return file;
            }
        }
    }

    /// <summary>
    /// 取得變更的行：merge-base 到工作目錄的差異（含已提交、已暫存、未暫存），加上未追蹤的檔案（整個檔案都算）。
    /// </summary>
    /// <param name="root">專案根目錄</param>
    /// <returns>路徑 → 變更的行（null 代表整個檔案）；說明文字；錯誤訊息</returns>
    public static (Dictionary<string, HashSet<int>?>? Map, string Note, string Error) GetChangedLines(string root)
    {
        string[] scope = ["--", "Involver/wwwroot/css", "Involver/wwwroot/js"];

        var baseName = "origin/master";
        var mergeBase = Git(root, "merge-base", "HEAD", baseName);
        if (mergeBase.Code != 0)
        {
            baseName = "master";
            mergeBase = Git(root, "merge-base", "HEAD", baseName);
        }

        string baseRef;
        string note;
        if (mergeBase.Code == 0)
        {
            baseRef = mergeBase.Out.Trim();
            note = $"只檢查變更的行（基準：HEAD 與 {baseName} 的 merge-base {baseRef[..Math.Min(7, baseRef.Length)]}）";
        }
        else
        {
            baseRef = "HEAD";
            note = "只檢查未提交的變更（找不到 origin/master 或 master）";
        }

        var diff = Git(root, ["-c", "core.quotepath=false", "diff", "-U0", "--no-color", "--no-ext-diff", "--relative", baseRef, .. scope]);
        if (diff.Code != 0)
        {
            return (null, note, $"git diff 失敗：{diff.Err.Trim()}");
        }

        var map = ParseDiff(diff.Out).ToDictionary(kv => kv.Key, kv => (HashSet<int>?)kv.Value);

        var untracked = Git(root, ["-c", "core.quotepath=false", "ls-files", "--others", "--exclude-standard", .. scope]);
        if (untracked.Code != 0)
        {
            return (null, note, $"git ls-files 失敗：{untracked.Err.Trim()}");
        }
        foreach (var line in untracked.Out.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            map[line.Replace('\\', '/')] = null;
        }

        return (map, note, string.Empty);
    }

    /// <summary>解析 git diff -U0 的輸出，取得每個檔案新增或修改的行號。</summary>
    /// <param name="diff">git diff 的輸出</param>
    /// <returns>路徑 → 行號</returns>
    public static Dictionary<string, HashSet<int>> ParseDiff(string diff)
    {
        var result = new Dictionary<string, HashSet<int>>();
        HashSet<int>? current = null;
        var previous = string.Empty;
        foreach (var raw in diff.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            // 只有緊接在 "--- " 後面的 "+++ " 才是檔案標頭（內容行 "++ x" 在 diff 裡也會變成 "+++ x"）
            if (line.StartsWith("+++ ", StringComparison.Ordinal) && previous.StartsWith("--- ", StringComparison.Ordinal))
            {
                var path = line[4..];
                if (path == "/dev/null")
                {
                    current = null;
                }
                else
                {
                    if (path.StartsWith("b/", StringComparison.Ordinal))
                    {
                        path = path[2..];
                    }
                    if (!result.TryGetValue(path, out current))
                    {
                        current = [];
                        result[path] = current;
                    }
                }
            }
            else if (current is not null && Hunk.Match(line) is { Success: true } m)
            {
                var start = int.Parse(m.Groups[1].Value);
                var count = m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : 1;
                for (var k = 0; k < count; k++)
                {
                    current.Add(start + k);
                }
            }
            previous = line;
        }
        return result;
    }

    /// <summary>判斷結果是否落在變更的行上。</summary>
    /// <param name="finding">檢查結果</param>
    /// <param name="lines">變更的行；null 代表整個檔案都算</param>
    /// <returns>是否要回報</returns>
    public static bool IsOnChangedLines(Finding finding, HashSet<int>? lines) =>
        lines is null
        || lines.Contains(finding.Line)
        || (finding.BlockLines?.Any(lines.Contains) ?? false);

    private static (int Code, string Out, string Err) Git(string root, params string[] arguments)
    {
        var info = new ProcessStartInfo("git")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
        };
        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        try
        {
            using var process = Process.Start(info)!;
            var errTask = process.StandardError.ReadToEndAsync();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return (process.ExitCode, output, errTask.Result);
        }
        catch (Exception ex)
        {
            return (-1, string.Empty, ex.Message);
        }
    }

    // ── import map ──────────────────────────────

    /// <summary>從 ImportMapHelper.cs 取出 import map 的對應（略過註解掉的行）。</summary>
    /// <param name="source">ImportMapHelper.cs 的內容</param>
    /// <returns>import map 的對應</returns>
    public static List<ImportMapEntry> ParseImportMap(string source)
    {
        var entries = new List<ImportMapEntry>();
        var lines = source.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].TrimStart().StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }
            foreach (Match m in ImportMapLine.Matches(lines[i]))
            {
                entries.Add(new ImportMapEntry(m.Groups[1].Value, m.Groups[2].Value, i + 1));
            }
        }
        return entries;
    }

    /// <summary>檢查 import map 指到的檔案是否存在於 wwwroot。</summary>
    /// <param name="importMap">import map 的對應</param>
    /// <param name="wwwroot">wwwroot 的完整路徑</param>
    /// <param name="mapPath">ImportMapHelper.cs 的相對路徑（輸出用）</param>
    /// <returns>檢查結果</returns>
    public static IEnumerable<Finding> CheckImportMapFiles(List<ImportMapEntry> importMap, string wwwroot, string mapPath)
    {
        foreach (var entry in importMap)
        {
            var target = entry.Target;
            if (target.StartsWith("~/", StringComparison.Ordinal))
            {
                target = target[1..];
            }
            if (!target.StartsWith('/') || target.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }
            var withoutQuery = target.Split('?', '#')[0];
            var file = Path.Combine(wwwroot, withoutQuery.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(file))
            {
                yield return new Finding(mapPath, entry.Line, "importmap-missing-file",
                    $"import map 的「{entry.Specifier}」指到 {entry.Target}，但 wwwroot 裡沒有這個檔案");
            }
        }
    }

    private static bool IsInImportMap(string specifier, List<ImportMapEntry> importMap) =>
        importMap.Any(e => e.Specifier == specifier
            || (e.Specifier.EndsWith('/') && specifier.StartsWith(e.Specifier, StringComparison.Ordinal)));

    // ── CSS ──────────────────────────────

    /// <summary>檢查 CSS：寫死的顏色與屬性排序。</summary>
    /// <param name="path">相對路徑（輸出用）</param>
    /// <param name="text">檔案內容</param>
    /// <returns>所有行的檢查結果</returns>
    public static List<Finding> LintCss(string path, string text)
    {
        var s = MaskCssComments(text);
        var index = new LineIndex(text);
        var findings = new List<Finding>();

        foreach (var block in ParseCssBlocks(s))
        {
            // 寫死的顏色
            foreach (var decl in block.Where(d => !d.Property.StartsWith("--", StringComparison.Ordinal)))
            {
                var masked = UrlOrString.Replace(decl.Value, m => new string(' ', m.Length));
                foreach (var m in HexColor.Matches(masked).Concat(FunctionColor.Matches(masked)).OrderBy(m => m.Index))
                {
                    var literal = Regex.Replace(m.Value, @"\s+", " ");
                    findings.Add(new Finding(path, index.LineOf(decl.ValueOffset + m.Index), "css-hardcoded-color",
                        $"「{decl.Property}」寫死顏色 {literal}，請改用主題變數（例如 var(--primary-accent)、var(--text-color)）"));
                }
            }

            // 屬性排序
            var ordered = block.Where(d => !d.Property.StartsWith("--", StringComparison.Ordinal)).ToList();
            for (var k = 1; k < ordered.Count; k++)
            {
                var key = SortKey(ordered[k].Property);
                if (string.CompareOrdinal(key, SortKey(ordered[k - 1].Property)) >= 0)
                {
                    continue;
                }
                var before = ordered.First(d => string.CompareOrdinal(SortKey(d.Property), key) > 0);
                var blockLines = block
                    .SelectMany(d => Enumerable.Range(index.LineOf(d.PropOffset), index.LineOf(d.EndOffset) - index.LineOf(d.PropOffset) + 1))
                    .ToHashSet();
                findings.Add(new Finding(path, index.LineOf(ordered[k].PropOffset), "css-property-order",
                    $"屬性要依字母排序：「{ordered[k].Property}」應排在「{before.Property}」（第 {index.LineOf(before.PropOffset)} 行）之前",
                    blockLines));
                break;
            }
        }

        return findings;
    }

    private static string SortKey(string property) => VendorPrefix.Replace(property.ToLowerInvariant(), string.Empty);

    /// <summary>把 CSS 註解換成空白（保留換行，位置不變）。</summary>
    private static string MaskCssComments(string text)
    {
        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            var c = chars[i];
            if (c is '"' or '\'')
            {
                i = SkipString(text, i);
                continue;
            }
            if (c == '/' && i + 1 < chars.Length && chars[i + 1] == '*')
            {
                var end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                end = end < 0 ? chars.Length : end + 2;
                for (var k = i; k < end; k++)
                {
                    if (chars[k] is not ('\n' or '\r'))
                    {
                        chars[k] = ' ';
                    }
                }
                i = end - 1;
            }
        }
        return new string(chars);
    }

    /// <summary>跳過引號字串，回傳結尾引號的位置（遇到換行就停）。</summary>
    private static int SkipString(string s, int start)
    {
        var quote = s[start];
        var i = start + 1;
        while (i < s.Length && s[i] != quote && s[i] != '\n')
        {
            if (s[i] == '\\')
            {
                i++;
            }
            i++;
        }
        return Math.Min(i, s.Length - 1);
    }

    /// <summary>把 CSS 拆成區塊，每個區塊是一串宣告（巢狀區塊各自獨立）。</summary>
    private static List<List<CssDecl>> ParseCssBlocks(string s)
    {
        var blocks = new List<List<CssDecl>>();
        var stack = new Stack<List<CssDecl>>();
        var segStart = 0;
        var paren = 0;
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (c is '"' or '\'')
            {
                i = SkipString(s, i);
                continue;
            }
            if (c == '(')
            {
                paren++;
            }
            else if (c == ')')
            {
                paren = Math.Max(0, paren - 1);
            }
            else if (paren == 0 && c == '{')
            {
                stack.Push([]);
                segStart = i + 1;
            }
            else if (paren == 0 && c == ';')
            {
                if (stack.Count > 0)
                {
                    AddDeclaration(stack.Peek(), s, segStart, i);
                }
                segStart = i + 1;
            }
            else if (paren == 0 && c == '}')
            {
                if (stack.Count > 0)
                {
                    AddDeclaration(stack.Peek(), s, segStart, i);
                    blocks.Add(stack.Pop());
                }
                segStart = i + 1;
            }
        }
        return blocks;
    }

    private static void AddDeclaration(List<CssDecl> block, string s, int start, int end)
    {
        var segment = s[start..end];
        var colon = segment.IndexOf(':');
        if (colon < 0)
        {
            return;
        }
        var property = segment[..colon].Trim();
        if (!PropertyName.IsMatch(property))
        {
            return;
        }
        var leading = segment.Length - segment.TrimStart().Length;
        block.Add(new CssDecl(property, start + leading, start + colon + 1, end, segment[(colon + 1)..]));
    }

    // ── JS ──────────────────────────────

    /// <summary>檢查 JS：var、雙引號、寬鬆比較、全域 Vue、import map。</summary>
    /// <param name="path">相對路徑（輸出用）</param>
    /// <param name="text">檔案內容</param>
    /// <param name="importMap">import map 的對應</param>
    /// <returns>所有行的檢查結果</returns>
    public static List<Finding> LintJs(string path, string text, List<ImportMapEntry> importMap)
    {
        var index = new LineIndex(text);
        var tokens = TokenizeJs(text).Where(t => t.Kind != TokKind.Comment).ToList();
        var findings = new List<Finding>();

        bool Is(int k, TokKind kind, string? value = null) =>
            k >= 0 && k < tokens.Count && tokens[k].Kind == kind && (value is null || tokens[k].Text == value);
        bool IsDot(int k) => Is(k, TokKind.Punct, ".") || Is(k, TokKind.Punct, "?.");
        void Add(Token t, string rule, string message) => findings.Add(new Finding(path, index.LineOf(t.Start), rule, message));

        // import * as Vue from 'vue' 之類的本地綁定，Vue.xxx 就不是全域變數
        var hasLocalVue = Enumerable.Range(0, tokens.Count).Any(k =>
            Is(k, TokKind.Ident, "Vue") && (Is(k - 1, TokKind.Ident, "as") || Is(k - 1, TokKind.Ident, "import")));

        for (var k = 0; k < tokens.Count; k++)
        {
            var t = tokens[k];
            switch (t.Kind)
            {
                case TokKind.Ident when t.Text == "var" && !IsDot(k - 1)
                    && (Is(k + 1, TokKind.Ident) || Is(k + 1, TokKind.Punct, "{") || Is(k + 1, TokKind.Punct, "[")):
                    Add(t, "js-no-var", "請改用 const 或 let，不要使用 var");
                    break;

                case TokKind.Ident when t.Text == "Vue":
                    if (IsDot(k - 1))
                    {
                        if (Is(k - 2, TokKind.Ident, "window") || Is(k - 2, TokKind.Ident, "globalThis") || Is(k - 2, TokKind.Ident, "self"))
                        {
                            Add(t, "js-global-vue", $"不要使用全域的 {tokens[k - 2].Text}.Vue，請 import {{ createApp }} from 'vue'");
                        }
                    }
                    else if (!hasLocalVue && IsDot(k + 1) && Is(k + 2, TokKind.Ident))
                    {
                        Add(t, "js-global-vue", $"不要使用全域的 Vue.{tokens[k + 2].Text}，請 import {{ {tokens[k + 2].Text} }} from 'vue'");
                    }
                    else if (!hasLocalVue && Is(k - 1, TokKind.Punct, "="))
                    {
                        Add(t, "js-global-vue", "不要從全域的 Vue 取出函式，請 import { createApp } from 'vue'");
                    }
                    break;

                case TokKind.String:
                    if (t.Quote == '"' && !t.Text.Contains('\''))
                    {
                        Add(t, "js-double-quote", $"字串請使用單引號：{Shorten(t.Text)}");
                    }
                    var isSpecifier = (Is(k - 1, TokKind.Ident, "from") && !IsDot(k - 2))
                        || (Is(k - 1, TokKind.Ident, "import") && !IsDot(k - 2))
                        || (Is(k - 1, TokKind.Punct, "(") && Is(k - 2, TokKind.Ident, "import") && !IsDot(k - 3));
                    if (isSpecifier && t.Text.Length >= 2)
                    {
                        var specifier = t.Text[1..^1];
                        var isBare = !(specifier.StartsWith("./", StringComparison.Ordinal)
                            || specifier.StartsWith("../", StringComparison.Ordinal)
                            || specifier.StartsWith('/')
                            || specifier.Contains("://", StringComparison.Ordinal)
                            || specifier.StartsWith("data:", StringComparison.Ordinal));
                        if (isBare && !IsInImportMap(specifier, importMap))
                        {
                            Add(t, "js-import-map", $"「{specifier}」不在 import map 裡，請加到 Involver/Helpers/ImportMapHelper.cs");
                        }
                    }
                    break;

                case TokKind.Punct when t.Text is "==" or "!=":
                    Add(t, "js-loose-equality", $"請使用 {t.Text}= 嚴格比較，不要用 {t.Text}");
                    break;
            }
        }

        return findings;
    }

    private static string Shorten(string text) => text.Length <= 40 ? text : $"{text[..37]}…\"";

    /// <summary>
    /// 簡單的 JS tokenizer：分出註解、字串、樣板字串（含 ${...}）、正規表示式、識別字、數字、符號。
    /// 正規表示式與除號依前一個 token 判斷。
    /// </summary>
    /// <param name="s">JS 原始碼</param>
    /// <returns>token 清單</returns>
    public static List<Token> TokenizeJs(string s)
    {
        var tokens = new List<Token>();
        // true 代表這個 { 是樣板字串的 ${，對應的 } 結束後要回到樣板字串
        var braces = new Stack<bool>();
        Token? last = null;
        var i = 0;
        while (i < s.Length)
        {
            var c = s[i];
            if (char.IsWhiteSpace(c) || c == '﻿')
            {
                i++;
                continue;
            }

            var start = i;
            Token token;
            if (c == '/' && i + 1 < s.Length && s[i + 1] == '/')
            {
                while (i < s.Length && s[i] != '\n')
                {
                    i++;
                }
                tokens.Add(new Token(TokKind.Comment, start, s[start..i]));
                continue;
            }
            if (c == '/' && i + 1 < s.Length && s[i + 1] == '*')
            {
                var end = s.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = end < 0 ? s.Length : end + 2;
                tokens.Add(new Token(TokKind.Comment, start, s[start..i]));
                continue;
            }

            if (c is '\'' or '"')
            {
                i = SkipString(s, i) + 1;
                token = new Token(TokKind.String, start, s[start..Math.Min(i, s.Length)], c);
            }
            else if (c == '`')
            {
                i = ScanTemplate(s, i + 1, braces);
                token = new Token(TokKind.Template, start, s[start..i]);
            }
            else if (c == '}' && braces.Count > 0 && braces.Peek())
            {
                braces.Pop();
                i = ScanTemplate(s, i + 1, braces);
                token = new Token(TokKind.Template, start, s[start..i]);
            }
            else if (char.IsLetter(c) || c is '_' or '$')
            {
                i++;
                while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] is '_' or '$' or '‌' or '‍'))
                {
                    i++;
                }
                token = new Token(TokKind.Ident, start, s[start..i]);
            }
            else if (char.IsAsciiDigit(c) || (c == '.' && i + 1 < s.Length && char.IsAsciiDigit(s[i + 1])))
            {
                i++;
                while (i < s.Length && (char.IsAsciiLetterOrDigit(s[i]) || s[i] is '.' or '_'))
                {
                    i++;
                }
                token = new Token(TokKind.Number, start, s[start..i]);
            }
            else if (c == '/' && IsRegexAllowed(last) && ScanRegex(s, i) is var regexEnd and > 0)
            {
                i = regexEnd;
                token = new Token(TokKind.Regex, start, s[start..i]);
            }
            else
            {
                var punct = Puncts.FirstOrDefault(p => string.CompareOrdinal(s, i, p, 0, p.Length) == 0
                    && !(p == "?." && i + 2 < s.Length && char.IsAsciiDigit(s[i + 2])))
                    ?? c.ToString();
                if (punct == "{")
                {
                    braces.Push(false);
                }
                else if (punct == "}" && braces.Count > 0)
                {
                    braces.Pop();
                }
                i += punct.Length;
                token = new Token(TokKind.Punct, start, punct);
            }

            tokens.Add(token);
            last = token;
        }
        return tokens;
    }

    /// <summary>從樣板字串內容開始掃描，回傳結尾 ` 之後或 ${ 之後的位置。</summary>
    private static int ScanTemplate(string s, int i, Stack<bool> braces)
    {
        while (i < s.Length)
        {
            if (s[i] == '\\')
            {
                i += 2;
                continue;
            }
            if (s[i] == '`')
            {
                return i + 1;
            }
            if (s[i] == '$' && i + 1 < s.Length && s[i + 1] == '{')
            {
                braces.Push(true);
                return i + 2;
            }
            i++;
        }
        return s.Length;
    }

    /// <summary>掃描正規表示式，回傳結尾（含旗標）之後的位置；同一行內沒有結尾就回傳 -1。</summary>
    private static int ScanRegex(string s, int start)
    {
        var inClass = false;
        for (var i = start + 1; i < s.Length; i++)
        {
            var c = s[i];
            if (c is '\n' or '\r')
            {
                return -1;
            }
            if (c == '\\')
            {
                i++;
            }
            else if (c == '[')
            {
                inClass = true;
            }
            else if (c == ']')
            {
                inClass = false;
            }
            else if (c == '/' && !inClass)
            {
                i++;
                while (i < s.Length && char.IsAsciiLetter(s[i]))
                {
                    i++;
                }
                return i;
            }
        }
        return -1;
    }

    /// <summary>依前一個 token 判斷 / 是正規表示式的開頭還是除號。</summary>
    private static bool IsRegexAllowed(Token? last) => last switch
    {
        null => true,
        { Kind: TokKind.Ident } => RegexKeywords.Contains(last.Text),
        { Kind: TokKind.Number or TokKind.String or TokKind.Regex } => false,
        { Kind: TokKind.Template } => last.Text.EndsWith("${", StringComparison.Ordinal),
        { Kind: TokKind.Punct } => last.Text is not (")" or "]" or "++" or "--"),
        _ => true,
    };

    // ── 自我測試 ──────────────────────────────

    private const string SampleCss = """
        :root {
          --primary-accent: #ff0000;
        }
        .a {
          color: #fff;
          background-color: var(--card-bg-color);
        }
        .b {
          border: 1px solid rgba(0, 0, 0, 0.1);
          box-shadow: 0 0 0 rgba(var(--rgb), 0.5);
          color: transparent;
        }
        /* .x { color: #123456; } */
        .c {
          margin: 0;
          color: var(--text-color);
          -webkit-appearance: none;
        }
        .d { content: "#fff;{"; background: url(data:image/svg+xml;utf8,#abc); }
        @media (prefers-color-scheme: dark) {
          .e { --x: rgb(1, 2, 3); -webkit-transition: none; transition: none; width: 1px; }
          .f:hover { outline: hsl(10 20% 30%); }
        }
        """;

    private const string SampleJs = """
        import { createApp } from 'vue';
        import { foo } from 'not-in-map';
        import x from './local.js';
        var a = 1;
        const b = "double";
        const c = "it's ok";
        const d = 'has "double" inside';
        // const e = "in comment";
        /* var f = "block"; */
        const g = `template "x" ${ "inner" } ${ { k: 'v' }.k } end`;
        const h = /"[^"/]*"/g;
        if (a == 1 || a != 2 || a === 3 || a !== 4) {}
        const i = a / 2 / "x".length;
        const app = Vue.createApp({});
        const w = window.Vue;
        const { ref } = Vue;
        const obj = { var: 1 };
        const lazy = await import('chart.js');
        const lazy2 = await import('missing-pkg');
        const t = `a${`nested ${"deep"}`}b`;
        const r = (x) => x.test(/'/) ? '"' : "'";
        """;

    private const string SampleDiff = """
        diff --git a/Involver/wwwroot/css/site.css b/Involver/wwwroot/css/site.css
        --- a/Involver/wwwroot/css/site.css
        +++ b/Involver/wwwroot/css/site.css
        @@ -10 +10,2 @@ body {
        -  color: red;
        +  color: blue;
        +++ looks like a header but is content
        @@ -20,3 +21 @@
        +  margin: 0;
        @@ -30 +30,0 @@
        -  padding: 0;
        diff --git a/Involver/wwwroot/js/old.js b/Involver/wwwroot/js/old.js
        --- a/Involver/wwwroot/js/old.js
        +++ /dev/null
        @@ -1 +0,0 @@
        -var x;
        """;

    /// <summary>用內建範例驗證規則。</summary>
    /// <returns>結束代碼：通過為 0，失敗為 1</returns>
    public static int RunSelfTest()
    {
        var failures = new List<string>();

        void Expect(string name, IEnumerable<Finding> actual, params (string Rule, int Line)[] expected)
        {
            var got = actual.Select(f => $"{f.Rule}@{f.Line}").Order(StringComparer.Ordinal).ToList();
            var want = expected.Select(e => $"{e.Rule}@{e.Line}").Order(StringComparer.Ordinal).ToList();
            if (!got.SequenceEqual(want))
            {
                failures.Add($"{name}：\n    預期 {string.Join(", ", want)}\n    實際 {string.Join(", ", got)}");
            }
        }

        var css = LintCss("sample.css", SampleCss);
        Expect("CSS", css,
            ("css-hardcoded-color", 5),
            ("css-property-order", 6),
            ("css-hardcoded-color", 9),
            ("css-property-order", 16),
            ("css-property-order", 19),
            ("css-hardcoded-color", 22));

        var importMap = new List<ImportMapEntry> { new("vue", "/lib/vue/vue.js", 1), new("chart.js", "/lib/chart.js", 2) };
        var js = LintJs("sample.js", SampleJs, importMap);
        Expect("JS", js,
            ("js-import-map", 2),
            ("js-no-var", 4),
            ("js-double-quote", 5),
            ("js-double-quote", 10),
            ("js-loose-equality", 12),
            ("js-loose-equality", 12),
            ("js-double-quote", 13),
            ("js-global-vue", 14),
            ("js-global-vue", 15),
            ("js-global-vue", 16),
            ("js-import-map", 19),
            ("js-double-quote", 20));

        var diff = ParseDiff(SampleDiff);
        var siteLines = diff.GetValueOrDefault("Involver/wwwroot/css/site.css") ?? [];
        if (diff.ContainsKey("Involver/wwwroot/js/old.js") || diff.Count != 1
            || !siteLines.SetEquals([10, 11, 21]))
        {
            failures.Add($"diff 解析：預期 site.css 的 10, 11, 21 行，實際 {string.Join("; ", diff.Select(kv => $"{kv.Key}: {string.Join(", ", kv.Value.Order())}"))}");
        }

        // 只改到區塊中的其他行，排序問題也要回報；改到區塊外的行則不回報
        var order = css.First(f => f.Rule == "css-property-order");
        if (!IsOnChangedLines(order, [5]) || IsOnChangedLines(order, [2]) || !IsOnChangedLines(order, null))
        {
            failures.Add("變更行過濾：css-property-order 應該在區塊任一行變更時回報");
        }
        var color = css.First(f => f.Rule == "css-hardcoded-color");
        if (IsOnChangedLines(color, [6]) || !IsOnChangedLines(color, [5]))
        {
            failures.Add("變更行過濾：css-hardcoded-color 只在該行變更時回報");
        }

        if (failures.Count > 0)
        {
            Console.WriteLine("自我測試失敗：");
            foreach (var failure in failures)
            {
                Console.WriteLine($"  {failure}");
            }
            return 1;
        }

        Console.WriteLine($"自我測試通過（CSS {css.Count} 個、JS {js.Count} 個預期的問題，diff 解析與變更行過濾正常）。");
        return 0;
    }
}
