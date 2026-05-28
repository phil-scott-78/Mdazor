namespace Mdazor;

internal static class AttributeConverter
{
    public static object? Convert(string value, Type targetType)
    {
        if (targetType == typeof(string)) return value;
        if (targetType == typeof(int) && int.TryParse(value, out var intValue)) return intValue;
        if (targetType == typeof(bool) && bool.TryParse(value, out var boolValue)) return boolValue;
        if (targetType == typeof(double) && double.TryParse(value, out var doubleValue)) return doubleValue;

        return value;
    }
}
