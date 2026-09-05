namespace Panyebar.Application.Suministros;

public static class NisFormatter
{
    private const long MaximumValue = 999999;

    public static string Format(long value)
    {
        if (value is < 1 or > MaximumValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "El correlativo NIS debe estar entre 1 y 999999.");
        }

        return $"PAN-{value:D6}";
    }
}