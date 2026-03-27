# qasist-backend
# QAsist - QA Management System

A production-ready ASP.NET Core 8 Web API built with Clean Architecture, PostgreSQL, Dapper, and JWT authentication.

## 🏗️ Architecture

This project follows **Clean Architecture** principles with clear separation of concerns:

```
QAsist/
├── src/
│   ├── QAsist.Api/              # Presentation Layer
│   ├── QAsist.Application/      # Application Layer
│   ├── QAsist.Domain/           # Domain Layer
│   └── QAsist.Infrastructure/   # Infrastructure Layer
└── Database/                    # PostgreSQL Scripts
```

## 🎯 Features

- ✅ Clean Architecture with SOLID principles
- ✅ PostgreSQL with Dapper (NO Entity Framework)
- ✅ Stored Procedures & Functions for all database operations
- ✅ JWT Authentication with Refresh Tokens
- ✅ Multi-tab session management
- ✅ Role-Based Access Control (RBAC)
- ✅ Global Exception Handling
- ✅ Structured Logging with Serilog
- ✅ AutoMapper for DTO mapping
- ✅ Correlation ID tracking
- ✅ Swagger/OpenAPI documentation
- ✅ Async/await throughout
- ✅ Centralized response models
- ✅ Centralized error messages
- ✅ UUID primary keys

## 📋 Prerequisites

- .NET 8 SDK
- PostgreSQL 14+
- Visual Studio 2022 / VS Code / Rider

## 🚀 Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd QAsist
```

### 2. Create Solution Structure

Run the commands from the setup section above to create all projects and add references.

### 3. Install Additional NuGet Package

```bash
dotnet add src/QAsist.Infrastructure/QAsist.Infrastructure.csproj package BCrypt.Net-Next
```

### 4. Setup PostgreSQL Database

```bash
# Create database
psql -U postgres
CREATE DATABASE qasist_db;
\q

# Run schema scripts in order
psql -U postgres -d qasist_db -f Database/01_schema.sql
psql -U postgres -d qasist_db -f Database/02_project_functions.sql
psql -U postgres -d qasist_db -f Database/03_user_functions.sql
psql -U postgres -d qasist_db -f Database/04_refresh_token_functions.sql
```

### 5. Configure Connection String

Update `appsettings.json` in QAsist.Api:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=qasist_db;Username=postgres;Password=yourpassword"
  }
}
```

### 6. Run the Application

```bash
cd src/QAsist.Api
dotnet run
```

The API will be available at:
- https://localhost:7001
- http://localhost:5001

Swagger UI: https://localhost:7001

## 🔐 Authentication & Authorization

### User Roles

```csharp
public enum UserRole
{
    SuperAdmin = 1,      // Full system access
    Admin = 2,           // Administrative access
    ProjectManager = 3,  // Project management
    QALead = 4,          // QA team lead
    QAEngineer = 5,      // QA engineer
    Developer = 6,       // Developer access
    Viewer = 7           // Read-only access
}
```

### Default User

```
Email: admin@qasist.com
Password: Admin@123
Role: SuperAdmin
```

### JWT Claims

All JWTs include the following claims:
- `sub` - User ID
- `email` - User email
- `given_name` - First name
- `family_name` - Last name
- `role` - User role
- `SessionId` - Unique session identifier
- `IsActive` - Account status

### Multi-Tab Session Management

The system supports multiple browser tabs with independent sessions:

1. Each tab gets a unique `sessionId` (generated client-side)
2. Refresh tokens are tied to specific sessions
3. Logging out one tab doesn't affect others
4. Use `logout-all` endpoint to revoke all sessions

### API Endpoints

#### Authentication

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "admin@qasist.com",
  "password": "Admin@123",
  "sessionId": "unique-session-id"
}
```

```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "token-here",
  "sessionId": "session-id-here"
}
```

```http
POST /api/auth/logout
Authorization: Bearer {token}
Content-Type: application/json

"session-id-here"
```

#### Projects

```http
GET /api/projects
GET /api/projects/{id}
GET /api/projects/paged?pageNumber=1&pageSize=10
POST /api/projects
PUT /api/projects/{id}
DELETE /api/projects/{id}
```

## 🗂️ Project Structure

### Domain Layer (`QAsist.Domain`)

Contains core business entities and enums. No dependencies on other layers.

```
Domain/
├── Entities/
│   ├── BaseEntity.cs
│   ├── Project.cs
│   ├── User.cs
│   └── RefreshToken.cs
└── Enums/
    └── Enums.cs
