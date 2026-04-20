# RS System - Church Management System

## Project Overview

RS System is a comprehensive church management system built with ASP.NET Core 9.0 (formerly known as "foundation_system"). It provides modules for church administration, member management, accounting, inventory, offerings, and reporting.

**Primary Purpose**: Church administration and management system for Roca Church (Iglesia Roca).

**Core Technologies**:
- **Backend**: ASP.NET Core 9.0 Web Application (MVC pattern)
- **Database**: PostgreSQL with Entity Framework Core
- **Authentication**: ASP.NET Core Identity with custom role-based permissions
- **Frontend**: Razor Pages with server-side rendering
- **Containerization**: Docker with Docker Compose

## Architecture

### Project Structure
```
RS_system/
├── Areas/                    # Feature areas (if any)
├── Components/              # View Components (e.g., MenuViewComponent)
├── Controllers/            # MVC Controllers (23 controllers)
├── Data/                   # Data access layer
│   ├── ApplicationDbContext.cs
│   ├── DbContextOptimizationExtensions.cs
│   ├── PostgresQueryExecutor.cs
│   └── PostgresScalarExecutor.cs
├── Filters/                # Action filters
├── Helpers/                # Utility classes
├── Migrations/             # Entity Framework migrations
├── Models/                 # Entity models (42 models)
│   ├── Enums/             # Enumeration types
│   └── ViewModels/        # View models
├── Services/              # Business logic services
├── Views/                 # Razor views
├── wwwroot/               # Static files
├── Program.cs             # Application entry point
└── RS_system.csproj       # Project configuration
```

### Key Modules
1. **Authentication & Authorization**: Custom role-based permission system
2. **Member Management**: Church member records and groups
3. **Accounting**: General church accounting with income/expense categories
4. **Inventory**: Church asset/inventory management
5. **Offerings & Tithes**: Offering collection and management
6. **Reports**: Financial and operational reporting
7. **Collaborations**: Member collaboration tracking
8. **Loans**: Church loan management system

## Database Configuration

### Connection Strings
- **Primary**: PostgreSQL database at `158.220.108.49:35432` (Roca_ChurhDB)
- **Secondary**: Local SQL Server for development

### Key Database Entities
- **Users & Roles**: Custom authentication system with roles and permissions
- **Members**: Church member information
- **Accounting**: Financial transactions with categories
- **Inventory**: Church assets with categories, statuses, and locations
- **Offerings**: Church offering records
- **Reports**: Monthly financial reports

## Building and Running

### Development Environment
```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run with development settings
dotnet run --environment Development

# Apply database migrations
dotnet ef database update
```

### Docker Deployment
```bash
# Build and run with Docker Compose
docker-compose up --build

# Access the application
# Web interface: http://localhost:4000
# Application port: 8080 (mapped to 4000)
# Additional port: 8081 (mapped to 8081)
```

### Production Deployment
The application is configured for production with:
- PostgreSQL connection pooling (1-20 connections)
- Command timeout: 30 seconds
- Retry on failure: 3 attempts with 5-second delays
- No-tracking queries by default for performance

## Development Conventions

### Code Style
- **C# 9.0+ features**: Nullable reference types enabled, implicit usings
- **Naming**: PascalCase for classes/methods, camelCase for parameters
- **Organization**: Controllers in `Controllers/`, Models in `Models/`, Services in `Services/`

### Entity Framework Optimizations
The project includes significant EF Core optimizations documented in `OPTIMIZACIONES_EF.md`:

1. **Connection Configuration**:
   - Retry on failure (3 attempts, 5-second delay)
   - Command timeout: 30 seconds
   - No-tracking queries by default
   - Detailed logging only in development

2. **Database Indexes**: Strategic indexes for frequently queried columns
3. **Query Optimization**: Reduced database round-trips with optimized joins
4. **Caching**: Menu caching with 15-minute expiration

### Service Layer Pattern
- Business logic encapsulated in `Services/` directory
- Dependency injection configured in `Program.cs`
- Scoped lifetime for most services

### Testing
- No test projects found in current structure
- Consider adding xUnit or NUnit tests for critical functionality

## Configuration Files

### `appsettings.json`
- Database connection strings
- Logging configuration (Information level default, Warning for ASP.NET Core)
- All hosts allowed (`*`)

### `appsettings.Development.json`
- Development-specific settings (if any)

### `docker-compose.yml`
- Single service configuration
- Port mapping: 4000:8080, 8081:8081
- Production environment
- Restart policy: unless-stopped

## Database Management

### Migration Commands
```bash
# Create a new migration
dotnet ef migrations add <MigrationName>

# Update database
dotnet ef database update

# Generate SQL script
dotnet ef migrations script
```

### SQL Files
- `recibos_generados.sql`: Receipt generation table
- Various SQL files in project root for specific modules

## Security Considerations

### Authentication
- Custom authentication service (`IAuthService`)
- BCrypt for password hashing
- Role-based permission system
- Session management with cookies

### Database Security
- Connection strings stored in configuration
- PostgreSQL with SSL/TLS recommended for production
- Parameterized queries via Entity Framework

## Performance Optimizations

### Implemented
1. **Query Optimization**: No-tracking queries, optimized joins
2. **Caching**: Menu data cached for 15 minutes
3. **Connection Pooling**: PostgreSQL connection pooling configured
4. **Indexing**: Strategic database indexes

### Recommended
1. **Response Caching**: For frequently accessed reports
2. **Database Read Replicas**: For reporting queries
3. **CDN**: For static assets in `wwwroot/`

## Troubleshooting

### Common Issues
1. **Database Connection**: Verify PostgreSQL server is accessible
2. **Migration Errors**: Ensure all migrations are applied
3. **Permission Issues**: Check role and permission assignments

### Logging
- Default logging level: Information
- ASP.NET Core warnings logged
- Enable detailed errors in development only

## Future Development Considerations

1. **API Layer**: Consider adding Web API controllers for mobile apps
2. **Real-time Features**: SignalR for live updates
3. **Export Features**: PDF/Excel export for reports
4. **Audit Logging**: Comprehensive audit trail
5. **Backup System**: Automated database backups

## Notes
- Project was originally named "foundation_system" in solution file
- Uses PostgreSQL-specific features (enums, custom types)
- Includes Spanish-language naming conventions in code
- Optimized for church administration workflows