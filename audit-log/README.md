# E470.AuditLog - Clean Architecture Template

> **🚀 Project Rebranding in Progress**
> 
> This project has been rebranded to **E470.AuditLog**. We are currently in the process of migrating the codebase to reflect this new naming.
> 
> 📖 **Important Documentation:**
> - [Quick Start Guide](QUICK_START_E470.md) - Get started with E470.AuditLog
> - [Migration Plan](MIGRATION_PLAN_E470.md) - Detailed migration roadmap and status
> - [PR1 Changes Summary](PR1_CHANGES_SUMMARY.md) - Summary of changes in the first pull request
> 
> Please refer to these documents for the latest information about the migration process and how to work with the E470.AuditLog codebase.

---

## What's included in the template?

- **SharedKernel** project with common Domain-Driven Design abstractions.
- **Domain** layer with sample entities for E470.AuditLog.
- **Application** layer with abstractions for:
  - CQRS
  - Example use cases
  - Cross-cutting concerns (logging, validation)
- **Infrastructure** layer with:
  - Authentication
  - Permission authorization
  - EF Core, PostgreSQL
  - Serilog
- **Seq** for searching and analyzing structured logs
  - Seq is available at http://localhost:8081 by default
- **Testing** projects
  - Architecture testing

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

Stay awesome!