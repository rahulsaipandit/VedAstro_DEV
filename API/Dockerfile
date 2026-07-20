#this file exists to RUN DOCKER locally in VS2022 and in Render Server
#this is not DEFAULT docker file

#Plain ASP.NET Core container - the API is a Kestrel minimal API (no Azure Functions
#host), so this just builds/publishes/runs it like any other .NET app.

#this is run from solution folder even though placed in API folder
#so paths need to include folder "API"
#path values are very important, cause of most failures
#a temp project build is made in "src" then use that to make the final container

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["API/API.csproj", "API/"]
COPY ["Data/VedAstro.Data.csproj", "Data/"]
COPY ["Library/Library.csproj", "Library/"]
RUN dotnet restore "API/API.csproj"
COPY . .

WORKDIR "/src/API"
RUN dotnet build "API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 7071
ENV ASPNETCORE_URLS=http://+:7071
ENTRYPOINT ["dotnet", "API.dll"]
