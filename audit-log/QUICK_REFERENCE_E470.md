# E470.AuditLog - Quick Reference Card

## 🚀 Quick Commands

### Build & Test
```bash
# Clean and build
dotnet clean E470.AuditLog.sln
dotnet build E470.AuditLog.sln --configuration Release

# Run tests
dotnet test E470.AuditLog.sln

# Run with coverage
dotnet test E470.AuditLog.sln /p:CollectCoverage=true
```

### Run Application

#### Aspire AppHost (Recommended)
```bash
cd audit-log
dotnet run --project src/E470.AudiLog.AppHost/E470.AuditLog.AppHost.csproj
# Open: http://localhost:15000
```

#### Web API Directly
```bash
cd audit-log/src/E470.AuditLog.Web.Api
dotnet run
# Open: https://localhost:5001/swagger
```

#### Docker
```bash
cd audit-log/docker
docker-compose -f compose.e470-webapi.yml up --build
# API: http://localhost:5000
```

---

## 📁 Project Structure

```
E470.AuditLog.sln
├── src/
│   ├── E470.AuditLog.SharedKernel/       # Domain primitives
│   ├── E470.AuditLog.Domain/             # Business entities
│   ├── E470.AuditLog.Application/        # Use cases (CQRS)
│   ├── E470.AuditLog.Infrastructure/     # Data access & external
│   ├── E470.AuditLog.EventBusClient/     # Event bus integration
│   ├── E470.AuditLog.Web.Api/            # REST API
│   ├── E470.AudiLog.AppHost/             # Aspire orchestration
│   └── E470.AuditLog.ServiceDefaults/    # Aspire config
└── tests/
    └── E470.AuditLog.ArchitectureTests/  # Architecture validation
```

---

## 🔧 Migration Commands

### Automated Migration
```bash
# Windows
.\complete-e470-migration.ps1

# Linux/macOS
chmod +x complete-e470-migration.sh
./complete-e470-migration.sh
```

### Dry Run (Safe Test)
```bash
# Windows
.\complete-e470-migration.ps1 -DryRun

# Linux/macOS
./complete-e470-migration.sh --dry-run
```

---

## 📚 Key Documentation

| Document | Purpose | Read Time |
|----------|---------|-----------|
| **QUICK_START_E470.md** | Get started fast | 5 min |
| **COMPLETE_E470_MIGRATION_GUIDE.md** | Full migration guide | 30 min |
| **MIGRATION_PLAN_E470.md** | Detailed plan | 20 min |
| **PR1_FINAL_SUMMARY.md** | PR overview | 15 min |
| **DOCUMENTATION_INDEX.md** | Navigation hub | 5 min |

---

## 🐳 Docker Quick Reference

### Build & Run
```bash
cd audit-log/docker

# Build and start
docker-compose -f compose.e470-webapi.yml up --build

# Start in background
docker-compose -f compose.e470-webapi.yml up -d

# View logs
docker-compose -f compose.e470-webapi.yml logs -f

# Stop
docker-compose -f compose.e470-webapi.yml down
```

### Database Access
```bash
# Connect to SQL Server
docker exec -it mssql /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "Pass@word1"

# Check databases
SELECT name FROM sys.databases;
GO
```

---

## 🔍 Common Tasks

### Add New Migration
```bash
cd audit-log/src/E470.AuditLog.Infrastructure
dotnet ef migrations add <MigrationName> --startup-project ../E470.AuditLog.Web.Api
```

### Update Database
```bash
cd audit-log/src/E470.AuditLog.Infrastructure
dotnet ef database update --startup-project ../E470.AuditLog.Web.Api
```

### Generate OpenAPI Spec
```bash
cd audit-log/src/E470.AuditLog.Web.Api
dotnet run --urls "https://localhost:5001"
# Access: https://localhost:5001/swagger/v1/swagger.json
```

---

## 🧪 Testing

### Run Specific Tests
```bash
# Architecture tests
dotnet test tests/E470.AuditLog.ArchitectureTests/E470.AuditLog.ArchitectureTests.csproj

# With filter
dotnet test --filter "TestCategory=Architecture"
```

### Check Health
```bash
# Health endpoint
curl http://localhost:5000/health

# Detailed health
curl http://localhost:5000/health/ready
```

---

## 🛠️ Troubleshooting

### Clear Build Artifacts
```bash
# Clean all
dotnet clean E470.AuditLog.sln
find . -name "bin" -type d -exec rm -rf {} +
find . -name "obj" -type d -exec rm -rf {} +

# Restore
dotnet restore E470.AuditLog.sln
```

### Fix Project References
```bash
# List all projects
dotnet sln E470.AuditLog.sln list

# Remove old reference
dotnet sln E470.AuditLog.sln remove src/OldProject/OldProject.csproj

# Add new reference
dotnet sln E470.AuditLog.sln add src/E470.AuditLog.NewProject/E470.AuditLog.NewProject.csproj
```

### Docker Issues
```bash
# Remove all containers and volumes
docker-compose -f compose.e470-webapi.yml down -v

# Rebuild from scratch
docker-compose -f compose.e470-webapi.yml build --no-cache
docker-compose -f compose.e470-webapi.yml up
```

---

## 📊 Namespace Mapping

