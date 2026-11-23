---
type: llm
focus: trace
weight: 1
---

JS 放在 Involver/wwwroot/js/components/，用 import { createApp } from 'vue'，沒有 window.Vue，Options 屬性順序為 data → watch → computed → mounted → methods
