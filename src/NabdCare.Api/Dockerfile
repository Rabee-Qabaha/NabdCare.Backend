FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5175
EXPOSE 7053

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/NabdCare.Api/NabdCare.Api.csproj"
RUN dotnet publish "src/NabdCare.Api/NabdCare.Api.csproj" -c Release -o /out

FROM base AS final
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "NabdCare.Api.dll"]