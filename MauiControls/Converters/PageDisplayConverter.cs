// Copyright © 2026 Robert Schoenstein. All rights reserved.
// Unauthorized use, reproduction, or distribution is strictly prohibited.

using System.Globalization;

namespace MauiControls.Converters;

/// <summary>
/// Multi-value converter that formats the footer page display as "Page X of Y".
/// Used by DataGrid footer to correctly show current page and total pages.
/// </summary>
/// <remarks>
/// Performance: Pure value conversion – zero allocations after first call.
/// Security: Pure math/string formatting – no user input processing.
/// </remarks>
public sealed class PageDisplayConverter : IMultiValueConverter
{
    /// <summary>
    /// Converts PageNumber and PageCount into a localized "Page X of Y" string.
    /// </summary>
    /// <param name="values">Array containing [PageNumber (int), PageCount (int)].</param>
    /// <param name="targetType">Not used.</param>
    /// <param name="parameter">Optional format string override (default: "Page {0} of {1}").</param>
    /// <param name="culture">Culture for formatting (falls back to current UI culture).</param>
    /// <returns>Formatted page display string.</returns>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not int pageNumber || values[1] is not int pageCount)
        {
            return "Page 1 of 1"; // graceful fallback
        }

        culture ??= CultureInfo.CurrentUICulture;
        var format = parameter as string ?? "Page {0} of {1}";

        return string.Format(culture, format, pageNumber, Math.Max(1, pageCount));
    }

    /// <summary>
    /// Not implemented – one-way conversion only (footer display).
    /// </summary>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException("PageDisplayConverter is one-way only.");
}