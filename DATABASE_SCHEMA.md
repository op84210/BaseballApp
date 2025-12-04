# 棒球資料庫結構定義

## 目錄

- [棒球資料庫結構定義](#棒球資料庫結構定義)
  - [目錄](#目錄)
  - [資料表](#資料表)
    - [比賽相關](#比賽相關)
      - [tblGame - 比賽基本資料](#tblgame---比賽基本資料)
      - [tblScores - 得分資料](#tblscores---得分資料)
    - [主檔資料](#主檔資料)
      - [tblStadium - 比賽場地資料](#tblstadium---比賽場地資料)
      - [tblSeason - 賽事資料](#tblseason---賽事資料)
      - [tblTeam - 球隊資料](#tblteam---球隊資料)
      - [tblBatter - 球員資料\_打者](#tblbatter---球員資料_打者)
      - [tblPitcher - 球員資料\_投手](#tblpitcher---球員資料_投手)
    - [成績資料](#成績資料)
      - [tblBatterBox - 打者成績](#tblbatterbox---打者成績)
      - [tblPitcherBox - 投手成績](#tblpitcherbox---投手成績)
      - [tblTeamGameStats - 球隊逐場事實表](#tblteamgamestats---球隊逐場事實表)
    - [打席相關](#打席相關)
      - [tblPA - 打席資料](#tblpa---打席資料)
      - [tblEvent - 打席內事件](#tblevent---打席內事件)
      - [tblRunner - 跑者資料](#tblrunner---跑者資料)
    - [快取資料](#快取資料)
      - [tblBattingRankingCache - 打者排行榜快取](#tblbattingrankingcache---打者排行榜快取)
      - [tblPitchingRankingCache - 投手排行榜快取](#tblpitchingrankingcache---投手排行榜快取)
      - [tblTeamSeasonRankingCache - 球隊賽季匯總/排行榜快取](#tblteamseasonrankingcache---球隊賽季匯總排行榜快取)
    - [代碼資料](#代碼資料)
      - [tblCodeBases - 壘包狀況代碼](#tblcodebases---壘包狀況代碼)
      - [tblCodePitchCode - 投球結果代碼](#tblcodepitchcode---投球結果代碼)
      - [tblCodeEventType - 事件型態代碼](#tblcodeeventtype---事件型態代碼)
      - [tblCodePitchType - 球種代碼](#tblcodepitchtype---球種代碼)
      - [tblCodeRunnerType - 跑壘型態代碼](#tblcoderunnertype---跑壘型態代碼)
      - [tblCodeResult - 打席結果代碼](#tblcoderesult---打席結果代碼)
      - [tblCodeTrajectory - 擊球彈道代碼](#tblcodetrajectory---擊球彈道代碼)
      - [tblCodeHardness - 擊球力道代碼](#tblcodehardness---擊球力道代碼)
  - [代碼對照表](#代碼對照表)
    - [BASES - 壘包狀況](#bases---壘包狀況)
    - [PITCH CODE - 投球結果](#pitch-code---投球結果)
    - [EVENT TYPE - 事件型態](#event-type---事件型態)
    - [PITCH TYPE - 球種](#pitch-type---球種)
    - [RUNNER TYPE - 跑壘型態](#runner-type---跑壘型態)
    - [RESULT - 打席結果](#result---打席結果)
    - [TRAJECTORY - 擊球彈道](#trajectory---擊球彈道)
    - [HARDNESS - 擊球力道](#hardness---擊球力道)
  - [備註](#備註)

---

## 資料表

### 比賽相關

#### tblGame - 比賽基本資料

| 欄位名稱 | 型別 | 約束 | 說明 | 備註 |
|---------|------|------|------|------|
| seasonId | String | PK | 賽事ID | |
| seq | Int | PK | 比賽編號 | |
| date | Date | | 比賽日期 | |
| stadiumId | Int | | 比賽場地ID | |
| awayTeamId | String | | 客場球隊ID | |
| homeTeamId | String | | 主場球隊ID | |

#### tblScores - 得分資料

| 欄位名稱 | 型別 | 約束 | 說明 | 備註 |
|---------|------|------|------|------|
| seasonId | String | PK | 賽事ID | |
| gameSeq | Int | PK | 比賽編號 | |
| homeOrAway | String | PK | 主/客場 | |
| inning | Int | PK | 局數 | |
| score | Int | | 得分 | |

外鍵：`FOREIGN KEY (seasonId, gameSeq) REFERENCES tblGame(seasonId, seq)`

### 主檔資料

#### tblStadium - 比賽場地資料

| 欄位名稱 | 型別 | 約束 | 說明 | 備註 |
|---------|------|------|------|------|
| ID | Int | PK | 比賽場地ID | 自動增加 |
| stadium | String | UNIQUE | 比賽場地 | |

#### tblSeason - 賽事資料

| 欄位名稱 | 型別 | 約束 | 說明 | 備註 |
|---------|------|------|------|------|
| seasonId | String | PK | 賽事ID | |
| season | String | | 賽事名稱 | |

#### tblTeam - 球隊資料

| 欄位名稱 | 型別 | 約束 | 說明 | 備註 |
|---------|------|------|------|------|
| teamId | String | PK | 球隊ID | |
| teamName | String | | 球隊名稱 | |

#### tblBatter - 球員資料_打者

| 欄位名稱 | 型別 | 約束 | 說明 | 備註 |
|---------|------|------|------|------|
| playerId | String | PK | 球員ID | |
| playerNumber | String | | 球員背號 | |
| playerName | String | | 球員名稱 | |

#### tblPitcher - 球員資料_投手

| 欄位名稱 | 型別 | 約束 | 說明 | 備註 |
|---------|------|------|------|------|
| playerId | String | PK | 球員ID | |
| playerNumber | String | | 球員背號 | |
| playerName | String | | 球員名稱 | |

### 成績資料

#### tblBatterBox - 打者成績

| 欄位名稱 | 型別 | 約束 | 說明 | 備註 |
|---------|------|------|------|------|
| ID | Int | PK | 流水號 | 自動增加 |
| seasonId | String | | 賽事ID | |
| gameSeq | Int | | 比賽編號 | |
| homeOrAway | String | | 主/客場 | |
| order | Int | | 打序 | 同隊第二個相同打序即為替補上場 |
| subOrder | Int | | 替補順序 | 0=先發, 1=第一個替補, 2=第二個替補... |
| playerId | String | | 球員ID | |
| PA | Int | | 打席 | |
| AB | Int | | 打數 | |
| R | Int | | 得分 | |
| H | Int | | 安打 | |
| RBI | Int | | 打點 | |
| 2B | Int | | 二壘安打 | |
| 3B | Int | | 三壘安打 | |
| HR | Int | | 全壘打 | |
| GIDP | Int | | 滾地雙殺 | |
| DP | Int | | 雙殺打 | 包含滾地雙殺 |
| TP | Int | | 三殺打 | |
| BB | Int | | 四壞 | 包含故意四壞，不包含觸身球 |
| IBB | Int | | 故意四壞 | |
| HBP | Int | | 觸身球 | |
| SO | Int | | 三振 | |
| SH | Int | | 犧牲觸擊 | |
| SF | Int | | 犧牲飛球 | |
| E | Int | | 失誤上壘 | |
| SB | Int | | 盜壘成功 | |
| CS | Int | | 盜壘失敗 | |

唯一鍵：`UNIQUE(seasonId, gameSeq, homeOrAway, [order], subOrder)`  
外鍵：
- `FOREIGN KEY (seasonId, gameSeq) REFERENCES tblGame(seasonId, seq)`
- `FOREIGN KEY (playerId) REFERENCES tblBatter(playerId)`

#### tblPitcherBox - 投手成績

| 欄位名稱 | 型別 | 約束 | 說明 | 備註 |
|---------|------|------|------|------|
| ID | Int | PK | 流水號 | 自動增加 |
| seasonId | String | | 賽事ID | |
| gameSeq | Int | | 比賽編號 | |
| homeOrAway | String | | 主/客場 | |
| order | Int | | 上場順序 | |
| playerId | String | | 球員ID | |
| IPOuts | Int | | 出局數 | 非常見IP局數，是出局數 |
| NP | Int | | 用球數 | |
| BF | Int | | 面對打席 | |
| H | Int | | 被安打 | |
| HR | Int | | 被全壘打 | |
| BB | Int | | 四壞 | 包含故意四壞，不包含觸身球 |
| IBB | Int | | 故意四壞 | |
| HB | Int | | 觸身球 | |
| SO | Int | | 三振 | |
| R | Int | | 失分 | |
| ER | Int | | 責失 | |

唯一鍵：`UNIQUE(seasonId, gameSeq, homeOrAway, [order])`  
外鍵：
- `FOREIGN KEY (seasonId, gameSeq) REFERENCES tblGame(seasonId, seq)`
- `FOREIGN KEY (playerId) REFERENCES tblPitcher(playerId)`

#### tblTeamGameStats - 球隊逐場事實表

| 欄位名稱 | 型別 | 約束 | 說明 | 備註 |
|---------|------|------|------|------|
| id | Integer | PK | 流水號 | 自動增加 |
| seasonId | Text | | 賽季ID | |
| gameId | Text | | 比賽ID | 唯一識別該場比賽 |
| gameDate | Text | | 比賽日期 | ISO 8601 格式 |
| teamId | Text | | 球隊ID | |
| teamName | Text | | 球隊名稱 | |
| opponentTeamId | Text | | 對手球隊ID | |
| opponentTeamName | Text | | 對手球隊名稱 | |
| isHome | Integer | | 是否為主場 | 1=主場, 0=客場 |
| teamScore | Integer | | 球隊得分 | |
| opponentScore | Integer | | 對手得分 | |
| pa | Integer | | 打席 | |
| ab | Integer | | 打數 | |
| h | Integer | | 安打 | |
| twoB | Integer | | 二壘安打 | |
| threeB | Integer | | 三壘安打 | |
| hr | Integer | | 全壘打 | |
| bb | Integer | | 四壞 | |
| so | Integer | | 三振 | |
| hbp | Integer | | 觸身球 | |
| sf | Integer | | 犧牲飛球 | |
| sb | Integer | | 盜壘成功 | |
| cs | Integer | | 盜壘失敗 | |
| ipOuts | Integer | | 投球出局數 | |
| er | Integer | | 責失分 | |
| hitsAllowed | Integer | | 被安打數 | |
| bbAllowed | Integer | | 被保送數 | |
| soPitching | Integer | | 奪三振數 | |
| hrAllowed | Integer | | 被全壘打數 | |
| createdAt | Text | | 建立時間 | |

唯一鍵：`UNIQUE(gameId, teamId)`  
索引：
- `IX_TeamGameStats_GameId_TeamId (gameId, teamId)`
- `IX_TeamGameStats_Season_Team_Date (seasonId, teamId, gameDate)`

### 打席相關

#### tblPA - 打席資料

| 欄位名稱 | 型別 | 約束 | 說明 | 備註 |
|---------|------|------|------|------|
| ID | Int | PK | 打席ID | 自動增加 |
| seasonId | String | | 賽事ID | |
| gameSeq | Int | | 比賽編號 | |
| homeOrAway | String | | 主/客場 | |
| inning | Int | | 局數 | |
| paSeq | Int | | 打席序號 | 該局第幾個打席（1, 2, 3...） |
| scored | Boolean | | 有得分 | |
| batterId | String | | 打者ID | |
| batterHand | String | | 打者用手 | R / L |
| pitcherId | String | | 投手ID | |
| pitcherHand | String | | 投手用手 | R / L |
| catcherId | String | | 捕手ID | |
| paRound | Int | | 進攻輪次 | |
| paOrder | Int | | 打序 | |
| isPH | Boolean | | 是代打 | |
| awayScores | Int | | 結束打席前客場分 | |
| homeScores | Int | | 結束打席前主場分 | |
| strikes | Int | | 結束打席前好球數 | |
| balls | Int | | 結束打席前壞球數 | |
| outs | Int | | 結束打席前出局數 | |
| bases | Integer | | 結束打席前壘包狀況 (0~7，位元遮罩：1=一壘, 2=二壘, 4=三壘) | |
| homeWE | decimal | | 結束打席前主場勝率 | |
| RE | decimal | | 結束打席前得分期望值 | |
| result | String | | 打席結果 | |
| RBI | Int | | 打點 | |
| locationCode | String | | 擊球落點 | |
| trajectory | String | | 擊球彈道 | |
| hardness | String | | 擊球力道 | 非全部擊球結果都有 |
| endAwayScores | Int | | 打席結束後客場分 | |
| endHomeScores | Int | | 打席結束後主場分 | |
| endOuts | Int | | 打席結束後出局數 | |
| endBases | Integer | | 打席結束後壘包狀況 (0~7，位元遮罩：1=一壘, 2=二壘, 4=三壘) | |
| WPA | decimal | | 勝率增加 | |
| RE24 | decimal | | 得分期望值增加 | |

唯一鍵：`UNIQUE(seasonId, gameSeq, homeOrAway, inning, paSeq)`  
外鍵：
- `FOREIGN KEY (seasonId, gameSeq) REFERENCES tblGame(seasonId, seq)`
- `FOREIGN KEY (batterId) REFERENCES tblBatter(playerId)`
- `FOREIGN KEY (pitcherId) REFERENCES tblPitcher(playerId)`
- `FOREIGN KEY (catcherId) REFERENCES tblBatter(playerId)`
> 註：`tblCodeBases` 提供代碼對應名稱（0~7），未建立外鍵，查詢時可用值對應。
- `FOREIGN KEY (result) REFERENCES tblCodeResult(code)`
- `FOREIGN KEY (trajectory) REFERENCES tblCodeTrajectory(code)`
- `FOREIGN KEY (hardness) REFERENCES tblCodeHardness(code)`
> 註：`tblCodeBases` 提供代碼對應名稱（0~7），未建立外鍵，查詢時可用值對應。

#### tblEvent - 打席內事件

| 欄位名稱 | 型別 | 約束 | 說明 | 備註 |
|---------|------|------|------|------|
| ID | Int | PK | 事件ID | 自動增加 |
| paID | Int | PK | 打席ID | |
| order | Int | PK | 事件順序 | |
| type | String | | 事件型態 | |
| inPlay | Boolean | | 球進場 | |
| isStrike | Boolean | | 是好球 | 判斷 pitchCode 是否應為好球，無論是否兩好球 |
| isBall | Boolean | | 是壞球 | 判斷 pitchCode 是否應為壞球，無論是否三壞球 |
| pitcherID | String | | 投手ID | |
| catcherID | String | | 捕手ID | |
| batterID | String | | 打者ID | |
| pitchCode | String | | 投球結果 | |
| pitchType | String | | 球種 | |
| velocity | Int | | 球速 | NULL 表示無資料 |
| coordX | Int | | COORD X | NULL 表示無資料 |
| coordY | Int | | COORD Y | NULL 表示無資料 |

#### tblRunner - 跑者資料

| 欄位名稱 | 型別 | 約束 | 說明 | 備註 |
|---------|------|------|------|------|
| ID | Int | PK | 流水號 | 自動增加 |
| eventID | Int | PK | 事件ID | |
| type | String | | 跑壘型態 | |
| runnerID | String | | 跑者ID | |
| isOut | Boolean | | 是出局 | |
| scored | Boolean | | 是得分 | |
| isRBI | Boolean | | 是打者的打點 | |
| isER | Boolean | | 是投手責失 | |
| ERPitcherID | String | | 被算責失的投手 | 非責失為 null |

### 快取資料

#### tblBattingRankingCache - 打者排行榜快取

| 欄位名稱 | 型別 | 約束 | 說明 | 備註 |
|---------|------|------|------|------|
| id | Integer | PK | 主鍵 | 自動增加 |
| seasonId | Text | | 賽季ID | |
| playerId | Text | | 球員ID | |
| playerName | Text | | 球員名稱 | |
| rank | Integer | | 排名 | |
| games | Integer | | 出賽場數 | |
| pa | Integer | | 打席數 | |
| ab | Integer | | 打數 | |
| h | Integer | | 安打數 | |
| twoB | Integer | | 二壘安打 | |
| threeB | Integer | | 三壘安打 | |
| hr | Integer | | 全壘打數 | |
| rbi | Integer | | 打點數 | |
| r | Integer | | 得分數 | |
| so | Integer | | 三振數 | |
| bb | Integer | | 保送數 | |
| hbp | Integer | | 觸身球數 | |
| sf | Integer | | 犧牲飛球數 | |
| sb | Integer | | 盜壘數 | |
| avg | Real | | 打擊率 | |
| obp | Real | | 上壘率 | |
| slg | Real | | 長打率 | |
| ops | Real | | OPS | |
| updatedAt | Text | | 更新時間 | |

唯一索引：`IX_BattingRankingCache_SeasonId_PlayerId (seasonId, playerId)`  
索引：`IX_BattingRankingCache_SeasonId_Rank (seasonId, rank)`

#### tblPitchingRankingCache - 投手排行榜快取

| 欄位名稱 | 型別 | 約束 | 說明 | 備註 |
|---------|------|------|------|------|
| id | Integer | PK | 主鍵 | 自動增加 |
| seasonId | Text | | 賽季ID | |
| playerId | Text | | 球員ID | |
| playerName | Text | | 球員名稱 | |
| rank | Integer | | 排名 | |
| games | Integer | | 出賽場數 | |
| ip | Real | | 投球局數（小數） | |
| ipOuts | Integer | | 投球局數（出局數） | |
| h | Integer | | 被安打數 | |
| hr | Integer | | 被全壘打數 | |
| bb | Integer | | 四壞球數 | |
| so | Integer | | 三振數 | |
| r | Integer | | 失分數 | |
| er | Integer | | 自責分數 | |
| w | Integer | | 勝場數 | |
| l | Integer | | 敗場數 | |
| era | Real | | 防禦率 | |
| whip | Real | | 每局被上壘率 | |
| k9 | Real | | 每九局三振率 | |
| bb9 | Real | | 每九局保送率 | |
| kbbRatio | Real | | 三振保送比 | |
| baa | Real | | 被打擊率 | |
| updatedAt | Text | | 更新時間 | |

唯一索引：`IX_PitchingRankingCache_SeasonId_PlayerId (seasonId, playerId)`  
索引：`IX_PitchingRankingCache_SeasonId_Rank (seasonId, rank)`

#### tblTeamSeasonRankingCache - 球隊賽季匯總/排行榜快取

| 欄位名稱 | 型別 | 約束 | 說明 | 備註 |
|---------|------|------|------|------|
| id | Integer | PK | 流水號 | 自動增加 |
| seasonId | Text | | 賽季ID | |
| teamId | Text | | 球隊ID | |
| teamName | Text | | 球隊名稱 | |
| rank | Integer | | 排名 | |
| gamesPlayed | Integer | | 出賽場數 | |
| wins | Integer | | 勝場數 | |
| losses | Integer | | 敗場數 | |
| runsScored | Integer | | 得分 | |
| runsAllowed | Integer | | 失分 | |
| pa | Integer | | 打席 | |
| ab | Integer | | 打數 | |
| h | Integer | | 安打 | |
| twoB | Integer | | 二壘安打 | |
| threeB | Integer | | 三壘安打 | |
| hr | Integer | | 全壘打 | |
| bb | Integer | | 保送 | |
| so | Integer | | 三振 | |
| hbp | Integer | | 觸身球 | |
| sf | Integer | | 犧牲飛球 | |
| sb | Integer | | 盜壘成功 | |
| cs | Integer | | 盜壘失敗 | |
| ipOuts | Integer | | 投球出局數 | |
| er | Integer | | 責失分 | |
| hitsAllowed | Integer | | 被安打數 | |
| bbAllowed | Integer | | 被保送數 | |
| soPitching | Integer | | 奪三振數 | |
| hrAllowed | Integer | | 被全壘打數 | |
| winPct | Real | | 勝率 | |
| avg | Real | | 打擊率 | |
| obp | Real | | 上壘率 | |
| slg | Real | | 長打率 | |
| ops | Real | | OPS | |
| era | Real | | 防禦率 | |
| fip | Real | | FIP (Fielding Independent Pitching) | 可為 NULL |
| runDiff | Integer | | 得失分差 | |
| updatedAt | Text | | 更新時間 | |

唯一索引：`IX_TeamSeasonRanking_SeasonId_TeamId (seasonId, teamId)`  
索引：`IX_TeamSeasonRanking_SeasonId_Rank (seasonId, rank)`

### 代碼資料

#### tblCodeBases - 壘包狀況代碼

| 欄位名稱 | 型別 | 約束 | 說明 |
|---------|------|------|------|
| code | Integer | PK | 代碼值（0~7） |
| name | String | | 代碼名稱 |

#### tblCodePitchCode - 投球結果代碼

| 欄位名稱 | 型別 | 約束 | 說明 |
|---------|------|------|------|
| code | String | PK | 代碼值 |
| name | String | | 代碼名稱 |

#### tblCodeEventType - 事件型態代碼

| 欄位名稱 | 型別 | 約束 | 說明 |
|---------|------|------|------|
| code | String | PK | 代碼值 |
| name | String | | 代碼名稱 |

#### tblCodePitchType - 球種代碼

| 欄位名稱 | 型別 | 約束 | 說明 |
|---------|------|------|------|
| code | String | PK | 代碼值 |
| name | String | | 代碼名稱 |

#### tblCodeRunnerType - 跑壘型態代碼

| 欄位名稱 | 型別 | 約束 | 說明 |
|---------|------|------|------|
| code | String | PK | 代碼值 |
| name | String | | 代碼名稱 |

#### tblCodeResult - 打席結果代碼

| 欄位名稱 | 型別 | 約束 | 說明 |
|---------|------|------|------|
| code | String | PK | 代碼值 |
| name | String | | 代碼名稱 |

#### tblCodeTrajectory - 擊球彈道代碼

| 欄位名稱 | 型別 | 約束 | 說明 |
|---------|------|------|------|
| code | String | PK | 代碼值 |
| name | String | | 代碼名稱 |

#### tblCodeHardness - 擊球力道代碼

| 欄位名稱 | 型別 | 約束 | 說明 |
|---------|------|------|------|
| code | String | PK | 代碼值 |
| name | String | | 代碼名稱 |

---

## 代碼對照表

### BASES - 壘包狀況

| 代碼 | 說明 |
|------|------|
| 0 | 無人在壘 |
| 1 | 一壘有人 |
| 2 | 二壘有人 |
| 3 | 一二壘有人 |
| 4 | 三壘有人 |
| 5 | 一三壘有人 |
| 6 | 二三壘有人 |
| 7 | 滿壘 |

### PITCH CODE - 投球結果

| 代碼 | 說明 |
|------|------|
| S | 無揮棒好球 |
| SW | 揮棒落空 |
| B | 壞球 |
| F | 界外 |
| FT | 擦棒被捕 |
| FOUL_BUNT | 觸擊界外 |
| TRY_BUNT | 觸擊落空 |
| BUNT | 觸擊 |
| H | 擊球進場 |

### EVENT TYPE - 事件型態

| 代碼 | 說明 |
|------|------|
| PITCH | 投球事件，可能包含跑壘 |
| BASE | 無投球跑壘事件 |
| NO_PITCH | 無投球事件 |
| SUB | 換人事件 |

### PITCH TYPE - 球種

| 代碼 | 說明 |
|------|------|
| FF | 四縫 |
| SI | 伸卡/二縫 |
| FC | 卡特 |
| KN | 蝴蝶 |
| SL | 滑球 |
| CU | 曲球 |
| CH | 變速 |
| FO | 指叉 |
| FS | 快指 |
| EP | 小便 |

### RUNNER TYPE - 跑壘型態

| 代碼 | 說明 |
|------|------|
| PA | 打者 |
| ADVANCE | 推進 |
| SB | 盜壘成功 |
| CS | 盜壘失敗 |
| CS_E | 盜壘失敗但野手失誤上壘 |
| PO | 牽制出局 |

### RESULT - 打席結果

| 代碼 | 說明 |
|------|------|
| 1B | 一安 |
| 2B | 二安 |
| 3B | 三安 |
| HR | 全壘打 |
| IHR | 場內全壘打 |
| SO | 三振 |
| uBB | 非故意四壞 |
| IBB | 故意四壞 |
| HBP | 觸身保送 |
| GO | 滾地出局 |
| FO | 飛球出局 |
| FC | 野手選擇 |
| E | 失誤 |
| SH | 犧牲觸擊 |
| SF | 犧牲飛球 |
| GIDP | 滾地雙殺 |
| DP | 雙殺 |
| TP | 三殺 |
| IH | 妨礙打擊 |
| IR | 妨礙跑壘 |
| ID | 妨礙守備 |
| IGNORE | 不算打席（壘包跑者出局導致半局結束等） |

### TRAJECTORY - 擊球彈道

| 代碼 | 說明 |
|------|------|
| G | 滾地 |
| L | 平飛 |
| F | 高飛 |
| P | 內野高飛 |

### HARDNESS - 擊球力道

| 代碼 | 說明 |
|------|------|
| S | 弱 |
| M | 中 |
| H | 強 |

---

## 備註

- 本文件同步於專案內 `Table Schema.txt`
- 欄位型別與約束條件請依實際資料庫實作調整（如 SQLite、SQL Server、PostgreSQL）
- 自動增加欄位建議使用 `AUTOINCREMENT` (SQLite) 或 `IDENTITY` (SQL Server)
- 日期與時間欄位建議使用 ISO 8601 格式 (`yyyy-MM-dd`)
