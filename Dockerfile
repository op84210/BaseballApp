# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 複製 csproj 並還原套件
COPY BaseballApp.csproj .
COPY BaseballApp.Core/BaseballApp.Core.csproj BaseballApp.Core/
RUN dotnet restore BaseballApp.csproj

# 複製所有原始碼並建置
COPY . .
RUN dotnet publish BaseballApp.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# 監聽埠號（Render 會自動綁定 PORT 環境變數，預設 10000）
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

# 複製建置結果
COPY --from=build /app/publish .

# 複製 SQLite 資料庫（用於首次部署時資料遷移）
COPY --from=build /src/data ./data

ENTRYPOINT ["dotnet", "BaseballApp.dll"]
