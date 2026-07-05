FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY YsMoment.Core/YsMoment.Core.csproj YsMoment.Core/
COPY YsMoment.Infrastructure/YsMoment.Infrastructure.csproj YsMoment.Infrastructure/
COPY YsMoment.Api/YsMoment.Api.csproj YsMoment.Api/
RUN dotnet restore YsMoment.Api/YsMoment.Api.csproj

COPY . .
RUN dotnet publish YsMoment.Api/YsMoment.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "YsMoment.Api.dll"]
