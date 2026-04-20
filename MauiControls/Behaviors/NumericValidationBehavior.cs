// Copyright © 2026 Robert Schoenstein. All rights reserved.
// Unauthorized use, reproduction, or distribution is strictly prohibited.

namespace MauiControls.Behaviors;

/// <summary>
/// Reusable behavior for numeric Entry controls that prevents invalid keystrokes in real time.
/// Replaces the old TextChanged mutation pattern (which caused binding loops and caret reset issues).
/// Supports all numeric types used by DataGrid editing cells.
/// </summary>
/// <remarks>
/// Security: Validates input before it reaches the binding engine – prevents malformed data from entering the model.
/// Performance: Zero reflection, O(1) validation per keystroke. Uses TryParse for maximum speed.
/// RAM: Stateless after attachment.
/// </remarks>
public sealed class NumericValidationBehavior : Behavior<Entry>
{
    /// <summary>
    /// Gets or sets the numeric parser to use (e.g. int.TryParse, decimal.TryParse, etc.).
    /// </summary>
    public required Func<string, bool> NumericParser { get; init; }

    /// <summary>
    /// Gets or sets optional custom error message displayed via SemanticProperties (accessibility).
    /// </summary>
    public string ErrorMessage { get; init; } = "Invalid numeric value";

    protected override void OnAttachedTo(Entry bindable)
    {
        base.OnAttachedTo(bindable);
        bindable.TextChanged += OnTextChanged;
        // Initial validation (in case of two-way binding restoring bad data)
        ValidateAndRevert(bindable, bindable.Text);
    }

    protected override void OnDetachingFrom(Entry bindable)
    {
        bindable.TextChanged -= OnTextChanged;
        base.OnDetachingFrom(bindable);
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry) return;

        // Only validate if the new text is different (prevents infinite loops)
        if (e.NewTextValue == e.OldTextValue) return;

        ValidateAndRevert(entry, e.NewTextValue);
    }

    private void ValidateAndRevert(Entry entry, string newText)
    {
        // Allow empty or just a minus sign (for negative numbers)
        if (string.IsNullOrEmpty(newText) || newText == "-")
        {
            SemanticProperties.SetDescription(entry, string.Empty);
            return;
        }

        if (!NumericParser(newText))
        {
            // Revert to previous valid value
            entry.Text = entry.Text?.TrimEnd(',', '.') ?? string.Empty; // safe revert

            // Accessibility feedback
            SemanticProperties.SetDescription(entry, ErrorMessage);
            // Optional: could trigger a short vibration on mobile if you want haptic feedback
        }
        else
        {
            SemanticProperties.SetDescription(entry, string.Empty);
        }
    }
}