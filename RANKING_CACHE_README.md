# 排行榜快取系統

## 概述

為了提升排行榜頁面的效能，避免每次都重新計算所有球員的統計數據，我們建立了排行榜快取系統。

## 系統架構

### 1. 資料表結構

#### tblBattingRankingCache (打者排行榜快取)
- 儲存預先計算好的打者排行榜資料
- 包含排名、統計數據（PA, AB, H, HR, RBI, AVG, OBP, SLG, OPS 等）
- 索引：(seasonId, playerId) 唯一索引、(seasonId, rank) 索引

#### tblPitchingRankingCache (投手排行榜快取)
- 儲存預先計算好的投手排行榜資料
- 包含排名、統計數據（IP, H, HR, BB, SO, ERA, WHIP 等）
- 索引：(seasonId, playerId) 唯一索引、(seasonId, rank) 索引

### 2. 核心服務

#### RankingCacheService
提供以下功能：
- `UpdateBattingRankingsAsync(seasonId)` - 更新指定賽季的打者排行榜快取
- `UpdatePitchingRankingsAsync(seasonId)` - 更新指定賽季的投手排行榜快取
- `UpdateAllRankingsAsync()` - 更新所有賽季的排行榜快取
- `GetBattingRankingsFromCacheAsync(seasonId, minQualifiedPA)` - 從快取讀取打者排行榜
- `GetPitchingRankingsFromCacheAsync(seasonId, minQualifiedIP)` - 從快取讀取投手排行榜
- `IsCacheStaleAsync(seasonId, hoursThreshold)` - 檢查快取是否過期

### 3. 背景服務

#### RankingCacheUpdateService
- 定期自動更新排行榜快取
- 預設每天凌晨 3 點執行
- 更新間隔可透過 `appsettings.json` 配置

### 4. API 端點

#### RankingCacheController
提供手動觸發快取更新的 API：
- `POST /api/rankingcache/batting/{seasonId}` - 更新指定賽季的打者排行榜
- `POST /api/rankingcache/pitching/{seasonId}` - 更新指定賽季的投手排行榜
- `POST /api/rankingcache/all` - 更新所有賽季的排行榜
- `GET /api/rankingcache/status/{seasonId}` - 檢查快取狀態

## 使用方式

### Controller 端使用快取

`BaseballController.Rankings()` 方法已整合快取機制：

1. **優先使用快取**：先檢查快取是否存在且未過期
2. **快取未過期**：直接從快取讀取資料（快速）
3. **快取過期或不存在**：
   - 立即重新計算並回傳結果給使用者
   - 背景更新快取（不阻塞使用者）

### 配置選項 (appsettings.json)

```json
{
  "RankingCache": {
    "UpdateIntervalHours": 24,      // 背景服務更新間隔（小時）
    "StaleThresholdHours": 24       // 快取過期門檻（小時）
  }
}
```

## 資料庫遷移步驟

### 1. 建立快取資料表

資料表已經透過初始化工具建立完成。如果需要重新建立，可以執行：

```powershell
cd c:\Users\kwlin\Desktop\ideas\BaseballApp\tools\InitRankingCache
dotnet run
```

啟動應用程式後，可以透過 API 手動觸發首次快取建立：

```powershell
# 更新所有賽季的排行榜快取
Invoke-RestMethod -Uri "http://localhost:5000/api/rankingcache/all" -Method POST
```

或針對特定賽季：

```powershell
# 更新 2024 賽季打者排行榜
Invoke-RestMethod -Uri "http://localhost:5000/api/rankingcache/batting/CPBL-2024-HE" -Method POST

# 更新 2024 賽季投手排行榜
Invoke-RestMethod -Uri "http://localhost:5000/api/rankingcache/pitching/CPBL-2024-HE" -Method POST
```

## 效能優勢

### 無快取（原始方式）
- 每次載入排行榜頁面都需要：
  - 讀取所有球員資料
  - 計算每位球員的統計數據
  - 排序並篩選
- 響應時間：數秒到數十秒（依球員數量而定）

### 有快取（新方式）
- 直接從快取資料表讀取預先計算好的結果
- 響應時間：通常少於 100ms
- 效能提升：**10-100 倍以上**

## 監控與維護

### 檢查快取狀態

```powershell
# 檢查特定賽季的快取狀態
Invoke-RestMethod -Uri "http://localhost:5000/api/rankingcache/status/CPBL-2024-HE" -Method GET
```

### 查看日誌

應用程式日誌會記錄：
- 快取更新時間
- 是否使用快取
- 快取更新失敗的錯誤訊息

### 手動更新快取

如果需要立即反映最新的比賽結果，可以手動觸發快取更新：

```powershell
# 更新所有賽季
Invoke-RestMethod -Uri "http://localhost:5000/api/rankingcache/all" -Method POST
```

## 注意事項

1. **首次啟動**：首次啟動時快取為空，會自動計算並建立快取
2. **背景更新**：背景服務會在凌晨 3 點自動更新快取
3. **資料一致性**：快取最多延遲 24 小時，適合不需要即時更新的排行榜
4. **記憶體使用**：快取儲存在資料庫中，不佔用應用程式記憶體
5. **並發處理**：背景更新不會影響使用者的瀏覽體驗

## 未來擴展

可以考慮的優化方向：
1. 增量更新：只更新有新比賽的賽季
2. Redis 快取：將熱門資料放入 Redis 以進一步提升效能
3. 細粒度快取：按不同排序方式建立多個快取
4. 快取預熱：在比賽結束後立即更新相關快取
