#:property TargetFramework=net10.0
#:property PublishAot=false

// 共用 Agent Skills 的設定與檢查。Skills 放在 .agents/skills（Antigravity、Gemini CLI、Codex 等會直接讀），
// Claude Code 只讀 .claude/skills，所以在本機建一個 junction 指過去（.claude/skills 已加進 .gitignore）。
//
// 用法（在儲存庫任一位置執行，需要 .NET 10 SDK）：
//   dotnet run skills.cs              建立 .claude/skills → .agents/skills 的 junction（已存在就略過），再檢查所有 skill
//   dotnet run skills.cs -- check     只檢查：frontmatter 格式、name 與資料夾同名、description 長度、內文提到的檔案與 Tools 是否存在
//   dotnet run skills.cs -- list      列出所有 skill 的名稱與描述
//
// clone 下來或換電腦後執行一次即可；之後新增或修改 .agents/skills 底下的檔案，Claude Code 會直接看到。

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;


Console.OutputEncoding = Encoding.UTF8;

var root = FindRepoRoot();
if (root is null)
{
    Console.Error.WriteLine("找不到 Involver.sln，請在儲存庫內執行。");
    return 2;
}

var skillsDir = Path.Combine(root, ".agents", "skills");
var linkPath = Path.Combine(root, ".claude", "skills");
var mode = args.FirstOrDefault() ?? "link";

if (!Directory.Exists(skillsDir))
{
    Console.Error.WriteLine($"找不到 {skillsDir}。");
    return 2;
}

switch (mode)
{
    case "link":
        var linkResult = EnsureLink();
        return linkResult != 0 ? linkResult : Check();
    case "check":
        return Check();
    case "list":
        foreach (var skill in LoadSkills())
        {
            Console.WriteLine($"{skill.Name,-22} {skill.Description}");
        }

        return 0;
    default:
        Console.WriteLine("用法：dotnet run skills.cs -- [link | check | list]");
        return 2;
}

// ── junction ──────────────────────────────

int EnsureLink()
{
    var info = new DirectoryInfo(linkPath);
    if (info.Exists && info.LinkTarget is not null)
    {
        var target = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(linkPath)!, info.LinkTarget)).TrimEnd('\\', '/');
        if (string.Equals(target, skillsDir, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"已存在：.claude/skills → .agents/skills");
            return 0;
        }

        Console.WriteLine($".claude/skills 目前指向 {target}，改為指向 .agents/skills");
        info.Delete();
    }
    else if (info.Exists)
    {
        Console.Error.WriteLine(".claude/skills 是一般資料夾，不是連結。請先把裡面的 skill 搬到 .agents/skills，刪除資料夾後再執行。");
        return 1;
    }

    Directory.CreateDirectory(Path.GetDirectoryName(linkPath)!);
    if (OperatingSystem.IsWindows())
    {
        // junction 不需要系統管理員權限或開發人員模式
        var process = Process.Start(new ProcessStartInfo("cmd", $"/c mklink /J \"{linkPath}\" \"{skillsDir}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        })!;
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            Console.Error.WriteLine($"建立 junction 失敗：{process.StandardError.ReadToEnd()}");
            return 1;
        }
    }
    else
    {
        Directory.CreateSymbolicLink(linkPath, Path.Combine("..", ".agents", "skills"));
    }

    Console.WriteLine("已建立：.claude/skills → .agents/skills");
    return 0;
}

// ── 檢查 ──────────────────────────────

int Check()
{
    var problems = new List<string>();
    var skills = LoadSkills();
    foreach (var skill in skills)
    {
        var where = $".agents/skills/{skill.Folder}/SKILL.md";
        if (skill.Name is null || skill.Description is null)
        {
            problems.Add($"{where}：缺少 frontmatter 的 name 或 description");
            continue;
        }

        if (skill.Name != skill.Folder)
        {
            problems.Add($"{where}：name「{skill.Name}」要和資料夾名稱相同");
        }

        if (!Regex.IsMatch(skill.Name, "^[a-z0-9]+(-[a-z0-9]+)*$") || skill.Name.Length > 64)
        {
            problems.Add($"{where}：name 只能用小寫英數字與連字號，最多 64 字元");
        }

        if (skill.Description.Length > 1024)
        {
            problems.Add($"{where}：description 超過 1024 字元（{skill.Description.Length}）");
        }

        // 內文提到的相對檔案（Markdown 連結）與儲存庫路徑（Tools/...、.claude/... 等）都要存在
        foreach (Match link in Regex.Matches(skill.Body, @"\]\((?!https?:|#)([^)\s]+)\)"))
        {
            var path = Path.GetFullPath(Path.Combine(skillsDir, skill.Folder, link.Groups[1].Value.Split('#')[0]));
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                problems.Add($"{where}：連結的檔案不存在 {link.Groups[1].Value}");
            }
        }

        foreach (Match reference in Regex.Matches(skill.Body, @"`((?:Tools|\.claude|\.agents|\.github|Involver|DataAccess|InvolverTest|InvolverRoutineWork)/[^`*{}<>\s]+\.(?:cs|js|mjs|json|yml|md|cshtml|css))`"))
        {
            if (!File.Exists(Path.Combine(root, reference.Groups[1].Value)))
            {
                problems.Add($"{where}：提到的檔案不存在 {reference.Groups[1].Value}");
            }
        }
    }

    foreach (var problem in problems)
    {
        Console.WriteLine(problem);
    }

    Console.WriteLine(problems.Count == 0 ? $"通過：{skills.Count} 個 skill。" : $"共 {problems.Count} 個問題。");
    return problems.Count == 0 ? 0 : 1;
}

List<Skill> LoadSkills() => Directory.GetDirectories(skillsDir)
    .Where(d => File.Exists(Path.Combine(d, "SKILL.md")))
    .Select(d =>
    {
        var text = File.ReadAllText(Path.Combine(d, "SKILL.md")).Replace("\r\n", "\n");
        var folder = Path.GetFileName(d);
        var match = Regex.Match(text, @"^---\n(?<front>.*?)\n---\n(?<body>.*)$", RegexOptions.Singleline);
        if (!match.Success)
        {
            return new Skill(folder, null, null, text);
        }

        var front = match.Groups["front"].Value;
        var name = Regex.Match(front, @"^name:\s*(.+)$", RegexOptions.Multiline).Groups[1].Value.Trim();
        var description = ReadDescription(front);
        return new Skill(folder, name.Length > 0 ? name : null, description, match.Groups["body"].Value);
    })
    .OrderBy(s => s.Folder, StringComparer.Ordinal)
    .ToList();

// description 可能是單行，或 YAML 的 > / | 多行區塊
static string? ReadDescription(string front)
{
    var lines = front.Split('\n');
    var index = Array.FindIndex(lines, l => l.StartsWith("description:"));
    if (index < 0)
    {
        return null;
    }

    var first = lines[index]["description:".Length..].Trim();
    if (first is not (">" or "|" or ">-" or "|-"))
    {
        return first.Trim('"', '\'');
    }

    var block = lines.Skip(index + 1).TakeWhile(l => l.StartsWith(' ') || l.Length == 0).Select(l => l.Trim());
    return string.Join(' ', block).Trim();
}

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

/// <summary>
/// 一個 skill 的內容
/// </summary>
/// <param name="Folder">資料夾名稱</param>
/// <param name="Name">frontmatter 的 name</param>
/// <param name="Description">frontmatter 的 description</param>
/// <param name="Body">frontmatter 之後的內文</param>
record Skill(string Folder, string? Name, string? Description, string Body);
