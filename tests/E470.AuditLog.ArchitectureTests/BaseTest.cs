using System.Reflection;
using E470.AuditLog.Application.Abstractions.Messaging;
using E470.AuditLog.Domain.Users;
using E470.AuditLog.Infrastructure.Database;
using E470.AuditLog.Web.Api;

namespace E470.AuditLog.ArchitectureTests;

public abstract class BaseTest
{
    protected static readonly Assembly DomainAssembly = typeof(User).Assembly;
    protected static readonly Assembly ApplicationAssembly = typeof(ICommand).Assembly;
    protected static readonly Assembly InfrastructureAssembly = typeof(ApplicationDbContext).Assembly;
    protected static readonly Assembly PresentationAssembly = typeof(Program).Assembly;
}
