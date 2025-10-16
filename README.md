# BaseballApp

一個使用 ASP.NET Core MVC 和 ECharts 的棒球數據視覺化應用程式。

## 功能特色

- 🏏 **棒球數據圖表**：使用 ECharts 展示棒球統計數據
- 📊 **多種圖表類型**：
  - 打擊率趨勢線圖
  - 全壘打統計柱狀圖
  - 球員表現雷達圖
- 🎨 **響應式設計**：支援各種螢幕尺寸
- 🚀 **現代化技術棧**：ASP.NET Core MVC + ECharts

## 技術棧

- **後端**：ASP.NET Core 9.0 MVC
- **前端**：HTML5, CSS3, JavaScript
- **圖表庫**：ECharts 5.4.3
- **UI框架**：Bootstrap 5
- **版本控制**：Git

## 快速開始

### 環境需求

- .NET 9.0 SDK
- Git

### 安裝與運行

1. **複製專案**
   ```bash
   git clone <repository-url>
   cd BaseballApp
   ```

2. **建置專案**
   ```bash
   dotnet build
   ```

3. **運行應用程式**
   ```bash
   dotnet run
   ```

4. **開啟瀏覽器**

   訪問 `http://localhost:5000` 或應用程式顯示的 URL

## 專案結構

```
BaseballApp/
├── Controllers/          # MVC 控制器
│   └── HomeController.cs
├── Models/              # 數據模型
├── Views/               # Razor 視圖
│   ├── Home/
│   │   ├── Index.cshtml
│   │   ├── Charts.cshtml    # 圖表頁面
│   │   └── Privacy.cshtml
│   └── Shared/             # 共用視圖
├── wwwroot/             # 靜態資源
│   ├── css/
│   ├── js/
│   └── lib/              # 前端函式庫
│       ├── bootstrap/
│       └── echarts/
├── appsettings.json     # 應用程式設定
└── Program.cs           # 應用程式入口點
```

## 圖表功能

### 打擊率趨勢圖
- 顯示多位球員的打擊率走勢
- 支援平滑曲線和互動式提示

### 全壘打統計圖
- 柱狀圖展示球員全壘打數據
- 支援數據比較和排序

### 球員表現雷達圖
- 多維度分析球員綜合表現
- 包含打擊率、上壘率、長打率等指標

## 自訂圖表數據

在 `Views/Home/Charts.cshtml` 中修改 JavaScript 部分的數據：

```javascript
// 修改球員數據
var battingOption = {
    series: [{
        data: [0.301, 0.289, 0.283, 0.295, 0.312, 0.298] // 您的數據
    }]
};
```

## 開發

### 新增圖表

1. 在 `HomeController.cs` 中新增動作方法
2. 建立對應的視圖檔案
3. 在 `_Layout.cshtml` 中新增導航連結

### 建置和測試

```bash
# 建置
dotnet build

# 運行測試（如果有）
dotnet test

# 發佈
dotnet publish -c Release
```

## 授權

此專案僅供學習和個人使用。

## 貢獻

歡迎提交 Issue 和 Pull Request！

---

**注意**：此應用程式包含範例數據，實際使用時請替換為真實的棒球統計數據。