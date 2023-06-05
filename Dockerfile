#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.
# Run this command from sln directory: docker build -t dilansd .

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Src/BlazorServer/Dilan.GrpcServiceDiscovery.BlazorServer.csproj", "Src/BlazorServer/"]
COPY ["Src/Grpc/Dilan.GrpcServiceDiscovery.Grpc.csproj", "Src/Grpc/"]
RUN dotnet restore "Src/BlazorServer/Dilan.GrpcServiceDiscovery.BlazorServer.csproj"
COPY . .
WORKDIR "/src/Src/BlazorServer"
RUN dotnet build "Dilan.GrpcServiceDiscovery.BlazorServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Dilan.GrpcServiceDiscovery.BlazorServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Dilan.GrpcServiceDiscovery.BlazorServer.dll"]