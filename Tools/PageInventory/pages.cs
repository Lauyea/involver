#:property TargetFramework=net10.0
#:property PublishAot=false

// Razor Pages 清單：靜態掃描 Involver 的頁面，列出路由、檔案、授權需求與 handler。
// 改完 UI 後用來查「要開哪個網址」以及「要用哪種帳號（匿名／登入／角色／policy）」。
//
// 用法（在這個資料夾或 repo 根目錄執行，需要 .NET 10 SDK 以上）：
//   dotnet run pages.cs                     列出所有頁面（Markdown 表格，依路由排序）
//   dotnet run pages.cs -- --filter Admin   只列路由或檔案路徑包含指定文字的頁面（不分大小寫）
//   dotnet run pages.cs -- --json           改以 JSON 陣列輸出（可與 --filter 一起用）
//   dotnet run pages.cs -- --help           顯示說明
//
// 只讀取原始碼，不建置網站、不連資料庫、不寫入任何檔案。
// 判斷規則（都是以正規表示式靜態解析，不是實際執行 ASP.NET Core 的授權管線）：
//   - 頁面：Involver/Pages 與 Involver/Areas/*/Pages 底下含 @page 的 .cshtml；略過底線開頭的檔案與 Shared 資料夾。
//   - 路由：依檔案路徑推算（Index 對應資料夾，Area 加上 /{Area} 前綴）；@page "..." 以 / 開頭時取代，否則接在後面。
//   - 授權：Program.cs 的全域 AuthorizeFilter／FallbackPolicy、options.Conventions.Authorize*／AllowAnonymous* 規則，
//     加上頁面模型（含基底類別）的 [Authorize]、[AllowAnonymous]。[AllowAnonymous] 會蓋過所有授權規則。
//   - 常數：Involver/Authorization 底下的 const string、static readonly string（與 string[]）會換成實際的字串。

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

Console.OutputEncoding = Encoding.UTF8;

string? filter = null;
var asJson = false;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--help":
        case "-h":
        case "/?":
            PrintUsage(Console.Out);
            return 0;
        case "--json":
            asJson = true;
            break;
        case "--filter":
            if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                Console.Error.WriteLine("--filter 後面要接要搜尋的文字，例如：--filter Admin");
                return 1;
            }
            filter = args[++i];
            break;
        default:
            Console.Error.WriteLine($"不認得的參數「{args[i]}」。");
            PrintUsage(Console.Error);
            return 1;
    }
}

try
{
    var repoRoot = FindRepoRoot();
    if (repoRoot is null)
    {
        Console.Error.WriteLine("找不到 repo 根目錄（從目前資料夾往上找不到 Involver.sln）。");
        return 1;
    }

    var webRoot = Path.Combine(repoRoot, "Involver");
    var programPath = Path.Combine(webRoot, "Program.cs");
    if (!File.Exists(programPath))
    {
        Console.Error.WriteLine($"找不到 {programPath}。");
        return 1;
    }

    var constants = ConstantTable.Load(Path.Combine(webRoot, "Authorization"), programPath);
    var program = ProgramConventions.Parse(programPath, constants);
    var classIndex = ClassIndex.Build(webRoot);
    var pages = PageScanner.Scan(repoRoot, webRoot, program, constants, classIndex);

    if (filter is not null)
    {
        pages = pages
            .Where(p => p.Route.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || p.File.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    if (asJson)
    {
        WriteJson(pages);
    }
    else
    {
        WriteMarkdown(pages, program, constants, filter);
    }

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"掃描失敗：{ex.Message}");
    return 1;
}

// ── 輸出 ──────────────────────────────

static void PrintUsage(TextWriter writer)
{
    writer.WriteLine("Razor Pages 清單：列出路由、檔案、授權需求與 handler（只讀取原始碼）。");
    writer.WriteLine();
    writer.WriteLine("用法：");
    writer.WriteLine("  dotnet run pages.cs                     列出所有頁面（Markdown 表格，依路由排序）");
    writer.WriteLine("  dotnet run pages.cs -- --filter Admin   只列路由或檔案路徑包含指定文字的頁面（不分大小寫）");
    writer.WriteLine("  dotnet run pages.cs -- --json           改以 JSON 陣列輸出（可與 --filter 一起用）");
    writer.WriteLine("  dotnet run pages.cs -- --help           顯示說明");
}

static void WriteJson(List<PageInfo> pages)
{
    var options = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };
    var rows = pages.Select(p => new
    {
        p.Route,
        p.File,
        p.Area,
        Access = p.Auth.AccessKind,
        Authorization = p.Auth.Display,
        Policies = p.Auth.Policies,
        Roles = p.Auth.RoleSets,
        AuthSources = p.Auth.Sources,
        Handlers = p.Handlers,
        ResourceAuthorization = p.ResourceAuthorization,
        Notes = p.Notes,
    });
    Console.WriteLine(JsonSerializer.Serialize(rows, options));
}

