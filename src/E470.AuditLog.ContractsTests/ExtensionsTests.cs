using E470.AuditLog.Contracts.Models;

namespace E470.AuditLog.Contracts.Tests
{
    [TestClass()]
    public class ExtensionsTests
    {
        [TestMethod()]
        public void TraceObjectPropertiesTest()
        {
            AuditLogEntry auditLogEntry = new AuditLogEntry 
            { 
                Action = "TestAction",
                UserId = "TestUser",
                Details = "TestDetails",
                Timestamp = DateTime.UtcNow,
                IpAddress = "1.1.1.1",
                Resource = "TestResource",
                IsProcessed = false,
                ProcessedAt = null
            };
            Extensions.TraceObjectProperties(auditLogEntry);

        }
    }
}