---
tags: [skill-creator]
max_turns: 100
timeout_seconds: 2400
allowed_tools: [Read, Glob, Grep, Skill, Write, Edit, Bash]
---

小說詳細頁跟個人頁的追蹤按鈕還是 social-actions.js 裡的全域函式，onclick 直接寫在 HTML 上。改成一個 Vue 的 View Component，兩個頁面共用，按鈕旁邊順便顯示目前的追蹤人數。