static void WriteMarkdown(List<PageInfo> pages, ProgramConventions program, ConstantTable constants, string? filter)
{
    Console.WriteLine("# Razor Pages 清單");
    Console.WriteLine();
    Console.WriteLine(program.GlobalRequiresLogin
        ? $"全站預設需登入（{program.GlobalSource}）；標了 [AllowAnonymous] 的頁面才開放匿名。"
        : "全站沒有預設授權；沒有 [Authorize] 或資料夾規則的頁面都可以匿名進入。");
    if (filter is not null)
    {
        Console.WriteLine($"篩選：路由或檔案包含「{filter}」。");
    }
    Console.WriteLine();
    Console.WriteLine("| 路由 | 檔案 | 授權 | Handlers | 備註 |");
    Console.WriteLine("|---|---|---|---|---|");
    foreach (var page in pages)
    {
        Console.WriteLine(string.Join(" | ",
            $"| `{Cell(page.Route)}`",
            Cell(page.File),
            Cell(page.Auth.Display),
            Cell(string.Join(", ", page.Handlers)),
            Cell(string.Join("；", page.Notes))) + " |");
    }

    var anonymous = pages.Count(p => p.Auth.AccessKind == "anonymous");
    var needAuth = pages.Count - anonymous;
    var needRoleOrPolicy = pages.Count(p => p.Auth.AccessKind is "policy" or "role");
    var resource = pages.Count(p => p.ResourceAuthorization);
    Console.WriteLine();
    Console.WriteLine($"共 {pages.Count} 頁：匿名 {anonymous}、需登入 {needAuth}（其中需 policy 或角色 {needRoleOrPolicy}）；頁面內另做資源授權 {resource}。");

    var usedPolicies = pages
        .SelectMany(p => p.Auth.Policies)
        .Distinct(StringComparer.Ordinal)
        .Order(StringComparer.Ordinal)
        .ToList();
    if (usedPolicies.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("## Policy 對照");
        Console.WriteLine();
        foreach (var policy in usedPolicies)
        {
            var roles = constants.PolicyRoles.GetValueOrDefault(policy);
            Console.WriteLine(roles is null
                ? $"- `{policy}`：找不到 RequireRole 設定，請看 AddPolicy 原始碼"
                : $"- `{policy}`：需要以下任一角色：{string.Join("、", roles)}");
        }
    }
}

static string Cell(string value) => value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");

static string? FindRepoRoot()
{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir is not null)
    {
        if (File.Exists(Path.Combine(dir.FullName, "Involver.sln")))
        {
            return dir.FullName;
        }
        dir = dir.Parent;
    }
    return null;
}

// ── 共用工具 ──────────────────────────────

/// <summary>
/// C# 原始碼的簡易文字處理（去註解、切參數）。
/// </summary>
static class CSharpText
{
    private static readonly Regex BlockComment = new(@"/\*.*?\*/", RegexOptions.Singleline);
    private static readonly Regex LineComment = new(@"^[ \t]*//.*$", RegexOptions.Multiline);

    /// <summary>
    /// 移除區塊註解與整行的 // 註解（行尾註解保留，避免誤刪字串中的網址）。
    /// </summary>
    /// <param name="code">原始碼</param>
    /// <returns>去掉註解的原始碼</returns>
    public static string StripComments(string code) => LineComment.Replace(BlockComment.Replace(code, " "), "");

    /// <summary>
    /// 以最外層的逗號切開參數（略過括號、中括號與字串內的逗號）。
    /// </summary>
    /// <param name="args">參數字串</param>
    /// <returns>去掉前後空白的各個參數</returns>
    public static List<string> SplitTopLevel(string args, char separator = ',')
    {
        var parts = new List<string>();
        var depth = 0;
        var inString = false;
        var start = 0;
        for (var i = 0; i < args.Length; i++)
        {
            var c = args[i];
            if (inString)
            {
                if (c == '\\')
                {
                    i++;
                }
                else if (c == '"')
                {
                    inString = false;
                }
                continue;
            }
            switch (c)
            {
                case '"':
                    inString = true;
                    break;
                case '(' or '[' or '{':
                    depth++;
                    break;
                case ')' or ']' or '}':
                    depth--;
                    break;
                default:
                    if (c == separator && depth == 0)
                    {
                        parts.Add(args[start..i].Trim());
                        start = i + 1;
                    }
                    break;
            }
        }
        parts.Add(args[start..].Trim());
        return parts.Where(p => p.Length > 0).ToList();
    }
}

// ── 常數與 policy ──────────────────────────────

