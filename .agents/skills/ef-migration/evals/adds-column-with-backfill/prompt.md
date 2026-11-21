---
tags: [skill-creator]
max_turns: 80
timeout_seconds: 2400
allowed_tools: [Read, Glob, Grep, Skill, Write, Edit, Bash]
---

幫 Novel 加一個 `EpisodeCount` 欄位（int，預設 0），記錄沒被刪除的章節數。migration 裡要把既有小說的數字回填好。這次只要欄位和回填，維護數字的程式之後再做。