| Old Namespace | New Namespace |
|---------------|---------------|
| `SharedKernel` | `E470.AuditLog.SharedKernel` |
| `Domain` | `E470.AuditLog.Domain` |
| `Application` | `E470.AuditLog.Application` |
| `Infrastructure` | `E470.AuditLog.Infrastructure` |
| `EventBusClient` | `E470.AuditLog.EventBusClient` |
| `Web.Api` | `E470.AuditLog.Web.Api` |

---

## 🎯 API Endpoints

### Authentication
```bash
# Register
POST /api/users/register
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "firstName": "John",
  "lastName": "Doe"
}

# Login
POST /api/users/login
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

### Todos
```bash
# Get all todos
GET /api/todos
Authorization: Bearer <token>

# Create todo
POST /api/todos
Authorization: Bearer <token>
{
  "description": "My task",
  "priority": 2,
  "dueDate": "2025-12-31"
}

# Complete todo
PUT /api/todos/{id}/complete
Authorization: Bearer <token>
```

---

## 🔐 Configuration

### Connection Strings

#### Development (appsettings.Development.json)
```json
{
  "ConnectionStrings": {
    "audit-log-db": "Server=localhost;Database=E470AuditLogDb;User Id=sa;Password=Pass@word1;TrustServerCertificate=True;"
  }
}
```

#### Docker (compose.e470-webapi.yml)
```yaml
environment:
  - ConnectionStrings__audit-log-db=Server=mssql;Database=E470AuditLogDb;User Id=sa;Password=Pass@word1;TrustServerCertificate=True;
```

### JWT Settings

```json
{
  "Jwt": {
    "Secret": "<your-secret-key-min-32-chars>",
    "Issuer": "E470.AuditLog",
    "Audience": "E470.AuditLog.Api",
    "ExpirationInMinutes": 60
  }
}
```

---

## ⚙️ Environment Variables

| Variable | Purpose | Example |
|----------|---------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment | `Development` |
| `ConnectionStrings__audit-log-db` | Database | `Server=...` |
| `Jwt__Secret` | JWT secret | `your-secret-key` |
| `ASPNETCORE_HTTP_PORTS` | HTTP port | `8080` |
| `ASPNETCORE_HTTPS_PORTS` | HTTPS port | `8081` |

---

## 📦 NuGet Packages

### Core
- Microsoft.EntityFrameworkCore.SqlServer: 10.0.2
- ASP.NET Core: 10.0.2
- Aspire: 13.1.0

### Testing
- NUnit: 4.2.2
- NetArchTest.Rules: 1.3.2

### Libraries
- FluentValidation: 12.1.1
- Serilog: 4.2.0
- DotNetCore.CAP: 10.0.1

---

## 🎓 Learning Path

### New to Project (1-2 hours)
1. Read `README.md`
2. Read `QUICK_START_E470.md`
3. Run application locally
4. Explore Swagger UI
5. Review architecture tests

### Migration (30-45 minutes)
1. Read `COMPLETE_E470_MIGRATION_GUIDE.md`
2. Run migration script
3. Verify build
4. Test locally

### Deep Dive (1 day)
1. Study Clean Architecture layers
2. Review CQRS implementation
3. Understand Domain Events
4. Explore Infrastructure patterns
5. Review API endpoints

---

## 🚨 Important Notes

### ⚠️ Breaking Changes
- **None!** - This is a rename only, no functionality changed

### ✅ Backwards Compatibility
- All APIs remain the same
- Database schemas unchanged
- Configurations preserved

### 📝 Git Workflow
```bash
# Feature branch
git checkout -b feature/your-feature

# Commit
git add .
git commit -m "Add: your feature"

# Push
git push origin feature/your-feature

# Create PR
# Target: main branch
```

---

## 💡 Tips & Best Practices

### Development
- ✅ Use Aspire AppHost for local development
- ✅ Run tests before committing
- ✅ Follow Clean Architecture boundaries
- ✅ Keep domain logic in Domain layer
- ✅ Use Result pattern for error handling

### Testing
- ✅ Write architecture tests for new features
- ✅ Test business logic in isolation
- ✅ Use integration tests for infrastructure
- ✅ Mock external dependencies

### Docker
- ✅ Use docker-compose for local environment
- ✅ Check health endpoints
- ✅ Monitor container logs
- ✅ Clean volumes when schema changes

---

## 📞 Help & Support

### Documentation
- Start with `DOCUMENTATION_INDEX.md` for navigation
- Check troubleshooting sections in guides
- Review architecture tests for examples

### Common Issues
- Build errors → Check project references
- Namespace errors → Run migration script
- Docker issues → Clear volumes and rebuild
- Database errors → Check connection string

### Getting Help
1. Check documentation
2. Search issues on GitHub
3. Ask team in chat
4. Create detailed issue

---

## ✨ Quick Wins

### First 5 Minutes
```bash
git clone <repo>
cd audit-log
dotnet build E470.AuditLog.sln
dotnet run --project src/E470.AudiLog.AppHost/E470.AuditLog.AppHost.csproj
```

### First Hour
- Explore Aspire Dashboard
- Test API endpoints in Swagger
- Review architecture
- Read documentation

### First Day
- Complete migration (if needed)
- Add new feature
- Write tests
- Submit PR

---

**Need more details? Check `DOCUMENTATION_INDEX.md` for complete guide list!**

---

**Quick Reference Version**: 1.0.0  
**Last Updated**: 2025  
**Print this card**: Keep it handy! 📄
