# 🔔 Notification Microservice

**Real-time notification orchestration service** built with .NET 9, implementing Clean Architecture, DDD, and CQRS patterns. This microservice provides centralized notification delivery through multiple channels (SignalR WebSockets, gRPC Streaming) with comprehensive tracking and tenant isolation.

## 📋 Overview

The Notification microservice acts as a central hub for real-time event distribution across the ExcellenceForge platform. It ingests notification requests via gRPC bidirectional streaming and distributes them to connected web clients through SignalR, with optional Redis backplane for horizontal scaling.

### Key Capabilities

- **Multi-channel Delivery**: User-specific, group-based, and broadcast notifications
- **Real-time Communication**: SignalR WebSocket connections with automatic reconnection
- **gRPC Streaming**: Bidirectional streaming for high-throughput event ingestion
- **Delivery Tracking**: MongoDB-based audit trail with success/failure tracking
- **Tenant Isolation**: Automatic tenant-based grouping and filtering
- **Scalability**: Redis backplane support for multi-instance deployments

## 🏗️ Architecture

### Layered Structure

```
CodeDesignPlus.Net.Microservice.Notification/
├── src/
│   ├── domain/
│   │   ├── Domain/              # Aggregates, entities, value objects
│   │   │   ├── NotificationsAggregate.cs
│   │   │   ├── Enums/
│   │   │   │   └── NotificationType.cs (User, Group, Broadcast)
│   │   │   ├── DomainEvents/
│   │   │   │   ├── NotificationsCreatedDomainEvent.cs
│   │   │   │   ├── NotificationsUpdatedDomainEvent.cs
│   │   │   │   └── NotificationsDeletedDomainEvent.cs
│   │   │   ├── Repositories/
│   │   │   │   └── INotificationsRepository.cs
│   │   │   └── Services/
│   │   │       └── INotifierGateway.cs
│   │   ├── Application/         # Use cases (CQRS commands/queries)
│   │   │   └── Notifications/
│   │   │       └── Commands/
│   │   │           ├── SendToUserNotification/
│   │   │           │   ├── SendToUserNotificationCommand.cs
│   │   │           │   └── SendToUserNotificationCommandHandler.cs
│   │   │           ├── SendToGroupNotification/
│   │   │           │   ├── SendToGroupNotificationCommand.cs
│   │   │           │   └── SendToGroupNotificationCommandHandler.cs
│   │   │           └── BroadcastNotification/
│   │   │               ├── BroadcastNotificationCommand.cs
│   │   │               └── BroadcastNotificationCommandHandler.cs
│   │   └── Infrastructure/      # External integrations
│   │       ├── Repositories/
│   │       │   └── NotificationsRepository.cs
│   │       └── Services/
│   │           ├── SignalRNotifierAdapter.cs
│   │           └── UserIdProvider.cs
│   └── entrypoints/
│       └── gRpc/                # gRPC + SignalR hybrid entrypoint
│           ├── Services/
│           │   └── NotificationsService.cs
│           ├── Hubs/
│           │   └── MainHub.cs
│           ├── Protos/
│           │   └── notifications.proto
│           └── Program.cs
├── tests/
│   ├── unit/
│   │   ├── Domain.Test/
│   │   ├── Application.Test/
│   │   ├── Infrastructure.Test/
│   │   └── gRpc.Test/
│   └── integration/
│       └── gRpc.Test/
├── charts/
│   └── ms-notification-grpc/    # Helm chart for Kubernetes
└── tools/                        # DevOps scripts
```

### Notification Flow