/// <summary>
/// 授權相關常數（ClassName.MemberName → 值）與 policy 需要的角色。
/// </summary>
sealed class ConstantTable
{
    private static readonly Regex ClassDecl = new(@"\b(?:class|record|struct)\s+(\w+)");
    private static readonly Regex ScalarDecl = new(
        @"\b(?:const\s+string|static\s+readonly\s+string|readonly\s+static\s+string)\s+(\w+)\s*=\s*([^;]+);");
    private static readonly Regex ArrayDecl = new(
        @"\bstatic\s+readonly\s+string\[\]\s+(\w+)\s*=\s*(?:\[(?<items>[^\]]*)\]|new\s*(?:string)?\s*\[\]\s*\{(?<items>[^}]*)\})",
        RegexOptions.Singleline);
    private static readonly Regex DerivedArrayDecl = new(
        @"\bstatic\s+readonly\s+string\[\]\s+(\w+)\s*=\s*(?<source>[\w.]+)\s*\.\s*Where\(\s*\w+\s*=>\s*\w+\s*\.\s*(?<op>EndsWith|StartsWith|Contains)\(\s*""(?<arg>[^""]*)""",
        RegexOptions.Singleline);
    private static readonly Regex PolicyDecl = new(
        @"AddPolicy\(\s*(?<name>[^,]+?)\s*,\s*\w+\s*=>\s*\w+\s*\.RequireRole\((?<roles>[^)]*)\)",
        RegexOptions.Singleline);
    private static readonly Regex StringLiteral = new(@"^@?""(?<v>(?:[^""\\]|\\.)*)""$");
    private static readonly Regex NameOf = new(@"^nameof\(\s*(?:[\w.]+\.)?(?<v>\w+)\s*\)$");

    private readonly Dictionary<string, string> _scalars = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<string>> _arrays = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (string Source, string Op, string Arg)> _derivedArrays = new(StringComparer.Ordinal);

