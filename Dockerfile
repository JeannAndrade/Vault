# <https://hub.docker.com/_/microsoft-dotnet>
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY global.json .
COPY Vault.slnx .

COPY Application/Application.csproj Application/
COPY Domain/Domain.csproj Domain/
COPY Persistence/Persistence.csproj Persistence/
COPY Service/Service.csproj Service/
COPY Service.Test/Service.Test.csproj Service.Test/

RUN dotnet restore Vault.slnx

# copy everything else
COPY . .

FROM build AS migrations
RUN dotnet tool install --tool-path /tools dotnet-ef --version 10.0.11
ENTRYPOINT ["/tools/dotnet-ef", "database", "update", "--project", "Persistence/Persistence.csproj", "--startup-project", "Persistence/Persistence.csproj"]

# build app
FROM build AS publish
RUN dotnet publish Service/Service.csproj -c Release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=publish /app ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "Service.dll"]
