# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY ChatApp/*.csproj ./ChatApp/
RUN dotnet restore ./ChatApp/ChatApp.csproj

COPY . .
RUN dotnet publish ./ChatApp/ChatApp.csproj -c Release -o out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/out .

EXPOSE 5000
EXPOSE 5001

ENTRYPOINT ["dotnet", "ChatApp.dll"]