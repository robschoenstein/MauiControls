// Copyright © 2026 Robert Schoenstein. All rights reserved.
// Unauthorized use, reproduction, or distribution is strictly prohibited.

using System.Globalization;

namespace MauiControls.Converters;

public class BorderThicknessToCellPaddingConverter : IValueConverter
{
    public object Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
    {
        if (value is Thickness thickness)
        {
            return new Thickness(thickness.Left / 2, thickness.Top / 2, thickness.Right / 2, thickness.Bottom / 2);
        }

        return new Thickness(0);
    }

    public object ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
    {
        if (value is Thickness thickness)
        {
            return new Thickness(thickness.Left * 2, thickness.Top * 2, thickness.Right * 2, thickness.Bottom * 2);
        }

        return new Thickness(0);
    }
}