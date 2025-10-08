# involver
關於 involver.tw 這個互動型創作平台的原始碼

---

## 安裝與開發

Clone 方案以後，需要安裝 SQL server Express。

然後下載 appsettings.Development.json，放至「Involver」這個資料夾裡。

[appsettings.Development.json](https://github.com/user-attachments/files/21649896/appsettings.Development.json)

這樣資料庫與連線字串就設定好了。

### DB schema

用 CLI 或是 Visual studio，從 DataAccess 這個 library 去建立DB。

可以[參考這裡](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/?tabs=dotnet-core-cli#create-your-database-and-schema)。

## Coding Style

[參考這裡](./.gemini/GEMINI.md)。
