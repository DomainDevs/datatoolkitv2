using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DataToolkit.Library.Metadata;

public static class EntityMetadataHelper
{
    private static readonly ConcurrentDictionary<Type, EntityMetadata> _metadataCache = new();

    public static EntityMetadata GetMetadata<T>() where T : class =>
        GetMetadata(typeof(T));

    public static EntityMetadata GetMetadata(Type type)
    {
        return _metadataCache.GetOrAdd(type, t =>
        {
            TableAttribute tableAttr = t.GetCustomAttribute<TableAttribute>();
            string tableName = tableAttr?.Name ?? t.Name;

            List<PropertyInfo> properties = t.GetProperties()
                .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null)
                .ToList();

            List<PropertyInfo> keys = properties
                .Where(p => p.GetCustomAttribute<KeyAttribute>() != null)
                .ToList();

            List<PropertyInfo> identities = properties
                .Where(p => p.GetCustomAttribute<DatabaseGeneratedAttribute>()?.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity)
                .ToList();

            List<PropertyInfo> required = properties
                .Where(p => p.GetCustomAttribute<RequiredAttribute>() != null)
                .ToList();

            Dictionary<PropertyInfo, string> columnMappings = properties.ToDictionary(
                p => p,
                p => p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name
            );

            return new EntityMetadata
            {
                TableName = tableName,
                Properties = properties,
                KeyProperties = keys,
                IdentityProperties = identities,
                RequiredProperties = required,
                ColumnMappings = columnMappings
            };
        });
    }

    public static IEnumerable<string> GetPropertiesFromExpression<T>(Expression<Func<T, object>> expression)
    {
        if (expression.Body is NewExpression newExpr)
        {
            // Ejemplo: c => new { c.Nombre, c.Email }
            return newExpr.Members.Select(m => m.Name);
        }

        if (expression.Body is MemberExpression memberExpr)
        {
            // Ejemplo: c => c.Nombre
            return new[] { memberExpr.Member.Name };
        }

        if (expression.Body is UnaryExpression unary && unary.Operand is MemberExpression innerMember)
        {
            // Ejemplo: c => (object)c.Nombre
            return new[] { innerMember.Member.Name };
        }

        throw new ArgumentException("Expresión no soportada. Usa c => new { c.Prop1, c.Prop2 } o c => c.Prop");
    }

    public static string GetTableName<T>() where T : class =>
        GetMetadata<T>().TableName;

    public static string GetColumnName(PropertyInfo prop, Type entityType) =>
        GetMetadata(entityType).ColumnMappings.TryGetValue(prop, out var name) ? name : prop.Name;

    public static bool IsKey(PropertyInfo prop, Type entityType) =>
        GetMetadata(entityType).KeyProperties.Contains(prop);

    public static bool IsIdentity(PropertyInfo prop, Type entityType) =>
        GetMetadata(entityType).IdentityProperties.Contains(prop);

    public static bool IsRequired(PropertyInfo prop, Type entityType) =>
        GetMetadata(entityType).RequiredProperties.Contains(prop);

    public static bool IsNotMapped(PropertyInfo prop) =>
        prop.GetCustomAttribute<NotMappedAttribute>() != null;
}