```

### Application Layer (`QAsist.Application`)

Contains business logic, interfaces, DTOs, and mappings.

```
Application/
├── Common/
│   ├── Exceptions/
│   │   └── CustomExceptions.cs
│   └── Responses/
│       ├── ApiResponse.cs
│       └── ResponseMessages.cs
├── DTOs/
│   ├── ProjectDtos.cs
│   └── AuthDtos.cs
├── Interfaces/
│   ├── IRepositories/
│   │   ├── IProjectRepository.cs
│   │   ├── IUserRepository.cs
│   │   └── IRefreshTokenRepository.cs
│   └── IServices/
│       ├── IProjectService.cs
│       ├── IAuthService.cs
│       └── IJwtService.cs
├── Mappings/
│   └── MappingProfile.cs
├── Services/
│   └── ProjectService.cs
└── DependencyInjection.cs
```

### Infrastructure Layer (`QAsist.Infrastructure`)

Contains data access, external services, and infrastructure concerns.

```
Infrastructure/
├── Persistence/
│   ├── DbConnectionFactory.cs
│   └── SqlQueries.cs
├── Repositories/
│   ├── ProjectRepository.cs
│   ├── UserRepository.cs
│   └── RefreshTokenRepository.cs
├── Services/
│   ├── JwtService.cs
│   └── AuthService.cs
└── DependencyInjection.cs
```

### API Layer (`QAsist.Api`)

Contains controllers, middleware, filters, and startup configuration.

```
Api/
├── Controllers/
│   ├── HealthController.cs
│   ├── ProjectsController.cs
│   └── AuthController.cs
├── Middleware/
│   ├── GlobalExceptionMiddleware.cs
│   ├── CorrelationIdMiddleware.cs
│   └── RequestLoggingMiddleware.cs
├── Filters/
│   ├── AuthorizeRolesAttribute.cs
│   └── ValidationFilter.cs
├── Extensions/
│   ├── ClaimsPrincipalExtensions.cs
│   └── HttpContextExtensions.cs
├── Program.cs
└── appsettings.json
```

## 🗄️ Database Design

### Centralized SQL Queries

All SQL queries are stored in `SqlQueries.cs`:

```csharp
public static class SqlQueries
{
    public static class Projects
    {
        public const string GetById = "SELECT * FROM get_project_by_id(@p_id);";
        public const string Create = "SELECT create_project(...);";
        // ...
    }
}
```

### Stored Procedures & Functions

All database operations use PostgreSQL functions:

- `get_project_by_id(uuid)` - Retrieve project by ID
- `create_project(...)` - Create new project
- `update_project(...)` - Update existing project
- `delete_project(uuid, uuid)` - Soft delete project

## 📝 Logging

Logs are written to:
- Console (colored, structured)
- Files: `logs/qasist-{Date}.log`

Each request includes:
- Correlation ID
- Request method and path
- Response status
- Execution duration

## 🛡️ Error Handling

Global exception middleware handles all exceptions:

```csharp
try {
    // Your code
} catch (NotFoundException ex) {
    // Returns 404
} catch (ValidationException ex) {
    // Returns 400 with validation errors
} catch (UnauthorizedException ex) {
    // Returns 401
}
```

Controllers don't need try-catch blocks.

## 📊 Response Format

All API responses follow a consistent structure:

```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": { ... },
  "errors": [],
  "correlationId": "abc-123-def",
  "timestamp": "2026-01-25T10:30:00Z"
}
```

Paged responses include:

```json
{
  "totalRecords": 100,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 10,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

## 🔧 Development Guidelines

### Adding New Features

1. **Domain**: Add entity to `Domain/Entities`
2. **Application**: 
   - Create DTOs
   - Create interface in `Interfaces`
   - Implement service in `Services`
3. **Infrastructure**:
   - Add SQL queries to `SqlQueries.cs`
   - Create repository implementation
4. **Database**: Create stored procedures/functions
5. **API**: Add controller

### SOLID Principles

- **S**ingle Responsibility: Each class has one reason to change
- **O**pen/Closed: Open for extension, closed for modification
- **L**iskov Substitution: Subtypes must be substitutable
- **I**nterface Segregation: Many specific interfaces over one general
- **D**ependency Inversion: Depend on abstractions, not concretions

### Avoid Code Duplication (DRY)

- Use base classes for common functionality
- Extract reusable logic into services
- Centralize constants and messages

## 🧪 Testing

```bash
# Unit Tests (to be added)
dotnet test

# Integration Tests (to be added)
dotnet test --filter Category=Integration
```

## 📦 Deployment

### Docker (Optional)

```dockerfile
# Dockerfile example
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "QAsist.Api.dll"]
```

## 🤝 Contributing

1. Fork the repository
2. Create feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Open Pull Request

## 📄 License

This project is licensed under the MIT License.

## 👥 Authors

Your Name - Initial work

## 🙏 Acknowledgments

- Clean Architecture by Robert C. Martin
- ASP.NET Core Documentation
- Dapper Documentation
- PostgreSQL Documentation