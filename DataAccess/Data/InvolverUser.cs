using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity;

namespace DataAccess.Data
{
    /// <summary>
    /// 登入帳號。會員（Prime）與封鎖（Banned）狀態以 Profiles 為唯一來源，不存在這裡。
    /// 銀行帳號等敏感資料在分潤功能上線、有加密設計之前不儲存。
    /// </summary>
    public class InvolverUser : IdentityUser
    {
    }
}