```
┌────────────────┐                    ┌──────────────────┐
│   Producer     │  gRPC Streaming    │  Notification    │
│  Microservice  │───────────────────>│   Microservice   │
│ (Any service)  │  NotificationReq   │  (gRPC Service)  │
└────────────────┘                    └──────────────────┘
                                               │
                         ┌─────────────────────┼─────────────────────┐
                         │                     │                     │
                         ▼                     ▼                     ▼
               ┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐
               │  SendToUser     │   │  SendToGroup    │   │   Broadcast     │
               │   (MediatR)     │   │   (MediatR)     │   │   (MediatR)     │
               └─────────────────┘   └─────────────────┘   └─────────────────┘
                         │                     │                     │
                         └─────────────────────┼─────────────────────┘
                                               │
                                               ▼
                                ┌──────────────────────────┐
                                │  INotifierGateway        │
                                │  (SignalR Adapter)       │
                                └──────────────────────────┘
                                               │
                         ┌─────────────────────┼─────────────────────┐
                         │                     │                     │
                         ▼                     ▼                     ▼
              ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
              │  SignalR Client  │  │  SignalR Client  │  │  SignalR Client  │
              │   (User A)       │  │   (Group X)      │  │   (All Tenant)   │
              └──────────────────┘  └──────────────────┘  └──────────────────┘
                         │                     │                     │
                         ▼                     ▼                     ▼
                    ┌─────────────────────────────────────────────────┐
                    │         MongoDB Audit Collection                │
                    │  (Success/Failure tracking per notification)    │
                    └─────────────────────────────────────────────────┘
```

## 🎯 Domain Model

### NotificationsAggregate

Central aggregate tracking notification delivery status:

```csharp
public class NotificationsAggregate : AggregateRoot
{
    public Guid? UserId { get; private set; }           // Target user (for User type)
    public NotificationType Type { get; private set; }   // User | Group | Broadcast
    public string? GroupName { get; private set; }       // Target group (for Group type)
    public string? PayloadPreview { get; private set; }  // Truncated payload for audit
    public Instant SentAt { get; private set; }          // Timestamp of delivery attempt
    public bool WasSuccess { get; private set; }         // Delivery status
    public string? FailureReason { get; private set; }   // Error message if failed
}
```

### NotificationType Enum

```csharp
public enum NotificationType
{
    User,       // Direct notification to specific user by UserId
    Group,      // Notification to all users in a named group
    Broadcast   // Notification to all connected users within tenant
}
```

### Factory Methods

```csharp
// User-specific notification
var aggregate = NotificationsAggregate.Create(id, userId, NotificationType.User, payloadPreview, tenant, sentBy);

// Group notification
var aggregate = NotificationsAggregate.Create(id, groupName, NotificationType.Group, payloadPreview, tenant, sentBy);

// Broadcast notification
var aggregate = NotificationsAggregate.Create(id, NotificationType.Broadcast, payloadPreview, tenant, sentBy);

// Mark delivery result
aggregate.MarkAsSent(updatedBy);
aggregate.MarkAsFailed(reason, updatedBy);
```

## 🔌 gRPC Service Contract

### notifications.proto

```protobuf
service Notifier {
  // Send notifications to a specific user identified by their ID
  rpc SendToUser (stream NotificationUserRequest) returns (stream NotificationResponse);
  
  // Sends a broadcast notification to all connected users (within a Tenant)
  rpc Broadcast (stream NotificationBroadcastRequest) returns (stream NotificationResponse);
  
  // Sends notifications to a logical group of connections (e.g., "Administrators", "Project_A")
  rpc SendToGroup (stream NotificationGroupRequest) returns (stream NotificationResponse);
}
```

### Message Definitions

#### NotificationUserRequest

```protobuf
message NotificationUserRequest {
  string id = 1;               // Notification tracking ID (GUID)
  string userId = 2;            // Target user ID (GUID or JWT Sub claim)
  string eventName = 3;         // Frontend event name (e.g., "OrderUpdated")
  string jsonPayload = 4;       // Serialized JSON data
  string tenant = 5;            // Tenant/organization ID
  string sentBy = 6;            // Originating system/user ID
}
```

#### NotificationGroupRequest

```protobuf
message NotificationGroupRequest {
  string id = 1;
  string groupName = 2;         // SignalR group name (must match client-side join)
  string eventName = 3;
  string jsonPayload = 4;
  string tenant = 5;
  string sentBy = 6;
}
```

#### NotificationBroadcastRequest

```protobuf
message NotificationBroadcastRequest {
  string id = 1;
  string eventName = 2;
  string jsonPayload = 3;
  string tenant = 4;
  string sentBy = 5;
}
```

#### NotificationResponse

