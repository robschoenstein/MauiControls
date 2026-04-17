// Copyright © 2026 Robert Schoenstein. All rights reserved.
// Unauthorized use, reproduction, or distribution is strictly prohibited.

namespace MauiControls.Extensions;

internal static class LayoutOptionsExtension
{
    internal static TextAlignment ToTextAlignment(this LayoutOptions layoutAlignment) => layoutAlignment.Alignment switch
    {
        LayoutAlignment.Start => TextAlignment.Start,
        LayoutAlignment.End => TextAlignment.End,
        _ => TextAlignment.Center,
    };
}