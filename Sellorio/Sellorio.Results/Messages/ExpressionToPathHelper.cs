using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Sellorio.Results.Messages;

internal static class ExpressionToPathHelper
{
    internal static ImmutableArray<ResultMessagePathItem> GetPath<TFrom, TTo>(Expression<Func<TFrom, TTo>> expression)
    {
        var current = expression.Body;
        var result = new List<ResultMessagePathItem>();

        while (current != null)
        {
            switch (current.NodeType)
            {
                case ExpressionType.Constant:
                case ExpressionType.Parameter:
                    current = null;
                    break;
                case ExpressionType.MemberAccess:
                    var memberExpression = (MemberExpression)current;

                    if (memberExpression.Member is FieldInfo)
                    {
                        current = null;
                        break;
                    }
                    else if (memberExpression.Member is PropertyInfo)
                    {
                        result.Add(new ResultMessagePathItem(ResultMessagePathItemType.Property, memberExpression.Member.Name));
                        current = memberExpression.Expression;
                    }
                    else
                    {
                        throw new InvalidOperationException("Unexpected member access member.");
                    }

                    break;
                case ExpressionType.ArrayIndex:
                    var indexExpression = (IndexExpression)current;
                    result.Add(new ResultMessagePathItem(ResultMessagePathItemType.Indexer, (int)((ConstantExpression)indexExpression.Arguments[0]).Value!));
                    current = indexExpression.Object;
                    break;
                case ExpressionType.Unbox:
                    current = ((UnaryExpression)current).Operand;
                    break;
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    current = ((TypeBinaryExpression)current).Expression;
                    break;
                default:
                    throw new InvalidOperationException("Unexpected expression type.");
            }
        }

        return [.. Enumerable.Reverse(result)];
    }
}