```protobuf
message NotificationResponse {
  bool success = 1;
  string message = 2;
}
```

## 🚀 CQRS Commands

### SendToUserNotificationCommand

Sends a notification to a specific user's active SignalR connections.

**Command:**
```csharp
public record SendToUserNotificationCommand(
    Guid Id, 
    Guid UserId, 
    string EventName, 
    string JsonPayload,
    Guid Tenant,
    Guid SentBy
) : IRequest<bool>;
```

**Validation Rules:**
- Id, UserId, Tenant must not be empty
- EventName is required (frontend event listener name)
- JsonPayload must be valid JSON (object or array)

**Handler Flow:**
1. Create NotificationsAggregate with UserId
2. Call INotifierGateway.SendToUserAsync()
3. Mark aggregate as Sent or Failed
4. Persist to repository for audit
5. Return success status

**SignalR Delivery:**
- Uses UserIdProvider to map JWT claims to SignalR user identity
- Sends to all active connections for the specified UserId
- Tenant isolation enforced via SignalR groups

### SendToGroupNotificationCommand

Sends a notification to all users in a named group within a tenant.

**Command:**
```csharp
public record SendToGroupNotificationCommand(
    Guid Id, 
    string GroupName, 
    string EventName, 
    string JsonPayload,
    Guid Tenant,
    Guid SentBy
) : IRequest<bool>;
```

**Validation Rules:**
- Id, Tenant must not be empty
- GroupName is required
- EventName and JsonPayload must be valid

**Handler Flow:**
1. Create NotificationsAggregate with GroupName
2. Call INotifierGateway.SendToGroupAsync()
3. Track delivery status in MongoDB
4. Return result

**Group Management:**
- Groups follow naming convention: `Tenant:{TenantId}:{GroupName}`
- Clients join groups via MainHub.JoinGroup(groupName)
- Useful for role-based notifications (e.g., "Administrators", "Project_123")

### BroadcastNotificationCommand

Sends a notification to all connected users within a tenant.

**Command:**
```csharp
public record BroadcastNotificationCommand(
    Guid Id,
    string EventName,
    string JsonPayload,
    Guid Tenant,
    Guid SentBy
) : IRequest<bool>;
```

**Validation Rules:**
- Id, Tenant, EventName must not be empty
- JsonPayload must be valid JSON

**Handler Flow:**
1. Create NotificationsAggregate with Broadcast type
2. Call INotifierGateway.BroadcastAsync()
3. Persist audit record
4. Return success status

**Broadcast Scope:**
- Sends to all SignalR connections in tenant group: `Tenant:{TenantId}`
- Automatically scoped to tenant via MainHub.OnConnectedAsync()
- Ideal for system-wide announcements

## 🌐 SignalR Hub

### MainHub

WebSocket hub for client connections with authentication and tenant grouping.

**Connection Lifecycle:**

```csharp
[Authorize]
public class MainHub : Hub
{
    // Automatic on connect
    public override async Task OnConnectedAsync()
    {
        // Extract tenant from JWT claims
        var tenantId = context.Tenant;
        
        // Auto-join tenant group for broadcast scoping
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Tenant:{tenantId}");
    }
    
    // Client-initiated group join
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Tenant:{tenantId}:{groupName}");
    }
    
    // Client-initiated group leave
    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Tenant:{tenantId}:{groupName}");
    }
}
```

**Client Connection Example (JavaScript/TypeScript):**

```typescript
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

const connection = new HubConnectionBuilder()
  .withUrl('https://services.example.com/ms-notification/hubs/notifications', {
    accessTokenFactory: () => getJwtToken() // Include Bearer token
  })
  .withAutomaticReconnect()
  .configureLogging(LogLevel.Information)
  .build();

// Start connection
await connection.start();

// Listen for specific event
connection.on('OrderUpdated', (jsonPayload) => {
  const data = JSON.parse(jsonPayload);
  console.log('Order updated:', data);
});

// Join a group
await connection.invoke('JoinGroup', 'Administrators');

// Leave a group
await connection.invoke('LeaveGroup', 'Administrators');
```

### UserIdProvider

