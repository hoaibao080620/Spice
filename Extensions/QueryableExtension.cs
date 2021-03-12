using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Text;

namespace Spice.Extensions {
    public static class QueryableExtension {
        public static IQueryable<T> ApplySort<T>(this IQueryable<T> source, string parameter) {
            if (!source.Any() || string.IsNullOrEmpty(parameter) || string.IsNullOrWhiteSpace(parameter)) {
                return source;
            }

            parameter = parameter.Trim();
            var orderParameter = parameter.Split(",", StringSplitOptions.RemoveEmptyEntries);
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var orderString = new StringBuilder();
            foreach (var order in orderParameter) {
                if (string.IsNullOrWhiteSpace(order)) {
                    continue;
                }

                var propertyOrder = order.Trim().Split(" ")[0];

                var property = properties.FirstOrDefault(p => p.Name.ToLower() == propertyOrder.ToLower());
                if (property != null) {
                    var orderDirection = order.EndsWith("desc") ? "descending" : "ascending";
                    orderString.Append($"{property.Name} {orderDirection},");
                }
            }

            var finalOrder = orderString.ToString().TrimEnd(',',' ');
            return string.IsNullOrEmpty(finalOrder) ? source : source.OrderBy(finalOrder);
        }
    }
}