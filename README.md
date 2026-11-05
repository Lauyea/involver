# involver
關於 involver.tw 這個互動型創作平台的原始碼

---

## 安裝與開發

專案用 Docker 容器啟動（Linux Container）。本機開發需要準備：

* [Docker Desktop](https://www.docker.com/products/docker-desktop/)（使用 Linux 容器模式）
* Visual Studio 2026（含「ASP.NET 與網頁程式開發」工作負載）或 .NET 10 SDK
* SQL Server Express（安裝在本機，由容器透過 `host.docker.internal` 連線）

### 設定 SQL Server Express

容器內無法使用 Windows 驗證與 `localhost\SQLEXPRESS` 這類連線字串，因此需要調整本機的 SQL Server Express：

1. 啟用 SQL Server 驗證：在 SSMS 對伺服器按右鍵 →「屬性」→「安全性」，選擇「SQL Server 及 Windows 驗證模式」，並重新啟動 SQL Server 服務。
2. 建立 SQL 登入身分：先依下方「DB schema」建立 `Involver` 資料庫，再於「安全性」→「登入」新增登入 `dbuser`，密碼 `password1234`，並在「使用者對應」勾選 `Involver` 資料庫，給予 `db_datareader` 與 `db_datawriter` 角色。
3. 啟用 TCP/IP：在「SQL Server 組態管理員」→「SQL Server 網路組態」→「SQLEXPRESS 的通訊協定」啟用 TCP/IP，並在「IP 位址」→「IPAll」把「TCP 通訊埠」設為 `1433`（清空「TCP 動態通訊埠」），再重新啟動 SQL Server 服務。
4. 確認 Windows 防火牆允許 TCP 1433 的連入連線。

上述帳號密碼對應 [launchSettings.json](./Involver/Properties/launchSettings.json) 中 `Container (Dockerfile)` 設定檔的 `ConnectionStrings__ConnectionStrings` 環境變數，若要使用其他帳號密碼，請一併修改該處。

### 啟動專案

用 Visual Studio 開啟 `Involver.sln`，將 `Involver` 設為啟動專案，選擇 `Container (Dockerfile)` 設定檔後按 F5 即可。Visual Studio 會依照 [Dockerfile](./Involver/Dockerfile) 建置映像並啟動容器（HTTP 8080 / HTTPS 8081）。

不使用 Visual Studio 時，也可以在方案根目錄用 CLI 建置與執行（建置內容為整個方案資料夾）：

```bash
docker build -f Involver/Dockerfile -t involver .
```

```bash
docker run --rm -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development -e "ConnectionStrings__ConnectionStrings=Server=host.docker.internal;Database=Involver;User Id=dbuser;Password=password1234;TrustServerCertificate=true;MultipleActiveResultSets=true" involver
```

之後開啟 <http://localhost:8080>。

Application Insights、Google / Facebook 登入等外部服務在沒有設定 Secrets 時會自動略過，不影響本機開發。

### DB schema

用 CLI 或是 Visual Studio，從 DataAccess 這個 library 去建立 DB。EF Core 工具是在本機（容器外）執行，可以直接用 Windows 驗證連線。`dotnet-ef` 的版本記錄在 [dotnet-tools.json](./dotnet-tools.json)，第一次使用前先還原：

```bash
dotnet tool restore
```

```bash
dotnet ef database update --project DataAccess --startup-project Involver --connection "Server=.\SQLEXPRESS;Database=Involver;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true"
```

可以[參考這裡](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/?tabs=dotnet-core-cli#create-your-database-and-schema)。

## 部署

正式環境用 GitHub Actions 手動部署，設定在 [deploy.yml](./.github/workflows/deploy.yml)。

1. 到 GitHub 的 **Actions** →「Deploy to production」→「Run workflow」，分支選 `master`。
2. 勾選這次要做的事：
   * 套用資料庫 migration：有 schema 變更時才勾。
   * 部署網站、部署排程工作 (Azure Functions)：預設都會部署。
3. 等 build 與測試跑完，執行會停在「Deploy」等待核准。有勾 migration 的話，可以先在這次執行頁面底部的 Artifacts 下載 `migration-sql` 檢查 SQL。
4. 按「Review deployments」核准後，會依序：建置並推送 image 到 ACR → 套用 migration → 切換網站 image → 部署 Functions。

### Migration 注意事項

* 產生的 SQL 是 idempotent 的，只會補跑正式 DB 還沒套用的 migration，重複執行也不會出錯。
* Migration 會在網站切換新版之前套用，所以舊版網站會短暫使用新的 schema。新增資料表或可為 null 的欄位沒有問題；更名或刪除欄位要拆成兩次部署：先上不再使用舊欄位的程式，下一次再刪除。
* 連線字串直接從網站 App Service 的 Connection strings 讀取，不另外存在 GitHub。

### 復原

* 網站：每次部署的 image 以 commit SHA 為 tag 保留在 ACR，可以在 Azure Portal 網站 App Service 的「部署中心」改回前一個 tag。
* 資料庫：使用 Azure SQL 的還原時間點 (Point-in-Time Restore)。

## Coding Style

[參考這裡](./AGENTS.md)。