Custom provider mapping JWT claims to SignalR user identity:

```csharp
public class UserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst(ClaimTypes.UserId)?.Value.ToLower();
    }
}
```

## 📦 Infrastructure Services

### SignalRNotifierAdapter

Adapter implementing INotifierGateway using SignalR HubContext:

```csharp
public class SignalRNotifierAdapter<THub> : INotifierGateway 
    where THub : Hub
{
    private readonly IHubContext<THub> _hubContext;
    
    // Send to specific user (all their connections)
    public async Task SendToUserAsync(Guid userId, string method, string payload, CancellationToken ct)
    {
        await _hubContext.Clients.User(userId.ToString()).SendAsync(method, payload, cancellationToken: ct);
    }
    
    // Send to all clients (tenant-scoped via group)
    public async Task BroadcastAsync(string method, string payload, CancellationToken ct)
    {
        await _hubContext.Clients.All.SendAsync(method, payload, cancellationToken: ct);
    }
    
    // Send to named group
    public async Task SendToGroupAsync(string group, string method, string payload, CancellationToken ct)
    {
        await _hubContext.Clients.Group(group).SendAsync(method, payload, cancellationToken: ct);
    }
}
```

### NotificationsRepository

MongoDB repository for notification audit trail:

```csharp
public interface INotificationsRepository : IRepositoryBase
{
    // Inherits CRUD operations from IRepositoryBase:
    // - CreateAsync(NotificationsAggregate)
    // - UpdateAsync(NotificationsAggregate)
    // - FindAsync(Guid id)
    // - GetAllAsync(filters)
}
```

**MongoDB Collection:** `db-ms-notification.notifications`

**Document Schema:**
```json
{
  "_id": "guid",
  "userId": "guid or null",
  "type": "User | Group | Broadcast",
  "groupName": "string or null",
  "payloadPreview": "truncated JSON string",
  "sentAt": "ISO 8601 timestamp",
  "wasSuccess": true,
  "failureReason": "error message or null",
  "tenant": "guid",
  "createdAt": "ISO 8601",
  "createdBy": "guid",
  "updatedAt": "ISO 8601",
  "updatedBy": "guid"
}
```

## 🛠️ Technology Stack

### Core Framework
- **.NET 9**: Latest LTS framework
- **Clean Architecture**: Domain, Application, Infrastructure, Entrypoints
- **DDD**: Aggregates, domain events, repositories
- **CQRS**: MediatR-based command handlers

### Communication
- **gRPC**: Bidirectional streaming for event ingestion (port 5001/HTTP2)
- **SignalR**: WebSocket real-time client communication (port 5000/HTTP)
- **ASP.NET Core Kestrel**: Multi-protocol endpoints

### Data & Caching
- **MongoDB**: Notification audit storage (`db-ms-notification`)
- **Redis**: (Optional) SignalR backplane for scaling

### Messaging & Events
- **RabbitMQ**: Domain event publishing

### Security & Observability
- **HashiCorp Vault**: Secret management (MongoDB credentials, RabbitMQ)
- **OpenTelemetry**: Distributed tracing (traces to OTel Collector)
- **Serilog**: Structured logging
- **HealthChecks**: `/health/live`, `/health/ready`

### Testing & Quality
- **xUnit**: Unit and integration tests
- **Testcontainers**: Integration test infrastructure
- **FluentValidation**: Command validation
- **Mapster**: DTO mapping

## ⚙️ Configuration

