FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "nuget.config"]
COPY ["src/domain/CodeDesignPlus.Net.Microservice.Notification.Application/CodeDesignPlus.Net.Microservice.Notification.Application.csproj", "domain/CodeDesignPlus.Net.Microservice.Notification.Application/"]
COPY ["src/domain/CodeDesignPlus.Net.Microservice.Notification.Domain/CodeDesignPlus.Net.Microservice.Notification.Domain.csproj", "domain/CodeDesignPlus.Net.Microservice.Notification.Domain/"]
COPY ["src/domain/CodeDesignPlus.Net.Microservice.Notification.Infrastructure/CodeDesignPlus.Net.Microservice.Notification.Infrastructure.csproj", "domain/CodeDesignPlus.Net.Microservice.Notification.Infrastructure/"]
COPY ["src/entrypoints/CodeDesignPlus.Net.Microservice.Notification.gRpc/CodeDesignPlus.Net.Microservice.Notification.gRpc.csproj", "entrypoints/CodeDesignPlus.Net.Microservice.Notification.gRpc/"]
RUN dotnet restore "./entrypoints/CodeDesignPlus.Net.Microservice.Notification.gRpc/CodeDesignPlus.Net.Microservice.Notification.gRpc.csproj" /p:PublishReadyToRun=true
COPY ["src/domain/", "domain/"]
COPY ["src/entrypoints/CodeDesignPlus.Net.Microservice.Notification.gRpc/", "entrypoints/CodeDesignPlus.Net.Microservice.Notification.gRpc/"]

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
RUN dotnet publish "./entrypoints/CodeDesignPlus.Net.Microservice.Notification.gRpc/CodeDesignPlus.Net.Microservice.Notification.gRpc.csproj" \
    -c "$BUILD_CONFIGURATION" \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:PublishReadyToRun=true

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app
EXPOSE 8080
ENV DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_GCDynamicAdaptationMode=1 \
    DOTNET_GCConserveMemory=5 \
    DOTNET_GCHeapHardLimitPercent=70
USER app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CodeDesignPlus.Net.Microservice.Notification.gRpc.dll"]
