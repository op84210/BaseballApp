# Render 部署完整指南

## 📋 前置準備

1. **GitHub 帳號**：將程式碼推到 GitHub
2. **Render 帳號**：免費註冊 https://render.com

## 🚀 部署步驟

### 步驟 1：推送程式碼到 GitHub

```powershell
# 在專案根目錄執行
git add .
git commit -m "Add Render deployment files"
git push origin master
```

### 步驟 2：在 Render 建立專案

1. 登入 [Render Dashboard](https://dashboard.render.com)
2. New → Blueprint
3. 連接你的 GitHub repo
   - 選擇 BaseballApp repository
   - Authorize Render 存取權限
4. 點擊 Deploy
   - Render 會讀取 `render.yaml` 自動建立 PostgreSQL + Web Service

### 步驟 3：等待部署完成

- Postgres 建立需要 1-2 分鐘
- Web 應用建置需要 3-5 分鐘
- 首次部署可能需要 10-15 分鐘

**查看部署狀態：** Dashboard → 你的 Service → Deployments

### 步驟 4：檢查環境變數

部署後，Render 會自動：
- 建立 `baseball-db` PostgreSQL
- 注入 `ConnectionStrings__PostgreSQL` 連線字串
- 設定 `DatabaseType=PostgreSQL`

**驗證方法：**
1. Dashboard → baseball-app Service
2. Environment 頁籤 → 檢查所有變數是否正確

### 步驟 5：資料庫初始化

**方法 A：透過 EF Core Migrations（推薦）**

1. 進入 baseball-app Service → Shell
2. 執行以下指令建立表結構：
```bash
dotnet ef database update --connection "$ConnectionStrings__PostgreSQL"
```

**方法 B：透過 Render 的 PostgreSQL Console**

1. 進入 baseball-db Service → Data
2. 在 Query 中執行建表 SQL（若有 migration script）

### 步驟 6：測試應用

1. Dashboard → baseball-app → 複製公開 URL
2. 在瀏覽器打開：`https://your-app.onrender.com`

## 💾 資料移轉（SQLite → PostgreSQL）

若要從本機 SQLite 遷移資料：

### 方法 1：用 pgloader（推薦，自動轉換）

**在本機安裝 pgloader：**
```powershell
# Windows 可下載可執行檔或用 WSL/WSL2
# 或用 Docker
docker run --rm -v C:\path\to\data:/data pgloader sqlite:///data/baseball.db postgresql://user:pass@host/db
```

### 方法 2：手動 SQL dump + 還原

```powershell
# 1. 在本機產生 SQLite dump
# （需先安裝 sqlite3.exe）
sqlite3 data/baseball.db .dump > dump.sql

# 2. 在 Render PostgreSQL Console 執行 SQL
# 移除 SQLite 特有語法後上傳
```

### 方法 3：用 .NET 工具（最簡單）

在 Render Shell 中執行：
```bash
# 建立臨時 migration + EF Core 自動建表
dotnet ef database update
```
然後需在本機執行資料複製（需要訪問本機 SQLite 檔）。

## 🔗 連線字串格式

Render PostgreSQL 會自動提供格式：
```
postgresql://user:password@host:port/database
```

EF Core Npgsql 需要的格式：
```
Host=host;Port=port;Username=user;Password=password;Database=database;SSL Mode=Require;Trust Server Certificate=true
```

Render 的 `render.yaml` 中 `fromDatabase` 會自動轉換為正確格式。

## 📊 監控與日誌

### 查看應用日誌
1. Dashboard → baseball-app
2. Logs 頁籤 → 即時日誌
3. 或 Deployments → 點選部署 → View Logs

### 查看資料庫狀態
1. Dashboard → baseball-db
2. Info 頁籤 → 查看連線資訊
3. Data 頁籤 → 執行查詢測試

## ⚠️ 常見問題

### 1. 應用無法連接資料庫
**症狀：** `Connection timeout` 或 `host not found`
**解決：**
- 檢查 `ConnectionStrings__PostgreSQL` 是否正確
- 確認 `DatabaseType=PostgreSQL`
- 在 Render Shell 測試連接：`psql "$ConnectionStrings__PostgreSQL"`

### 2. 冷啟動太慢
**症狀：** 首次請求需要 30-50 秒
**原因：** 免費層會休眠，是正常現象
**解決：** 無法避免，但可用 ping 服務保活（如 Uptime Robot）

### 3. 資料表不存在
**症狀：** 404 或表名錯誤
**解決：**
- 進入 Render Shell 確認資料表已建立：`\dt`
- 或重新執行 migration：`dotnet ef database update`

### 4. 埠號錯誤
**症狀：** `Address already in use` 或無法連線
**解決：** Render 固定用 `10000`，Dockerfile 已設定，勿修改

## 💰 成本預估

**Render Free Tier（免費）：**
- Web Service：0.1 vCPU、512MB RAM、無流量限制
- PostgreSQL：256MB、無流量限制
- 缺點：30 分鐘無請求會自動休眠（冷啟動）

**何時升級付費：**
- 需要固定運行（不休眠）
- 需要更多 DB 空間（>256MB）
- 需要 backups/monitoring

## 🔄 後續更新

每次推送到 GitHub 時，Render 會自動重新部署：

```powershell
git add .
git commit -m "Update features"
git push origin master
# Render 自動構建並部署
```

## 📞 支援

- Render 文檔：https://render.com/docs
- .NET on Render：https://render.com/docs/deploy-dotnet
