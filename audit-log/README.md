# E470.AuditLog - Clean Architecture Template

> **✨ E470.AuditLog - Complete Migration**
> 
> Welcome to **E470.AuditLog** - a fully branded clean architecture template with comprehensive audit logging capabilities.
> 
> This project has been completely migrated from the original template to reflect the E470.AuditLog branding and enhanced functionality.

---

## 📚 Migration Notice

This repository represents the **E470-branded version** of the Clean Architecture template, specifically designed for audit logging scenarios. All namespaces, project names, and documentation have been updated to reflect the E470.AuditLog identity.

### Quick Documentation Links

- 📖 [Complete Migration Guide](COMPLETE_E470_MIGRATION_GUIDE.md) - Comprehensive migration documentation
- 🚀 [Quick Start Guide](QUICK_START_E470.md) - Get started with E470.AuditLog
- 📋 [Migration Plan](MIGRATION_PLAN_E470.md) - Detailed migration roadmap and status
- 📝 [PR1 Changes Summary](PR1_CHANGES_SUMMARY.md) - Summary of initial pull request changes
- 🔄 [Migration Status](MIGRATION_STATUS.md) - Current migration progress

---

## 🎯 What's New in E470

The E470.AuditLog template brings several enhancements:

- **Complete Branding**: All projects, namespaces, and documentation updated to E470.AuditLog
- **Audit Logging Focus**: Specialized for audit log management and tracking
- **Enhanced Documentation**: Comprehensive guides for setup, migration, and usage
- **Docker Support**: Pre-configured Docker Compose files for E470.AuditLog
- **Migration Tools**: Detailed migration scripts and guides for existing projects

---

## 📦 What's included in the template?

- **E470.AuditLog.SharedKernel** project with common Domain-Driven Design abstractions.
- **E470.AuditLog.Domain** layer with sample entities for audit logging scenarios.
- **E470.AuditLog.Application** layer with abstractions for:
  - CQRS (Command Query Responsibility Segregation)
  - Example use cases for audit log management
  - Cross-cutting concerns (logging, validation)
- **E470.AuditLog.Infrastructure** layer with:
  - Authentication
  - Permission authorization
  - EF Core with PostgreSQL
  - Serilog integration
- **E470.AuditLog.WebApi** presentation layer with REST API endpoints
- **Seq** for searching and analyzing structured logs
  - Seq is available at http://localhost:8081 by default
- **Testing** projects
  - E470.AuditLog.ArchitectureTests
  - Unit and integration testing support

---

## 🚀 Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Docker Desktop (for running PostgreSQL and Seq)
- Visual Studio 2022, Rider, or VS Code

### Build the Solution

```bash
dotnet build E470.AuditLog.sln
```

### Run with Docker

Start the required infrastructure services (PostgreSQL, Seq):

```bash
docker-compose -f compose.e470-webapi.yml up -d
```

### Run the API

```bash
dotnet run --project src/E470.AuditLog.WebApi/E470.AuditLog.WebApi.csproj
```

The API will be available at:
- HTTP: http://localhost:5000
- HTTPS: https://localhost:5001
- Swagger UI: https://localhost:5001/swagger

### Stop Docker Services

```bash
docker-compose -f compose.e470-webapi.yml down
```

---

## 🏗️ Project Structure

```
E470.AuditLog/
├── src/
│   ├── E470.AuditLog.SharedKernel/      # Common DDD abstractions
│   ├── E470.AuditLog.Domain/            # Domain entities and logic
│   ├── E470.AuditLog.Application/       # Use cases and CQRS
│   ├── E470.AuditLog.Infrastructure/    # External concerns
│   └── E470.AuditLog.WebApi/           # API presentation layer
├── tests/
│   └── E470.AuditLog.ArchitectureTests/ # Architecture validation
├── docs/                                 # Additional documentation
├── compose.e470-webapi.yml              # Docker Compose configuration
└── E470.AuditLog.sln                    # Solution file
```

---

## 🧪 Running Tests

### Run All Tests

```bash
dotnet test E470.AuditLog.sln
```

### Run Architecture Tests

```bash
dotnet test tests/E470.AuditLog.ArchitectureTests/E470.AuditLog.ArchitectureTests.csproj
```

---

## 📖 Learn More

I'm open to hearing your feedback about the E470.AuditLog template and what you'd like to see in future iterations.

If you're ready to learn more, check out [**Pragmatic Clean Architecture**](https://www.milanjovanovic.tech/pragmatic-clean-architecture?utm_source=ca-template):

- Domain-Driven Design
- Role-based authorization
- Permission-based authorization
- Distributed caching with Redis
- OpenTelemetry
- Outbox pattern
- API Versioning
- Unit testing
- Functional testing
- Integration testing

---

## 🤝 Contributing

Contributions are welcome! Please refer to the migration documentation to understand the current state and conventions used in E470.AuditLog.

---

## 📄 License

This project maintains the original license terms. Please see the LICENSE file for details.

---

Stay awesome!