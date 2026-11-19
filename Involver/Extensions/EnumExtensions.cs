using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Involver.Extensions;

/// <summary>
/// 列舉的擴充方法
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// 取得列舉值的 [Display(Name)]；沒有設定時回傳列舉名稱
    /// </summary>
    /// <param name="value">列舉值</param>
    /// <returns>顯示名稱</returns>
    public static string GetDisplayName(this Enum value)
    {
        var name = value.ToString();
        var member = value.GetType().GetField(name);
        return member?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? name;
    }
}
