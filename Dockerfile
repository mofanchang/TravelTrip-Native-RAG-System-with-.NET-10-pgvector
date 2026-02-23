# 使用 .NET 10 SDK 進行建置
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# 複製 csproj 並還原依賴
COPY ["TravelTripApi/TravelTripApi.csproj", "TravelTripApi/"]
RUN dotnet restore "TravelTripApi/TravelTripApi.csproj"

# 複製所有原始碼並發佈
COPY . .
WORKDIR "/src/TravelTripApi"
RUN dotnet publish "TravelTripApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 使用運行時映像檔
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TravelTripApi.dll"]
