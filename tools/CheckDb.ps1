# 檢查 baseball.db 的內容
$dbPath = ".\data\baseball.db"

Write-Host "=== 資料庫檔案資訊 ===" -ForegroundColor Green
Get-Item $dbPath | Select-Object Name, Length, LastWriteTime

Write-Host "`n=== 使用 .NET 查詢資料庫 ===" -ForegroundColor Green

Add-Type -Path "C:\Users\kwlin\.nuget\packages\microsoft.data.sqlite\9.0.0\lib\net9.0\Microsoft.Data.Sqlite.dll"

$connString = "Data Source=$dbPath"
$conn = New-Object Microsoft.Data.Sqlite.SqliteConnection($connString)
$conn.Open()

# 列出所有表格
Write-Host "`n--- 資料表列表 ---" -ForegroundColor Yellow
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name"
$reader = $cmd.ExecuteReader()
$tables = @()
while ($reader.Read()) {
    $tableName = $reader.GetString(0)
    $tables += $tableName
    Write-Host $tableName
}
$reader.Close()

# 查詢每個表的資料筆數
Write-Host "`n--- 各表資料筆數 ---" -ForegroundColor Yellow
foreach ($table in $tables) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT COUNT(*) FROM $table"
    $count = $cmd.ExecuteScalar()
    Write-Host "$table : $count 筆" -ForegroundColor Cyan
}

# 查看 tblGame 的前 5 筆
Write-Host "`n--- tblGame 前 5 筆 ---" -ForegroundColor Yellow
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT seasonId, seq, date, stadium, awayTeam, homeTeam, awayFinalScore, homeFinalScore FROM tblGame LIMIT 5"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    Write-Host "$($reader[0]) G$($reader[1]) | $($reader[2]) | $($reader[3]) | $($reader[4]) vs $($reader[5]) ($($reader[6])-$($reader[7]))"
}
$reader.Close()

# 查看 tblBatterBox 的前 3 筆
Write-Host "`n--- tblBatterBox 前 3 筆 ---" -ForegroundColor Yellow
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT seasonId, seq, teamId, playerName, PA, AB, H, HR, RBI, SO FROM tblBatterBox LIMIT 3"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    Write-Host "G$($reader[1]) $($reader[2]) | $($reader[3]) | PA:$($reader[4]) AB:$($reader[5]) H:$($reader[6]) HR:$($reader[7]) RBI:$($reader[8]) SO:$($reader[9])"
}
$reader.Close()

# 查看 tblPitcherBox 的前 3 筆
Write-Host "`n--- tblPitcherBox 前 3 筆 ---" -ForegroundColor Yellow
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT seasonId, seq, teamId, playerName, IPOuts, NP, H, HR, BB, SO, ER FROM tblPitcherBox LIMIT 3"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    Write-Host "G$($reader[1]) $($reader[2]) | $($reader[3]) | IP:$($reader[4]/3) NP:$($reader[5]) H:$($reader[6]) HR:$($reader[7]) BB:$($reader[8]) SO:$($reader[9]) ER:$($reader[10])"
}
$reader.Close()

# 查看 tblPA 的前 3 筆
Write-Host "`n--- tblPA 前 3 筆 ---" -ForegroundColor Yellow
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT seasonId, seq, inning, batterName, pitcherName, result, strikes, balls, outs FROM tblPA LIMIT 3"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    Write-Host "G$($reader[1]) T$($reader[2]) | $($reader[3]) vs $($reader[4]) | 結果:$($reader[5]) | $($reader[6])S $($reader[7])B $($reader[8])O"
}
$reader.Close()

$conn.Close()
Write-Host "`n=== 檢查完成 ===" -ForegroundColor Green
