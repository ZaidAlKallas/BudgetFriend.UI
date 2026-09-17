using BudgetFriend.App.Models;
using System.Globalization;

namespace BudgetFriend.App.Application.Services;

/// <summary>
/// Centralized financial value formatting. Currency and digit conventions
/// follow the selected language (Arabic uses Arabic-Indic digits), so all
/// money values are rendered consistently across the application.
/// </summary>
public sealed class FinanceFormatter {
    private readonly LocalizationService _localization;

    public FinanceFormatter(LocalizationService localization) {
        _localization = localization;
    }

    public string Currency(decimal amount, Currency CurrencyCode) {
        var text = amount.ToString("N2", Culture);
        return $"{text} {CurrencyCode}";
    }

    public string Currency(decimal amount, string CurrencyCode) {
        var text = amount.ToString("N2", Culture);
        return $"{text} {CurrencyCode}";
    }

    public string Currency(decimal amount) => Currency(amount, string.Empty).Trim();

    public string Amount(decimal amount) {
        var sign = amount < 0 ? "-" : "";
        return $"{sign}{Math.Abs(amount).ToString("N2", Culture)}";
    }

    public string Number(decimal value, int decimals = 0)
        => value.ToString($"N{decimals}", Culture);

    public string ShortDate(DateTimeOffset value)
            => value.UtcDateTime.ToString("d", Culture);

    public string ShortDateTime(DateTimeOffset value) {
        var utc = value.UtcDateTime;
        return utc.TimeOfDay == TimeSpan.Zero
            ? utc.ToString("d", Culture)
            : utc.ToString("g", Culture);
    }

    public string MonthYear(DateTimeOffset value)
        => value.LocalDateTime.ToString("MMM yyyy", Culture);

    public string MediumDate(DateTimeOffset value)
        => value.LocalDateTime.ToString("g", Culture);

    private CultureInfo Culture => _localization.Culture;
}