# 球隊快取系統實作總結

## 概述

本次實作新增了球隊數據的快取系統，包含場次級別的統計資料和賽季級別的排行榜快取。

## 新增的資料表

### 1. tblTeamGameStats（球隊場次統計）

**用途**：儲存每場比賽每支球隊的統計數據，作為時間序列分析的基礎

**欄位**：
- 基本資訊：seasonId, teamId, teamName, gameDate, gameSeq, teamScore, opponentScore
- 打擊數據：pa, ab, h, twoB, threeB, hr, bb, so, hbp, sf, sb, cs
- 投球數據：ipOuts, er, hitsAllowed, bbAllowed, soPitching, hrAllowed
- 時間戳記：createdAt

**索引**：
- 唯一索引：(seasonId, teamId, gameDate)
- 適用於時間範圍查詢和球隊查詢

### 2. tblTeamSeasonRankingCache（球隊賽季排行榜快取）

**用途**：儲存賽季級別的球隊統計數據，用於快速顯示排行榜和球隊平均數據

**欄位**：
- 基本資訊：seasonId, teamId, teamName, rank
- 戰績：gamesPlayed, wins, losses, winPct
- 得分：runsScored, runsAllowed, runDiff
- 打擊數據：pa, ab, h, twoB, threeB, hr, bb, so, hbp, sf, sb, cs
- 打擊率：avg, obp, slg, ops
- 投球數據：ipOuts, er, hitsAllowed, bbAllowed, soPitching, hrAllowed
- 投球率：era, fip
- 時間戳記：updatedAt

**索引**：
- 唯一索引：(seasonId, teamId)
- 排名索引：(seasonId, rank)

## 資料流程

### 資料導入流程（DataEtl）

```
JSON 檔案 → DataEtl
    ↓
1. 讀取比賽資料
    ↓
2. 聚合打者打席數據（AggregateBatterBox）
   - 計算 PA, AB, H, 2B, 3B, HR, BB, SO, HBP, SF, SB, CS
    ↓
3. 聚合投手投球數據（AggregatePitcherBox）
   - 計算 ipOuts, ER, H, BB, SO, HR
    ↓
4. 寫入 tblTeamGameStats（InsertOneTeamGameRow）
    ↓
5. 從 tblTeamGameStats 聚合到 tblTeamSeasonRankingCache
   - 使用 SQL GROUP BY 計算賽季總計
   - 計算衍生指標（winPct, AVG, OBP, SLG, OPS, ERA）
   - 使用 ROW_NUMBER() 計算排名
```

### 快取更新流程（RankingCacheService）

```
API 呼叫 → RankingCacheController
    ↓
RankingCacheService.UpdateTeamRankingsAsync(seasonId)
    ↓
執行 SQL 更新語句：
1. DELETE FROM tblTeamSeasonRankingCache WHERE seasonId = ?
2. INSERT INTO tblTeamSeasonRankingCache SELECT ... FROM tblTeamGameStats GROUP BY ...
3. UPDATE tblTeamSeasonRankingCache SET ops = obp + slg
4. WITH ranked AS (...) UPDATE tblTeamSeasonRankingCache SET rank = ...
    ↓
快取已更新
```

## API 端點

### 新增的端點

1. **POST /api/rankingcache/team/{seasonId}**
   - 更新指定賽季的球隊排行榜快取
   - 範例：`POST /api/rankingcache/team/CPBL-2024-HE`

2. **POST /api/rankingcache/team?seasonId={seasonId}**
   - 更新球隊排行榜快取（seasonId 為可選參數）
   - 不提供 seasonId 則更新所有賽季
   - 範例：`POST /api/rankingcache/team` （更新所有）
   - 範例：`POST /api/rankingcache/team?seasonId=CPBL-2024-HE`（更新特定賽季）

3. **POST /api/rankingcache/all**
   - 已更新為包含球隊排行榜
   - 更新所有賽季的打者、投手、球隊排行榜快取

## 前端整合

### 球員詳細頁面雷達圖