### appsettings.json

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://*:5000",
        "Protocols": "Http1"
      },
      "Http2": {
        "Url": "http://*:5001",
        "Protocols": "Http2"
      }
    }
  },
  "Core": {
    "AppName": "ms-notification",
    "TypeEntryPoint": "grpc",
    "Version": "v1",
    "Description": "Microservicio centralizado de notificaciones en tiempo real que orquesta la ingesta de eventos vía gRPC Streaming y su distribución escalable a clientes web mediante SignalR y Redis Backplane.",
    "Business": "CodeDesignPlus",
    "Contact": {
      "Name": "Wilzon Liscano",
      "Email": "wliscano@codedesignplus.com"
    }
  },
  "Mongo": {
    "Enable": true,
    "ConnectionString": "mongodb://localhost:27017",
    "Database": "db-ms-notification"
  },
  "Redis": {
    "Instances": {
      "Core": {
        "ConnectionString": "localhost:6379"
      }
    }
  },
  "RedisCache": {
    "Enable": true,
    "Expiration": "00:05:00"
  },
  "RabbitMQ": {
    "Enable": true,
    "Host": "localhost",
    "Port": 5672,
    "UserName": "admin",
    "Password": "password"
  },
  "Vault": {
    "Enable": true,
    "Address": "http://127.0.0.1:8200",
    "AppName": "ms-notification",
    "Solution": "security-codedesignplus",
    "Token": "root",
    "Mongo": {
      "Enable": true,
      "TemplateConnectionString": "mongodb://{0}:{1}@localhost:27017"
    },
    "RabbitMQ": {
      "Enable": true
    }
  },
  "Observability": {
    "Enable": true,
    "ServerOtel": "http://127.0.0.1:4317",
    "Trace": {
      "Enable": true,
      "AspNetCore": true,
      "CodeDesignPlusSdk": true,
      "Redis": true
    }
  },
  "Security": {
    "ValidateAudience": true,
    "ValidateIssuer": true,
    "ValidateLifetime": true,
    "RequireHttpsMetadata": true
  }
}
```

### Environment-Specific Configuration

- **Development** (`appsettings.Development.json`): Vault disabled, local services
- **Docker** (`appsettings.Docker.json`): Containerized service URLs
- **Staging** (`appsettings.Staging.json`): Cloud-hosted dependencies

## 🚀 Getting Started

### Prerequisites

- **.NET 9 SDK** (9.0 or later)
- **Docker & Docker Compose** (for infrastructure)
- **MongoDB 7.0+**
- **Redis 7.0+**
- **RabbitMQ 3.12+**
- **Vault** (optional, for secret management)

### Local Development Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com/codedesignplus/CodeDesignPlus.Net.Microservice.Notification.git
   cd CodeDesignPlus.Net.Microservice.Notification
   ```

2. **Start infrastructure services:**
   ```bash
   # Clone environment repository
   git clone https://github.com/codedesignplus/CodeDesignPlus.Environment.Dev.git
   cd CodeDesignPlus.Environment.Dev/resources
   
   # Start MongoDB, Redis, RabbitMQ, Vault, OTel Collector
   docker-compose up -d
   ```

3. **Configure Vault (optional):**
   ```bash
   cd ../../CodeDesignPlus.Net.Microservice.Notification/tools/vault
   ./config-vault.sh
   ```

4. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

5. **Build the solution:**
   ```bash
   dotnet build
   ```

6. **Run the gRPC entrypoint:**
   ```bash
   dotnet run --project src/entrypoints/CodeDesignPlus.Net.Microservice.Notification.gRpc
   ```

7. **Service endpoints:**
   - **SignalR Hub**: `http://localhost:5000/hubs/notifications`
   - **gRPC Service**: `http://localhost:5001` (HTTP/2)
   - **Health Checks**: 
     - `http://localhost:5000/health/live`
     - `http://localhost:5000/health/ready`

### Testing SignalR Connection

**JavaScript Client Example:**

```javascript
// Install: npm install @microsoft/signalr

const signalR = require('@microsoft/signalr');

const connection = new signalR.HubConnectionBuilder()
    .withUrl('http://localhost:5000/hubs/notifications', {
        accessTokenFactory: () => 'YOUR_JWT_TOKEN'
    })
    .withAutomaticReconnect()
    .build();

connection.on('TestEvent', (payload) => {
    console.log('Received notification:', JSON.parse(payload));
});

await connection.start();
console.log('Connected to SignalR hub');

// Join a group
await connection.invoke('JoinGroup', 'TestGroup');
```

### Testing gRPC Service

**Using grpcurl (gRPC Reflection):**

