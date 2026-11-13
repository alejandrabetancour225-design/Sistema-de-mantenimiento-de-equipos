# API .NET 10. Compila en la imagen del SDK y ejecuta en la imagen liviana de ASP.NET.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Backend.API.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_gcServer=0
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "Backend.API.dll"]
