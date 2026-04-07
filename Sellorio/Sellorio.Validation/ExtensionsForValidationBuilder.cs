using Sellorio.Results.Messages;
using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Sellorio.Validation;

public static class ExtensionsForValidationBuilder
{
    public static IValidationBuilder<TObject> Attributes<TObject>(
        this IValidationBuilder<TObject> validate,
        ResultMessageSeverity severity = ResultMessageSeverity.Critical)
    {
        if (validate.Target == null)
        {
            return validate;
        }

        var properties = typeof(TObject).GetProperties();

        foreach (var property in properties)
        {
            object? value = null;
            bool? hasValue = null;

            var attributes = property.GetCustomAttributes(true);
            var requiredAttribute = attributes.OfType<RequiredAttribute>().FirstOrDefault();

            if (requiredAttribute != null)
            {
                EnsureValueAndHasValue(ref hasValue, ref value, () => property.GetValue(validate.Target));

                if (hasValue == false)
                {
                    AddMessageForProperty(validate, property, "Is required.", severity);
                }
            }

            var stringLengthAttribute = attributes.OfType<StringLengthAttribute>().FirstOrDefault();

            if (stringLengthAttribute != null)
            {
                EnsureValueAndHasValue(ref hasValue, ref value, () => property.GetValue(validate.Target));

                if (hasValue == true && value is string s)
                {
                    if (stringLengthAttribute.MinimumLength == stringLengthAttribute.MaximumLength && s.Length != stringLengthAttribute.MaximumLength)
                    {
                        AddMessageForProperty(validate, property, $"Must be exactly {stringLengthAttribute.MaximumLength} characters in length.", severity);
                    }
                    else if (s.Length > stringLengthAttribute.MaximumLength)
                    {
                        AddMessageForProperty(validate, property, $"Cannot be longer than {stringLengthAttribute.MaximumLength} characters.", severity);
                    }
                    else if (s.Length < stringLengthAttribute.MinimumLength)
                    {
                        AddMessageForProperty(validate, property, $"Cannot be shorter than {stringLengthAttribute.MaximumLength} characters.", severity);
                    }
                }
            }

            var maxLengthAttribute = attributes.OfType<MaxLengthAttribute>().FirstOrDefault();

            if (maxLengthAttribute != null)
            {
                EnsureValueAndHasValue(ref hasValue, ref value, () => property.GetValue(validate.Target));

                if (hasValue == true)
                {
                    if (value is string s)
                    {
                        if (s.Length > maxLengthAttribute.Length)
                        {
                            AddMessageForProperty(validate, property, $"Cannot be longer than {maxLengthAttribute.Length} characters.", severity);
                        }
                    }
                    else
                    {
                        int? count = null;

                        if (count == null && validate is ICollection collection)
                        {
                            count = collection.Count;
                        }

                        if (count == null && value is IEnumerable enumerable)
                        {
                            count = 0;

                            foreach (var item in enumerable)
                            {
                                count++;
                            }
                        }

                        if (count > maxLengthAttribute.Length)
                        {
                            AddMessageForProperty(validate, property, $"Must not contain more than {maxLengthAttribute.Length} items.", severity);
                        }
                    }
                }
            }
        }

        return validate;
    }

    public static IValidationBuilder<TObject> MaxLength<TObject, TPathValue>(
        this IValidationBuilder<TObject> validationBuilder,
        Expression<Func<TObject, TPathValue>> path,
        int maxLength,
        ResultMessageSeverity severity = ResultMessageSeverity.Critical)
    {
        var valueGetter = path.Compile();
        var value = valueGetter.Invoke(validationBuilder.Target);

        if (value != null)
        {
            if (value is string str)
            {
                if (str.Length > maxLength)
                {
                    validationBuilder.AddMessage(path, $"Must be less than {maxLength} in length.", severity);
                }
            }
            else if (value is ICollection collection)
            {
                if (collection.Count > maxLength)
                {
                    validationBuilder.AddMessage(path, $"Must be less than {maxLength} in length.", severity);
                }
            }
            else if (value is IEnumerable enumerable)
            {
                if (enumerable.Cast<object>().Count() > maxLength)
                {
                    validationBuilder.AddMessage(path, $"Must be less than {maxLength} in length.", severity);
                }
            }
            else
            {
                throw new InvalidOperationException("Cannot validate length on a non-IEnumerable.");
            }
        }

        return validationBuilder;
    }

    private static void EnsureValueAndHasValue(ref bool? hasValue, ref object? value, Func<object?> getter)
    {
        if (hasValue == null)
        {
            value = getter.Invoke();
            hasValue = value is not null and not "";
        }
    }

    private static void AddMessageForProperty<TObject>(IValidationBuilder<TObject> validate, PropertyInfo property, string message, ResultMessageSeverity severity)
    {
        var parameter = Expression.Parameter(typeof(TObject));

        var propertyExpression =
            Expression.Lambda(
                typeof(Func<,>).MakeGenericType(typeof(TObject), property.PropertyType),
                Expression.Property(parameter, property),
                false,
                parameter);

        try
        {
            validate.GetType().GetMethods()
                .Single(x => x.Name == nameof(validate.AddMessage) && x.GetParameters().Length == 3)
                .MakeGenericMethod(property.PropertyType)
                .Invoke(validate, [propertyExpression, message, severity]);
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException!;
        }
    }
}
