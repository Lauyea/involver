using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Common
{
    // Voting enums
    /// <summary>
    /// 投票政策。
    /// 自由模式可以自由選擇投票的價值。平等模式每張選票價值相同。
    /// </summary>
    public enum PolicyType
    {
        Equality, Liberty
    }
    /// <summary>
    /// 是否為競標模式。
    /// 競標模式的話，可以出錢設置選項。
    /// </summary>
    public enum ModeType
    {
        Normal, Bidding
    }
    /// <summary>
    /// 投票截止的依據。
    /// 限時：有限定時間內才能投票。
    /// 限量：有限定投票數量。
    /// 限值：有限定數值的投票。
    /// </summary>
    public enum LimitType
    {
        Time, Number, Value
    }

    // Novel Enums
    public enum Type
    {
        Fantasy,
        History,
        Love,
        Real,
        Modern,
        Science,
        Horror,
        Detective
    }
}