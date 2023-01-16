FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
COPY . /app
WORKDIR /app/Src/NewsParser/
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:6.0  AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENTRYPOINT [ "dotnet","NewsParser.dll"]
