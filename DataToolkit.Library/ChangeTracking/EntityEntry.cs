// DataToolkit.Library.ChangeTracking/EntityEntry.cs
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace DataToolkit.Library.ChangeTracking
{
    public class EntityEntry<T> where T : class
    {
        private readonly Dictionary<string, object?> _originalValues = new();
        private readonly HashSet<string> _modifiedProperties = new();

        public EntityEntry(T original)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            foreach (var prop in typeof(T).GetProperties())
                _originalValues[prop.Name] = prop.GetValue(original);
        }

        /// <summary>
        /// Marca manualmente una propiedad como modificada (equivalente a Entry.Property(...).IsModified = true)
        /// </summary>
        public void Property(Expression<Func<T, object>> property)
        {
            var name = GetPropertyName(property);
            _modifiedProperties.Add(name);
        }

        /// <summary>
        /// Compara el original (capturado en el ctor) con la instancia "current" y llena ModifiedProperties.
        /// Devuelve las propiedades detectadas como distintas.
        /// </summary>
        public IReadOnlyCollection<string> DetectChanges(T current)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));

            foreach (var prop in typeof(T).GetProperties())
            {
                var orig = _originalValues.ContainsKey(prop.Name) ? _originalValues[prop.Name] : null;
                var cur = prop.GetValue(current);

                if (!Equals(orig, cur))
                    _modifiedProperties.Add(prop.Name);
            }

            return _modifiedProperties;
        }

        public IReadOnlyCollection<string> ModifiedProperties => _modifiedProperties;

        private static string GetPropertyName(Expression<Func<T, object>> expr)
        {
            if (expr.Body is MemberExpression m) return m.Member.Name;
            if (expr.Body is UnaryExpression u && u.Operand is MemberExpression m2) return m2.Member.Name;
            throw new ArgumentException("La expresión debe ser una propiedad.");
        }
    }
}
