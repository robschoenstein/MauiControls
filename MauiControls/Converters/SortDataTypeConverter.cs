// Copyright © 2026 Robert Schoenstein. All rights reserved.
// Unauthorized use, reproduction, or distribution is strictly prohibited.

using System.ComponentModel;
using System.Globalization;
using MauiControls.DataSorting;

namespace MauiControls.Converters;

/// <summary>
/// Converts string to <see cref="SortData"/>.
/// </summary>
public sealed class SortDataTypeConverter : TypeConverter // This needs to be public or it will produce a MethodAccessException
{
    /// <inheritdoc/>
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value == null)
        {
            return null;
        }

        if (value is int index || int.TryParse(value.ToString(), out index))
        {
            return (SortData)index;
        }

        return base.ConvertFrom(context, culture, value);
    }
}