**功能**：在球員能力雷達圖中顯示球隊平均值

**資料來源**：
```sql
SELECT avg, obp, slg, ops, hr, rbi, so, bb
FROM tblTeamSeasonRankingCache tsc
JOIN tblPlayerTeam pt ON tsc.seasonId = pt.seasonId AND tsc.teamId = pt.teamId
WHERE pt.playerId = ? AND pt.seasonId = ?
```

**顯示方式**：
- 球員數據：藍色實線
- 球隊平均：黃色實線
- 賽季平均：綠色虛線

**PR 值計算**：
- 球隊平均的 PR 值使用簡易線性估算：`(teamValue / seasonAverage) * 50`
- 僅供參考，實際 PR 應基於分布計算

## 測試工具

### test-cache-api.html

新增的測試按鈕：
- 🏟️ 更新球隊排行榜（特定賽季）
- 🏆 更新所有球隊排行榜

使用方式：
1. 啟動應用程式
2. 瀏覽 http://localhost:5000/test-cache-api.html
3. 點擊相應按鈕測試 API

## 使用範例

### 命令列操作

```powershell
# 更新 CPBL-2024-HE 賽季的球隊快取
Invoke-RestMethod -Uri "http://localhost:5000/api/rankingcache/team/CPBL-2024-HE" -Method POST

# 更新所有賽季的球隊快取
Invoke-RestMethod -Uri "http://localhost:5000/api/rankingcache/team" -Method POST

# 更新所有類型的快取（包含球員和球隊）
Invoke-RestMethod -Uri "http://localhost:5000/api/rankingcache/all" -Method POST
```

### 資料導入與快取建立

```powershell
# 1. 執行 DataEtl 導入資料並建立快取表
cd c:\Users\kwlin\Desktop\ideas\BaseballApp\tools\DataEtl
dotnet run -- --db c:\Users\kwlin\Desktop\ideas\BaseballApp\data\baseball.db

# 2. 啟動應用程式
cd c:\Users\kwlin\Desktop\ideas\BaseballApp
dotnet run

# 3. 手動觸發快取更新（如需要）
Invoke-RestMethod -Uri "http://localhost:5000/api/rankingcache/all" -Method POST
```

## 效能考量

### tblTeamGameStats
- 資料量：每支球隊每場比賽 1 筆（4 隊 × 120 場 = 480 筆/賽季）
- 查詢效率：透過 (seasonId, teamId, gameDate) 索引實現快速範圍查詢
- 適用場景：折線圖、趨勢分析

### tblTeamSeasonRankingCache
- 資料量：每支球隊每賽季 1 筆（4 筆/賽季）
- 查詢效率：透過 (seasonId, teamId) 索引實現即時查詢
- 更新頻率：每天凌晨 3 點（背景服務）或手動觸發
- 適用場景：排行榜顯示、球隊平均值比較

## 未來擴展建議

1. **防守數據**：加入守備率、失誤、雙殺等數據
2. **進階指標**：計算 FIP, wOBA, wRC+ 等進階指標
3. **對戰數據**：記錄球隊間的對戰成績
4. **球場因素**：考慮主客場表現差異
5. **月份數據**：加入月份維度的統計分析
6. **即時更新**：透過 SignalR 實現即時快取更新通知

## 相關檔案

- **資料導入**：`tools/DataEtl/Program.cs`
- **Service**：`Services/RankingCacheService.cs`
- **Controller**：`Controllers/RankingCacheController.cs`
- **前端**：`wwwroot/js/playerDetail.js`
- **View**：`Views/Baseball/PlayerDetail.cshtml`
- **ViewModel**：`Models/PlayerDetailViewModel.cs`
- **測試工具**：`wwwroot/test-cache-api.html`
- **文件**：`RANKING_CACHE_README.md`

## 版本歷史

- **v1.0**（2024）：初始實作球員快取系統（打者、投手）
- **v1.1**（本次）：新增球隊快取系統（場次級、賽季級）

---

**實作完成日期**：2024
**實作者**：GitHub Copilot
