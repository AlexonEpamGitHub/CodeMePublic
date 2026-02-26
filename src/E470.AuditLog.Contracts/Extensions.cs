using System.Diagnostics;

namespace E470.AuditLog.Contracts
{
    public static class Extensions
    {
        public static void TraceObjectProperties<T>(this T request) where T : class
        {
            var properties = typeof(T).GetProperties()
                .Where(p => p.CanRead)
                .ToDictionary(p => p.Name, p => p.GetValue(request)?.ToString() ?? string.Empty);

            foreach (var property in properties)
            {
                Activity.Current?.SetTag(property.Key, property.Value);
            }
        }
    }
}