```bash
# List services
grpcurl -plaintext localhost:5001 list

# Stream notifications to user
grpcurl -plaintext -d @ localhost:5001 Notifications.Notifier/SendToUser <<EOM
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "eventName": "OrderUpdated",
  "jsonPayload": "{\"orderId\": \"12345\", \"status\": \"Shipped\"}",
  "tenant": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "sentBy": "550e8400-e29b-41d4-a716-446655440001"
}
EOM
```

**C# Client Example:**

```csharp
using Grpc.Net.Client;
using CodeDesignPlus.Net.Microservice.Notification.gRpc;

var channel = GrpcChannel.ForAddress("http://localhost:5001");
var client = new Notifier.NotifierClient(channel);

using var call = client.SendToUser();

// Send notification
await call.RequestStream.WriteAsync(new NotificationUserRequest
{
    Id = Guid.NewGuid().ToString(),
    UserId = "123e4567-e89b-12d3-a456-426614174000",
    EventName = "OrderUpdated",
    JsonPayload = "{\"orderId\": \"12345\", \"status\": \"Shipped\"}",
    Tenant = "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    SentBy = Guid.NewGuid().ToString()
});

// Receive response
await foreach (var response in call.ResponseStream.ReadAllAsync())
{
    Console.WriteLine($"Success: {response.Success}, Message: {response.Message}");
}
```

## 🧪 Testing

### Run All Tests

```bash
# Run unit + integration tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportFormat=opencover
```

### Test Structure

```
tests/
├── unit/
│   ├── Domain.Test/           # Aggregate logic, domain events
│   ├── Application.Test/      # Command handlers, validators
│   ├── Infrastructure.Test/   # Repository, adapters
│   └── gRpc.Test/            # gRPC service logic
└── integration/
    └── gRpc.Test/            # End-to-end gRPC + SignalR tests
```

### Integration Test Example

```csharp
[Collection("Server")]
public class NotificationsServiceTests
{
    private readonly GrpcChannel _channel;
    private readonly Notifier.NotifierClient _client;
    
    [Fact]
    public async Task SendToUser_Should_Return_Success()
    {
        // Arrange
        using var call = _client.SendToUser();
        
        var request = new NotificationUserRequest
        {
            Id = Guid.NewGuid().ToString(),
            UserId = TestData.UserId,
            EventName = "TestEvent",
            JsonPayload = "{\"message\": \"Hello\"}",
            Tenant = TestData.TenantId,
            SentBy = Guid.NewGuid().ToString()
        };
        
        // Act
        await call.RequestStream.WriteAsync(request);
        await call.RequestStream.CompleteAsync();
        
        // Assert
        var response = await call.ResponseStream.ReadAllAsync().FirstAsync();
        Assert.True(response.Success);
    }
}
```

## 🐳 Docker Support

### Build Docker Image

```bash
docker build -t ms-notification-grpc:latest \
  -f src/entrypoints/CodeDesignPlus.Net.Microservice.Notification.gRpc/Dockerfile \
  .
```

### Run Container

```bash
docker run -d \
  --name ms-notification-grpc \
  --network backend \
  -p 5000:5000 \
  -p 5001:5001 \
  -e ASPNETCORE_ENVIRONMENT=Docker \
  -e Vault__Enable=false \
  -e Mongo__ConnectionString=mongodb://mongo:27017 \
  -e Redis__Instances__Core__ConnectionString=redis:6379 \
  -e RabbitMQ__Host=rabbitmq \
  ms-notification-grpc:latest
```

### Docker Compose Example

```yaml
version: '3.8'

services:
  ms-notification-grpc:
    image: codedesignplus/ms-notification-grpc:v0.1.0-alpha.100
    ports:
      - "5000:5000"
      - "5001:5001"
    environment:
      ASPNETCORE_ENVIRONMENT: Docker
      Mongo__ConnectionString: mongodb://mongo:27017
      Redis__Instances__Core__ConnectionString: redis:6379
      RabbitMQ__Host: rabbitmq
    depends_on:
      - mongo
      - redis
      - rabbitmq
    networks:
      - backend
```

## ☸️ Kubernetes Deployment

### Helm Chart

Located in `charts/ms-notification-grpc/`

**Install to Staging:**