    /// <summary>
    /// policy 名稱 → 需要的角色（任一即可）
    /// </summary>
    public Dictionary<string, List<string>> PolicyRoles { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// 讀取 Authorization 資料夾與其他指定檔案中的常數與 policy。
    /// </summary>
    /// <param name="authorizationDir">Involver/Authorization 資料夾</param>
    /// <param name="extraFiles">其他要一起讀的檔案（例如 Program.cs）</param>
    /// <returns>常數表</returns>
    public static ConstantTable Load(string authorizationDir, params string[] extraFiles)
    {
        var table = new ConstantTable();
        var files = Directory.Exists(authorizationDir)
            ? Directory.EnumerateFiles(authorizationDir, "*.cs", SearchOption.AllDirectories).ToList()
            : [];
        files.AddRange(extraFiles.Where(File.Exists));

        var sources = files.Select(f => CSharpText.StripComments(File.ReadAllText(f))).ToList();
        foreach (var code in sources)
        {
            var classes = ClassDecl.Matches(code).ToList();
            foreach (Match m in ScalarDecl.Matches(code))
            {
                table._scalars[$"{ClassAt(classes, m.Index)}.{m.Groups[1].Value}"] = m.Groups[2].Value.Trim();
            }
            foreach (Match m in ArrayDecl.Matches(code))
            {
                table._arrays[$"{ClassAt(classes, m.Index)}.{m.Groups[1].Value}"] =
                    CSharpText.SplitTopLevel(m.Groups["items"].Value);
            }
            // 例如 AdministratorRoles = AllRoles.Where(r => r.EndsWith("Administrators"))
            foreach (Match m in DerivedArrayDecl.Matches(code))
            {
                table._derivedArrays[$"{ClassAt(classes, m.Index)}.{m.Groups[1].Value}"] =
                    (m.Groups["source"].Value, m.Groups["op"].Value, m.Groups["arg"].Value);
            }
        }

        // policy 要等常數都讀完才解析
        foreach (var code in sources)
        {
            var classes = ClassDecl.Matches(code).ToList();
            foreach (Match m in PolicyDecl.Matches(code))
            {
                var context = ClassAt(classes, m.Index);
                var name = table.Resolve(m.Groups["name"].Value, context) ?? m.Groups["name"].Value.Trim();
                table.PolicyRoles[name] = CSharpText.SplitTopLevel(m.Groups["roles"].Value)
                    .SelectMany(r => table.ResolveList(r, context))
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
            }
        }
        return table;
    }

    /// <summary>
    /// 把運算式換成字串值：字串常值、nameof、Class.Member 常數，以及用 + 串接的組合。
    /// </summary>
    /// <param name="expression">C# 運算式</param>
    /// <param name="contextClass">運算式所在的類別（解析未加類別名稱的成員用）</param>
    /// <returns>解析出的字串；無法解析時為 null</returns>
    public string? Resolve(string expression, string? contextClass = null) => Resolve(expression, contextClass, 0);

    /// <summary>
    /// 把運算式換成字串清單；string[] 常數會展開成各個元素。
    /// </summary>
    /// <param name="expression">C# 運算式</param>
    /// <param name="contextClass">運算式所在的類別</param>
    /// <returns>解析出的字串；無法解析的部分保留原始運算式</returns>
    public List<string> ResolveList(string expression, string? contextClass = null)
    {
        var expr = expression.Trim();
        var arrayKey = FindKey(_arrays, expr, contextClass);
        if (arrayKey is not null)
        {
            var owner = arrayKey[..arrayKey.IndexOf('.')];
            return _arrays[arrayKey].SelectMany(item => ResolveList(item, owner)).ToList();
        }
        var derivedKey = FindKey(_derivedArrays, expr, contextClass);
        if (derivedKey is not null)
        {
            var owner = derivedKey[..derivedKey.IndexOf('.')];
            var (source, op, arg) = _derivedArrays[derivedKey];
            return ResolveList(source, owner)
                .Where(v => op switch
                {
                    "EndsWith" => v.EndsWith(arg, StringComparison.Ordinal),
                    "StartsWith" => v.StartsWith(arg, StringComparison.Ordinal),
                    _ => v.Contains(arg, StringComparison.Ordinal),
                })
                .ToList();
        }
        return [Resolve(expr, contextClass) ?? expr];
    }

    private string? Resolve(string expression, string? contextClass, int depth)
    {
        var expr = expression.Trim();
        if (depth > 10 || expr.Length == 0)
        {
            return null;
        }

        var parts = CSharpText.SplitTopLevel(expr, '+');
        if (parts.Count > 1)
        {
            var values = parts.Select(p => Resolve(p, contextClass, depth + 1)).ToList();
            return values.Any(v => v is null) ? null : string.Concat(values);
        }

        var literal = StringLiteral.Match(expr);
        if (literal.Success)
        {
            return Regex.Unescape(literal.Groups["v"].Value);
        }

        var nameOf = NameOf.Match(expr);
        if (nameOf.Success)
        {
            return nameOf.Groups["v"].Value;
        }

        var key = FindKey(_scalars, expr, contextClass);
        if (key is null)
        {
            return null;
        }
        return Resolve(_scalars[key], key[..key.IndexOf('.')], depth + 1);
    }

    /// <summary>
    /// 依序用「Class.Member」、「所在類別.Member」、「全域唯一的 Member」找出對應的鍵。
    /// 最後一種可以處理 using 別名（例如 PaymentRoles.X）。
    /// </summary>
    private static string? FindKey<T>(Dictionary<string, T> map, string expr, string? contextClass)
    {
        if (!Regex.IsMatch(expr, @"^[\w.]+$"))
        {
            return null;
        }
        var segments = expr.Split('.');
        var member = segments[^1];
        if (segments.Length >= 2)
        {
            var qualified = $"{segments[^2]}.{member}";
            if (map.ContainsKey(qualified))
            {
                return qualified;
            }
        }
        else if (contextClass is not null && map.ContainsKey($"{contextClass}.{member}"))
        {
            return $"{contextClass}.{member}";
        }

        var candidates = map.Keys.Where(k => k.EndsWith($".{member}", StringComparison.Ordinal)).Take(2).ToList();
        return candidates.Count == 1 ? candidates[0] : null;
    }

    private static string ClassAt(List<Match> classes, int index) =>
        classes.LastOrDefault(c => c.Index < index)?.Groups[1].Value ?? "_";
}

// ── Program.cs 的授權規則 ──────────────────────────────

/// <summary>
/// Program.cs 中的全域授權與 Razor Pages 授權規則（Conventions）。
/// </summary>
sealed class ProgramConventions
{
    private static readonly Regex ConventionCall = new(
        @"Conventions\s*\.\s*(?<method>AuthorizeFolder|AuthorizeAreaFolder|AuthorizePage|AuthorizeAreaPage|AllowAnonymousToPage|AllowAnonymousToFolder|AllowAnonymousToAreaPage|AllowAnonymousToAreaFolder)\s*\((?<args>[^()]*(?:\([^()]*\)[^()]*)*)\)");

    /// <summary>
    /// 是否全站預設需登入
    /// </summary>
    public bool GlobalRequiresLogin { get; private set; }

    /// <summary>
    /// 全站預設授權的來源說明
    /// </summary>
    public string GlobalSource { get; private set; } = "";

    /// <summary>
    /// 解析出的 Conventions 規則
    /// </summary>
    public List<ConventionRule> Rules { get; } = [];

