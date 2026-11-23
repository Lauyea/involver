---
type: llm
focus: trace
weight: 1
---

切換追蹤改用 POST／PUT／DELETE 並驗證 anti-forgery token（[ValidateAntiForgeryToken]），JS 送出 Program.cs 設定的 X-CSRF-TOKEN header；沒有沿用會改變資料的 GET