```bash
helm upgrade --install ms-notification-grpc \
  ./charts/ms-notification-grpc \
  -f ./charts/ms-notification-grpc/Staging.yaml \
  --namespace urbancore \
  --create-namespace
```

**Key Configuration (Staging.yaml):**

```yaml
ms-base:
  image:
    repository: codedesignplus/ms-notification-grpc
    tag: "v0.1.0-alpha.100"
  
  service:
    type: ClusterIP
    ports:
      - name: http
        port: 5000
        targetPort: http
        protocol: TCP
      - name: grpc
        port: 5001
        targetPort: grpc
        protocol: TCP
  
  virtualService:
    create: true
    hosts:
      - services.codedesignplus.com
    http:
      - match:
        - uri:
            prefix: /ms-notification/
        rewrite:
          uri: /
        route:
        - destination:
            host: ms-notification-grpc.urbancore.svc.cluster.local
            port:
              number: 5000
```

### Service Account & RBAC

The Helm chart creates a ServiceAccount with Vault integration for secret retrieval.

### Health Probes

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: http
readinessProbe:
  httpGet:
    path: /health/ready
    port: http
```

## 🔒 Security

### Authentication

- **SignalR Hub**: JWT Bearer token required ([Authorize] attribute)
- **gRPC Service**: Can be secured with Interceptors (currently open for internal use)

### Authorization Claims

Required JWT claims for SignalR:
- `sub` or custom `UserId` claim (mapped by UserIdProvider)
- `tenant` claim (for tenant isolation)

### Tenant Isolation

All notifications are scoped by tenant:
1. Tenant extracted from JWT on SignalR connect
2. Connections auto-join `Tenant:{TenantId}` group
3. Broadcast operations only affect tenant group members
4. User notifications filtered by tenant in UserIdProvider

### Secret Management

Credentials stored in HashiCorp Vault:
- **MongoDB**: Dynamic credentials via `mongodb://{user}:{pass}@host`
- **RabbitMQ**: User/password from Vault KV store

**Vault Configuration:**
```json
{
  "Vault": {
    "Enable": true,
    "Address": "http://vault:8200",
    "AppName": "ms-notification",
    "Solution": "security-codedesignplus",
    "Mongo": {
      "Enable": true,
      "TemplateConnectionString": "mongodb://{0}:{1}@mongo:27017"
    }
  }
}
```

## 📊 Observability

### Distributed Tracing

OpenTelemetry instrumentation for:
- ASP.NET Core HTTP requests
- gRPC calls
- SignalR Hub operations
- MongoDB operations
- Redis cache operations

**Configuration:**
```json
{
  "Observability": {
    "Enable": true,
    "ServerOtel": "http://otel-collector:4317",
    "Trace": {
      "Enable": true,
      "AspNetCore": true,
      "CodeDesignPlusSdk": true,
      "Redis": true
    }
  }
}
```

### Structured Logging

Serilog with JSON output including:
- Correlation IDs
- Tenant context
- User IDs
- SignalR connection lifecycle
- Notification delivery results

### Metrics

Exposed via OpenTelemetry:
- HTTP request duration
- gRPC call counts/durations
- SignalR connection counts
- Notification delivery success/failure rates
- MongoDB operation latency

### Health Checks

- **Liveness** (`/health/live`): Service is running
- **Readiness** (`/health/ready`): Dependencies (MongoDB, Redis, RabbitMQ) are available

## 🔧 Development Tools

### Update NuGet Packages

```bash
cd tools/update-packages
./update-packages.sh
```

### Upgrade .NET Version

```bash
cd tools/upgrade-dotnet
./upgrade-dotnet.sh
```

### SonarQube Analysis

```bash
cd tools/sonarqube
./sonarqube.sh
```

### Line Ending Normalization

```bash
cd tools
./convert-crlf-to-lf.sh
```

## 🏗️ Integration Patterns

### Producer Microservice Example

Any microservice can send notifications via gRPC:

```csharp
// In your producer microservice (e.g., ms-orders)
public class OrderEventHandler
{
    private readonly Notifier.NotifierClient _notifierClient;
    
    public async Task OnOrderStatusChanged(OrderAggregate order)
    {
        using var call = _notifierClient.SendToUser();
        
        await call.RequestStream.WriteAsync(new NotificationUserRequest
        {
            Id = Guid.NewGuid().ToString(),
            UserId = order.CustomerId.ToString(),
            EventName = "OrderStatusChanged",
            JsonPayload = JsonSerializer.Serialize(new
            {
                orderId = order.Id,
                status = order.Status,
                updatedAt = DateTime.UtcNow
            }),
            Tenant = order.Tenant.ToString(),
            SentBy = "ms-orders"
        });
        
        var response = await call.ResponseStream.ReadAllAsync().FirstAsync();
        // Handle response
    }
}
```

### Frontend Integration Example

**React with SignalR:**

```typescript
import { HubConnectionBuilder, HubConnection } from '@microsoft/signalr';
import { useEffect, useState } from 'react';

export const useNotifications = () => {
  const [connection, setConnection] = useState<HubConnection | null>(null);

  useEffect(() => {
    const connect = async () => {
      const hubConnection = new HubConnectionBuilder()
        .withUrl('/hubs/notifications', {
          accessTokenFactory: () => localStorage.getItem('jwt') || ''
        })
        .withAutomaticReconnect()
        .build();

      // Order notifications
      hubConnection.on('OrderStatusChanged', (payload) => {
        const data = JSON.parse(payload);
        toast.success(`Order ${data.orderId} is now ${data.status}`);
      });

      // Payment notifications
      hubConnection.on('PaymentProcessed', (payload) => {
        const data = JSON.parse(payload);
        updatePaymentUI(data);
      });

      await hubConnection.start();
      setConnection(hubConnection);
    };

    connect();

    return () => {
      connection?.stop();
    };
  }, []);

  return connection;
};
```

### Redis Backplane (Optional)

For multi-instance deployments, enable Redis backplane:

**Program.cs:**
```csharp
builder.Services.AddSignalR()
    .AddStackExchangeRedis(options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("Notify_");
    });
```

**Benefits:**
- Horizontal scaling across multiple pods
- Notifications reach users regardless of connected instance
- Sticky sessions not required

## 📈 Performance Considerations

### Scalability

- **Horizontal Scaling**: Deploy multiple replicas with Redis backplane
- **Connection Limits**: Default 100 concurrent connections per instance
- **Payload Limits**: Recommended max 64KB per notification
- **MongoDB Indexing**: Index on `userId`, `tenant`, `sentAt` for audit queries

### Best Practices

1. **Payload Size**: Keep JSON payloads small; include IDs, not full entities
2. **Event Names**: Use PascalCase convention (e.g., `OrderUpdated`, `PaymentFailed`)
3. **Group Naming**: Follow convention `Tenant:{TenantId}:{Feature}` 
4. **Connection Management**: Implement reconnection logic with exponential backoff
5. **Audit Cleanup**: Implement TTL indexes on MongoDB for old notifications
6. **Error Handling**: Always handle failed deliveries in producers

## 🤝 Contributing

Please read our [Contributing Guide](https://github.com/codedesignplus/.github/blob/main/CONTRIBUTING.md) for details on our code of conduct and development process.

## 📄 License

This project is licensed under the **GNU Lesser General Public License v3.0** - see the [LICENSE.md](LICENSE.md) file for details.

## 📚 Related Documentation

- [CodeDesignPlus.Net.Sdk Documentation](https://codedesignplus.github.io/)
- [SignalR Documentation](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction)
- [gRPC Streaming](https://grpc.io/docs/what-is-grpc/core-concepts/#bidirectional-streaming-rpc)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)
- [HashiCorp Vault](https://developer.hashicorp.com/vault/docs)

## 🆘 Support

For issues, questions, or contributions:
- **Email**: wliscano@codedesignplus.com
- **GitHub Issues**: [Create an issue](https://github.com/codedesignplus/CodeDesignPlus.Net.Microservice.Notification/issues)
- **Organization**: CodeDesignPlus

---

**Built with ❤️ by CodeDesignPlus** | .NET 9 | Clean Architecture | DDD | CQRS