    /// <summary>
    /// 解析 Program.cs。
    /// </summary>
    /// <param name="programPath">Program.cs 路徑</param>
    /// <param name="constants">常數表</param>
    /// <returns>解析結果</returns>
    public static ProgramConventions Parse(string programPath, ConstantTable constants)
    {
        var code = CSharpText.StripComments(File.ReadAllText(programPath));
        var result = new ProgramConventions();

        if (Regex.IsMatch(code, @"Filters\s*\.\s*Add\(\s*new\s+AuthorizeFilter\s*\("))
        {
            result.GlobalRequiresLogin = true;
            result.GlobalSource = "Program.cs 的全域 AuthorizeFilter";
        }
        else if (Regex.IsMatch(code, @"FallbackPolicy\s*="))
        {
            result.GlobalRequiresLogin = true;
            result.GlobalSource = "Program.cs 的 FallbackPolicy";
        }
        else if (Regex.IsMatch(code, @"MapRazorPages\(\s*\)\s*\.\s*RequireAuthorization\("))
        {
            result.GlobalRequiresLogin = true;
            result.GlobalSource = "Program.cs 的 MapRazorPages().RequireAuthorization()";
        }

        foreach (Match m in ConventionCall.Matches(code))
        {
            var method = m.Groups["method"].Value;
            var rawArgs = CSharpText.SplitTopLevel(m.Groups["args"].Value);
            var values = rawArgs.Select(a => constants.Resolve(a) ?? a).ToList();
            var isArea = method.Contains("Area", StringComparison.Ordinal);
            var offset = isArea ? 1 : 0;
            if (values.Count <= offset)
            {
                continue;
            }
            result.Rules.Add(new ConventionRule(
                Method: method,
                Area: isArea ? values[0] : null,
                Path: values[offset],
                IsFolder: method.EndsWith("Folder", StringComparison.Ordinal),
                AllowAnonymous: method.StartsWith("AllowAnonymous", StringComparison.Ordinal),
                Policy: values.Count > offset + 1 ? values[offset + 1] : null,
                Source: $"Program.cs：{method}({string.Join(", ", values)})"));
        }
        return result;
    }
}

/// <summary>
/// 一條 Razor Pages 授權規則（AuthorizeFolder、AllowAnonymousToPage 等）。
/// </summary>
/// <param name="Method">方法名稱</param>
/// <param name="Area">Area 名稱；非 Area 規則為 null</param>
/// <param name="Path">資料夾或頁面路徑（例如 /Creator、/Users/Index）</param>
/// <param name="IsFolder">是否為資料夾規則</param>
/// <param name="AllowAnonymous">是否為開放匿名的規則</param>
/// <param name="Policy">需要的 policy；只需登入時為 null</param>
/// <param name="Source">顯示用的來源說明</param>
sealed record ConventionRule(string Method, string? Area, string Path, bool IsFolder, bool AllowAnonymous, string? Policy, string Source)
{
    /// <summary>
    /// 規則是否套用到指定頁面。
    /// </summary>
    /// <param name="area">頁面的 Area；非 Area 頁面為 null</param>
    /// <param name="viewPath">頁面在 Pages 資料夾內的路徑（例如 /Users/Index）</param>
    /// <returns>是否套用</returns>
    public bool Matches(string? area, string viewPath)
    {
        if (!string.Equals(Area, area, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        if (!IsFolder)
        {
            return string.Equals(Path, viewPath, StringComparison.OrdinalIgnoreCase);
        }
        var folder = Path.TrimEnd('/');
        return folder.Length == 0 || viewPath.StartsWith($"{folder}/", StringComparison.OrdinalIgnoreCase);
    }
}

// ── 類別索引（找基底類別用）──────────────────────────────

/// <summary>
/// Involver 專案中類別名稱 → 定義所在檔案，用來追查頁面模型的基底類別。
/// </summary>
sealed class ClassIndex
{
    private static readonly Regex ClassDecl = new(@"\bclass\s+(\w+)");
    private readonly Dictionary<string, string> _files = new(StringComparer.Ordinal);

    /// <summary>
    /// 建立索引（略過 bin、obj）。
    /// </summary>
    /// <param name="webRoot">Involver 專案資料夾</param>
    /// <returns>類別索引</returns>
    public static ClassIndex Build(string webRoot)
    {
        var index = new ClassIndex();
        foreach (var file in Directory.EnumerateFiles(webRoot, "*.cs", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(webRoot, file).Replace('\\', '/');
            if (rel.StartsWith("bin/", StringComparison.OrdinalIgnoreCase) || rel.StartsWith("obj/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            foreach (Match m in ClassDecl.Matches(File.ReadAllText(file)))
            {
                index._files.TryAdd(m.Groups[1].Value, file);
            }
        }
        return index;
    }

    /// <summary>
    /// 取得類別定義所在的檔案。
    /// </summary>
    /// <param name="className">類別名稱</param>
    /// <returns>檔案路徑；找不到時為 null</returns>
    public string? FileOf(string className) => _files.GetValueOrDefault(className);
}

// ── 頁面掃描 ──────────────────────────────

/// <summary>
/// 掃描 .cshtml 與對應的頁面模型。
/// </summary>
static class PageScanner
{
    private static readonly Regex PageDirective = new(@"^[ \t]*@page\b[ \t]*(?:""(?<template>[^""]*)"")?", RegexOptions.Multiline);
    private static readonly Regex ModelDirective = new(@"^[ \t]*@model\s+(?<type>[\w.]+)", RegexOptions.Multiline);
    private static readonly Regex AttributeList = new(@"^[ \t]*\[(?<body>[^\[\]\r\n]*)\]", RegexOptions.Multiline);
    private static readonly Regex AuthAttribute = new(
        @"(?<![\w.])(?<kind>Authorize|AllowAnonymous)(?:Attribute)?\b\s*(?:\((?<args>(?:[^()]|\([^()]*\))*)\))?");
    private static readonly Regex LeadingAttributes = new(@"^\s*\[[^\]]*\]", RegexOptions.Multiline);
    private static readonly Regex ClassName = new(@"\bclass\s+(?<name>\w+)");
    private static readonly Regex MethodName = new(@"(?<name>\w+)\s*(?:<[^>]*>)?\s*\(");
    private static readonly Regex HandlerDecl = new(
        @"\b(?:public|private|protected|internal)\b[^;{}=()]*?\b(?<name>On(?:Get|Post|Put|Delete|Patch)\w*)\s*\(");
    private static readonly Regex ResourceAuth = new(@"uthorizationService\s*\.\s*AuthorizeAsync\s*\(");
    private static readonly Regex ChallengeCall = new(@"\breturn\s+Challenge\s*\(");

    /// <summary>
    /// 從左括號之後讀到對應的右括號為止（不含括號本身）。
    /// </summary>
    /// <param name="code">原始碼</param>
    /// <param name="start">左括號之後的位置</param>
    /// <returns>括號內的文字</returns>
    private static string ReadBalanced(string code, int start)
    {
        var depth = 1;
        for (var i = start; i < code.Length; i++)
        {
            if (code[i] == '(')
            {
                depth++;
            }
            else if (code[i] == ')' && --depth == 0)
            {
                return code[start..i];
            }
        }
        return code[start..];
    }

    /// <summary>
    /// 掃描所有頁面。
    /// </summary>
    /// <param name="repoRoot">repo 根目錄</param>
    /// <param name="webRoot">Involver 專案資料夾</param>
    /// <param name="program">Program.cs 的授權規則</param>
    /// <param name="constants">常數表</param>
    /// <param name="classIndex">類別索引</param>
    /// <returns>依路由排序的頁面清單</returns>
    public static List<PageInfo> Scan(string repoRoot, string webRoot, ProgramConventions program, ConstantTable constants, ClassIndex classIndex)
    {
        var roots = new List<(string Dir, string? Area)> { (Path.Combine(webRoot, "Pages"), null) };
        var areasDir = Path.Combine(webRoot, "Areas");
        if (Directory.Exists(areasDir))
        {
            roots.AddRange(Directory.EnumerateDirectories(areasDir)
                .Select(d => (Path.Combine(d, "Pages"), (string?)Path.GetFileName(d)))
                .Where(r => Directory.Exists(r.Item1)));
        }

        var pages = new List<PageInfo>();
        foreach (var (dir, area) in roots)
        {
            if (!Directory.Exists(dir))
            {
                continue;
            }
            foreach (var file in Directory.EnumerateFiles(dir, "*.cshtml", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(dir, file).Replace('\\', '/');
                var segments = rel.Split('/');
                if (Path.GetFileName(file).StartsWith('_')
                    || segments.Any(s => s.Equals("Shared", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var markup = File.ReadAllText(file);
                var directive = PageDirective.Match(markup);
                if (!directive.Success)
                {
                    continue;
                }

                var viewPath = "/" + rel[..^".cshtml".Length];
                var template = directive.Groups["template"].Success ? directive.Groups["template"].Value : null;
                var modelType = ModelDirective.Match(markup) is { Success: true } mm ? mm.Groups["type"].Value.Split('.')[^1] : null;
                pages.Add(BuildPage(repoRoot, file, area, viewPath, template, modelType, program, constants, classIndex));
            }
        }
        return pages.OrderBy(p => p.Route, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static PageInfo BuildPage(string repoRoot, string file, string? area, string viewPath, string? template,
        string? modelType, ProgramConventions program, ConstantTable constants, ClassIndex classIndex)
    {
        var auth = new AuthInfo();
        var notes = new List<string>();
        var handlers = new List<string>();
        var resourceAuth = false;
        var usesChallenge = false;
        var inPagePolicies = new List<string>();

        if (program.GlobalRequiresLogin)
        {
            auth.RequiresLogin = true;
            auth.Sources.Add(program.GlobalSource);
        }

        var conventionSources = new List<string>();
        foreach (var rule in program.Rules.Where(r => r.Matches(area, viewPath)))
        {
            if (rule.AllowAnonymous)
            {
                auth.AnonymousSources.Add(rule.Source);
            }
            else
            {
                auth.RequiresLogin = true;
                if (rule.Policy is not null)
                {
                    auth.AddPolicy(rule.Policy);
                }
                conventionSources.Add(rule.Source);
            }
            auth.Sources.Add(rule.Source);
        }

        var codeBehind = file + ".cs";
        if (File.Exists(codeBehind))
        {
            var code = CSharpText.StripComments(File.ReadAllText(codeBehind));
            var pageClass = modelType ?? (ClassName.Match(code) is { Success: true } cm ? cm.Groups["name"].Value : null);

            ApplyAttributes(code, pageClass, auth, notes, constants, "");
            ApplyBaseClassAttributes(code, pageClass, auth, notes, constants, classIndex);

            handlers = HandlerDecl.Matches(code)
                .Select(m => m.Groups["name"].Value)
                .Select(n => n.EndsWith("Async", StringComparison.Ordinal) ? n[..^"Async".Length] : n)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            // AuthorizeAsync(User, resource, requirement) 是資源授權；AuthorizeAsync(User, policy) 是在頁面內檢查 policy
            foreach (Match call in ResourceAuth.Matches(code))
            {
                var callArgs = CSharpText.SplitTopLevel(ReadBalanced(code, call.Index + call.Length));
                if (callArgs.Count >= 3)
                {
                    resourceAuth = true;
                }
                else if (callArgs.Count == 2)
                {
                    var policy = constants.Resolve(callArgs[1]) ?? callArgs[1];
                    if (!inPagePolicies.Contains(policy, StringComparer.Ordinal))
                    {
                        inPagePolicies.Add(policy);
                    }
                }
            }
            usesChallenge = ChallengeCall.IsMatch(code);
        }
        else
        {
            notes.Add("沒有 .cshtml.cs");
        }

        if (resourceAuth)
        {
            notes.Add("資源授權（擁有者或管理角色）");
        }
        if (inPagePolicies.Count > 0)
        {
            notes.Add($"頁面內依 policy 決定顯示內容：{string.Join("、", inPagePolicies)}");
        }
        if (auth.AccessKind == "anonymous" && usesChallenge)
        {
            notes.Add("部分 handler 未登入時會 Challenge（導向登入）");
        }
        if (auth.AnonymousSources.Count > 0 && conventionSources.Count > 0)
        {
            notes.Add($"{string.Join("、", auth.AnonymousSources)} 蓋過 {string.Join("、", conventionSources)}");
        }
        else if (auth.AnonymousSources.Count == 0)
        {
            notes.AddRange(conventionSources);
        }

        return new PageInfo(
            Route: BuildRoute(area, viewPath, template),
            File: Path.GetRelativePath(repoRoot, file).Replace('\\', '/'),
            Area: area,
            Auth: auth,
            Handlers: handlers,
            ResourceAuthorization: resourceAuth,
            Notes: notes);
    }

    /// <summary>
    /// 依檔案路徑與 @page 範本推算路由。
    /// </summary>
    private static string BuildRoute(string? area, string viewPath, string? template)
    {
        if (template is not null && (template.StartsWith('/') || template.StartsWith("~/", StringComparison.Ordinal)))
        {
            return "/" + template.TrimStart('~', '/');
        }

        var segments = viewPath.Trim('/').Split('/').ToList();
        if (segments[^1].Equals("Index", StringComparison.OrdinalIgnoreCase))
        {
            segments.RemoveAt(segments.Count - 1);
        }
        if (area is not null)
        {
            segments.Insert(0, area);
        }
        if (!string.IsNullOrEmpty(template))
        {
            segments.Add(template);
        }
        return "/" + string.Join('/', segments);
    }

    /// <summary>
    /// 讀取原始碼中的 [Authorize] 與 [AllowAnonymous]：頁面類別上的會套用，handler 方法上的只記在備註。
    /// </summary>
    private static void ApplyAttributes(string code, string? pageClass, AuthInfo auth, List<string> notes, ConstantTable constants, string sourcePrefix)
    {
        foreach (Match list in AttributeList.Matches(code))
        {
            var attributes = AuthAttribute.Matches(list.Groups["body"].Value);
            if (attributes.Count == 0)
            {
                continue;
            }

            // 找出屬性套用的對象：之後到第一個 { ; => 之前的宣告
            var rest = code[(list.Index + list.Length)..];
            var end = rest.IndexOfAny(['{', ';']);
            var arrow = rest.IndexOf("=>", StringComparison.Ordinal);
            if (arrow >= 0 && (end < 0 || arrow < end))
            {
                end = arrow;
            }
            var declaration = LeadingAttributes.Replace(end >= 0 ? rest[..end] : rest, "");

            var classMatch = ClassName.Match(declaration);
            string? handler = null;
            if (classMatch.Success)
            {
                if (pageClass is not null && classMatch.Groups["name"].Value != pageClass)
                {
                    continue;
                }
            }
            else if (MethodName.Match(declaration) is { Success: true } method && method.Groups["name"].Value.StartsWith("On", StringComparison.Ordinal))
            {
                handler = method.Groups["name"].Value;
            }
            else
            {
                continue;
            }

            foreach (Match attribute in attributes)
            {
                var text = $"{sourcePrefix}[{attribute.Value.Trim()}]";
                if (handler is not null)
                {
                    notes.Add($"{handler} 標了 {text}（Razor Pages 不支援 handler 層級授權，不會生效）");
                    continue;
                }
                auth.Sources.Add(text);
                if (attribute.Groups["kind"].Value == "AllowAnonymous")
                {
                    auth.AnonymousSources.Add(text);
                    continue;
                }

                auth.RequiresLogin = true;
                if (!attribute.Groups["args"].Success)
                {
                    continue;
                }
                foreach (var arg in CSharpText.SplitTopLevel(attribute.Groups["args"].Value))
                {
                    var named = Regex.Match(arg, @"^(?<name>\w+)\s*=\s*(?<value>.+)$", RegexOptions.Singleline);
                    var name = named.Success ? named.Groups["name"].Value : "Policy";
                    var value = named.Success ? named.Groups["value"].Value : arg;
                    switch (name)
                    {
                        case "Policy":
                            auth.AddPolicy(constants.Resolve(value) ?? value.Trim());
                            break;
                        case "Roles":
                            var roles = (constants.Resolve(value) ?? value.Trim())
                                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .ToList();
                            auth.RoleSets.Add(roles);
                            break;
                        case "AuthenticationSchemes":
                            notes.Add($"指定驗證方式：{constants.Resolve(value) ?? value.Trim()}");
                            break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 沿著頁面模型的基底類別往上找 [Authorize]／[AllowAnonymous]（只認得 Involver 專案內的類別）。
    /// </summary>
    private static void ApplyBaseClassAttributes(string code, string? pageClass, AuthInfo auth, List<string> notes, ConstantTable constants, ClassIndex classIndex)
    {
        var current = pageClass;
        var currentCode = code;
        var visited = new HashSet<string>(StringComparer.Ordinal);
        for (var depth = 0; depth < 6 && current is not null; depth++)
        {
            var baseMatch = Regex.Match(currentCode,
                $@"\bclass\s+{Regex.Escape(current)}\b(?:\s*<[^>]*>)?(?:\s*\((?:[^()]|\([^()]*\))*\))?\s*:\s*(?<base>\w+)");
            if (!baseMatch.Success)
            {
                return;
            }
            var baseName = baseMatch.Groups["base"].Value;
            var baseFile = classIndex.FileOf(baseName);
            if (baseFile is null || !visited.Add(baseName))
            {
                return;
            }
            currentCode = CSharpText.StripComments(File.ReadAllText(baseFile));
            ApplyAttributes(currentCode, baseName, auth, notes, constants, $"{baseName} ");
            current = baseName;
        }
    }
}

// ── 資料模型 ──────────────────────────────

/// <summary>
/// 一個頁面的掃描結果。
/// </summary>
/// <param name="Route">路由（例如 /Novels/Details/{id}）</param>
/// <param name="File">.cshtml 相對於 repo 根目錄的路徑</param>
/// <param name="Area">Area 名稱；非 Area 頁面為 null</param>
/// <param name="Auth">授權需求</param>
/// <param name="Handlers">handler 方法名稱（已去掉 Async）</param>
/// <param name="ResourceAuthorization">頁面內是否呼叫 AuthorizationService.AuthorizeAsync</param>
/// <param name="Notes">備註</param>
sealed record PageInfo(string Route, string File, string? Area, AuthInfo Auth, List<string> Handlers, bool ResourceAuthorization, List<string> Notes);

/// <summary>
/// 一個頁面合併後的授權需求。
/// </summary>
sealed class AuthInfo
{
    /// <summary>
    /// 是否需要登入（任一授權規則或屬性都會設為 true）
    /// </summary>
    public bool RequiresLogin { get; set; }

    /// <summary>
    /// 開放匿名的來源；有任何一個就不需登入
    /// </summary>
    public List<string> AnonymousSources { get; } = [];

    /// <summary>
    /// 需要同時滿足的 policy
    /// </summary>
    public List<string> Policies { get; } = [];

    /// <summary>
    /// 需要的角色：每組內任一即可，各組都要滿足
    /// </summary>
    public List<List<string>> RoleSets { get; } = [];

    /// <summary>
    /// 授權設定的來源
    /// </summary>
    public List<string> Sources { get; } = [];

    /// <summary>
    /// 種類：anonymous、authenticated、policy、role
    /// </summary>
    public string AccessKind =>
        AnonymousSources.Count > 0 || !RequiresLogin ? "anonymous"
        : Policies.Count > 0 ? "policy"
        : RoleSets.Count > 0 ? "role"
        : "authenticated";

    /// <summary>
    /// 表格顯示用的授權說明
    /// </summary>
    public string Display
    {
        get
        {
            if (AccessKind == "anonymous")
            {
                return "匿名";
            }
            var parts = new List<string>();
            if (Policies.Count > 0)
            {
                parts.Add($"Policy {string.Join(" + ", Policies)}");
            }
            parts.AddRange(RoleSets.Select(set => $"角色 {string.Join("／", set)}"));
            return parts.Count == 0 ? "登入" : string.Join("；", parts);
        }
    }

    /// <summary>
    /// 加入 policy（重複的略過）。
    /// </summary>
    /// <param name="policy">policy 名稱</param>
    public void AddPolicy(string policy)
    {
        if (!Policies.Contains(policy, StringComparer.Ordinal))
        {
            Policies.Add(policy);
        }
    }
}
