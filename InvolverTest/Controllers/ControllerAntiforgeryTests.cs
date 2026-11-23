using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Involver.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

using NUnit.Framework;

namespace InvolverTest.Controllers
{
    /// <summary>
    /// 檢查所有 API controller 的 anti-forgery 設定：會改資料的 action 都要驗證 token，
    /// 不驗證的必須列在白名單並說明原因；GET 不能被要求帶 token
    /// </summary>
    [TestFixture]
    public class ControllerAntiforgeryTests
    {
        /// <summary>
        /// 刻意不驗證 anti-forgery token 的 action（controller.action）與原因
        /// </summary>
        private static readonly Dictionary<string, string> IgnoredActions = new()
        {
            ["CheckOutFeedbackController.PostCheckOutFeedbackAsync"] = "綠界伺服器回呼，沒有使用者的 token",
            ["PreviewsApiController.GetContentAsync"] = "以閱讀密碼保護，讀者多半沒有登入",
            ["PreviewsApiController.RevokeAsync"] = "以撤銷密碼保護，讀者多半沒有登入",
            ["ViewsController.GetBatchArticleViews"] = "唯讀查詢，只是用 POST 帶 ID 陣列",
            ["ViewsController.GetBatchNovelViews"] = "唯讀查詢，只是用 POST 帶 ID 陣列",
        };

        private static readonly string[] SafeMethods = ["GET", "HEAD", "OPTIONS", "TRACE"];

        /// <summary>
        /// 所有 controller 的 action，以及它接受的 HTTP 方法（沒有標示時視為接受所有方法）
        /// </summary>
        private static IEnumerable<(Type Controller, MethodInfo Action, string[] HttpMethods)> GetActions()
        {
            var controllers = typeof(FollowsApiController).Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t));

            foreach (var controller in controllers)
            {
                var actions = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(m => !m.IsSpecialName && m.GetCustomAttribute<NonActionAttribute>() == null);

                foreach (var action in actions)
                {
                    var httpMethods = action.GetCustomAttributes<HttpMethodAttribute>()
                        .SelectMany(a => a.HttpMethods)
                        .ToArray();
                    yield return (controller, action, httpMethods);
                }
            }
        }

        /// <summary>
        /// 取得 action 實際生效的 anti-forgery 設定：action 上的優先，其次是 controller 上的
        /// </summary>
        private static Attribute? GetEffectivePolicy(Type controller, MethodInfo action)
        {
            static Attribute? Find(MemberInfo member) => member.GetCustomAttributes(inherit: true)
                .OfType<Attribute>()
                .FirstOrDefault(a => a is ValidateAntiForgeryTokenAttribute
                    or AutoValidateAntiforgeryTokenAttribute
                    or IgnoreAntiforgeryTokenAttribute);

            return Find(action) ?? Find(controller);
        }

        private static string NameOf(Type controller, MethodInfo action) => $"{controller.Name}.{action.Name}";

        [Test]
        public void MutatingActions_InAllControllers_ValidateTokenOrAreAllowlisted()
        {
            var problems = new List<string>();

            foreach (var (controller, action, httpMethods) in GetActions())
            {
                var isMutating = httpMethods.Length == 0 || httpMethods.Any(m => !SafeMethods.Contains(m));
                if (!isMutating)
                {
                    continue;
                }

                var name = NameOf(controller, action);
                switch (GetEffectivePolicy(controller, action))
                {
                    case ValidateAntiForgeryTokenAttribute or AutoValidateAntiforgeryTokenAttribute:
                        break;
                    case IgnoreAntiforgeryTokenAttribute when IgnoredActions.ContainsKey(name):
                        break;
                    case IgnoreAntiforgeryTokenAttribute:
                        problems.Add($"{name}：標了 [IgnoreAntiforgeryToken]，但不在白名單");
                        break;
                    default:
                        problems.Add($"{name}：沒有驗證 anti-forgery token（在 controller 加上 [AutoValidateAntiforgeryToken]）");
                        break;
                }
            }

            Assert.That(problems, Is.Empty);
        }

        [Test]
        public void GetActions_InAllControllers_DoNotRequireToken()
        {
            // [ValidateAntiForgeryToken] 連 GET 也會驗證；controller 有 GET 時要用 [AutoValidateAntiforgeryToken]
            var problems = GetActions()
                .Where(a => a.HttpMethods.Contains("GET"))
                .Where(a => GetEffectivePolicy(a.Controller, a.Action) is ValidateAntiForgeryTokenAttribute)
                .Select(a => NameOf(a.Controller, a.Action))
                .ToList();

            Assert.That(problems, Is.Empty);
        }

        [Test]
        public void IgnoredActions_Allowlist_MatchesExistingActions()
        {
            var ignored = GetActions()
                .Where(a => GetEffectivePolicy(a.Controller, a.Action) is IgnoreAntiforgeryTokenAttribute)
                .Where(a => a.HttpMethods.Length == 0 || a.HttpMethods.Any(m => !SafeMethods.Contains(m)))
                .Select(a => NameOf(a.Controller, a.Action))
                .ToHashSet();

            Assert.That(IgnoredActions.Keys, Is.EquivalentTo(ignored));
        }
    }
}
