using System.Collections;
using System.Reflection;
using Serilog.Core;
using Serilog.Events;

namespace FinanceReport.Api.Logging;

/// <summary>
/// Masque les propriétés sensibles des objets déstructurés (<c>{@Objet}</c>) : <c>password*</c>, <c>token</c>,
/// <c>*Hash</c> et la clé de signature JWT (RG-20, TS §4.1).
/// </summary>
public sealed class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    private const string Mask = "***";

    public static bool IsSensitive(string propertyName) =>
        propertyName.StartsWith("password", StringComparison.OrdinalIgnoreCase)
        || propertyName.Equals("token", StringComparison.OrdinalIgnoreCase)
        || propertyName.EndsWith("Hash", StringComparison.OrdinalIgnoreCase)
        || propertyName.Equals("jwtSigningKey", StringComparison.OrdinalIgnoreCase);

    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
    {
        result = null!;
        var type = value.GetType();
        if (type.IsPrimitive || type.IsEnum || value is string or IEnumerable)
        {
            return false;
        }

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToArray();
        if (!properties.Any(p => IsSensitive(p.Name)))
        {
            return false;
        }

        result = new StructureValue(
            properties.Select(p => new LogEventProperty(
                p.Name,
                IsSensitive(p.Name) ? new ScalarValue(Mask) : propertyValueFactory.CreatePropertyValue(p.GetValue(value), destructureObjects: true))),
            type.Name);
        return true;
    }
}
