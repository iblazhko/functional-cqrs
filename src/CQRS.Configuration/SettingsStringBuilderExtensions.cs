using System.Globalization;
using System.Linq.Expressions;
using System.Text;

namespace CQRS.Configuration;

public static class SettingsStringBuilderExtensions
{
    private const string Indent = "  ";
    private const string DefaultValue = "<NOT SET>";

    public static StringBuilder AppendSettingsTitle(this StringBuilder stringBuilder, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name must be specified", nameof(name));

        stringBuilder.AppendLine($"{name} settings".ToUpperInvariant());
        stringBuilder.AppendLine("=====");

        return stringBuilder;
    }

    public static StringBuilder AppendSettingsSection<TResult>(
        this StringBuilder stringBuilder,
        Expression<Func<TResult>> propertyExpression
    ) => AppendPropertyValue(stringBuilder, propertyExpression, false);

    public static StringBuilder AppendSettingValue<TResult>(
        this StringBuilder stringBuilder,
        Expression<Func<TResult>> propertyExpression
    ) => AppendPropertyValue(stringBuilder, propertyExpression, true);

    private static StringBuilder AppendPropertyValue<TResult>(
        this StringBuilder stringBuilder,
        Expression<Func<TResult>> propertyExpression,
        bool isPrimitiveProperty
    )
    {
        var memberExpression = propertyExpression?.Body is MemberExpression m
            ? m
            : throw new ArgumentException(
                "Expression should use settings property, e.g. '() => ServiceUrl'",
                nameof(propertyExpression)
            );
        var memberName = memberExpression.Member.Name;
        var value = propertyExpression.Compile()();
        var formattedValue = value?.ToString() ?? DefaultValue;

        if (isPrimitiveProperty)
        {
            stringBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $"{Indent}{memberName}: {formattedValue}"
            );
        }
        else
        {
            stringBuilder.AppendSettingSectionTitle(memberName);
            foreach (var line in formattedValue.Split(Environment.NewLine))
            {
                stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"{Indent}{Indent}{line}");
            }
        }

        return stringBuilder;
    }

    private static void AppendSettingSectionTitle(this StringBuilder stringBuilder, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name must be specified", nameof(name));

        stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"{Indent}{name}");
        stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"{Indent}---");
    }
}
