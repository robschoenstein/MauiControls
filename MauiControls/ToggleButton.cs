// Copyright © 2026 Robert Schoenstein. All rights reserved.
// Unauthorized use, reproduction, or distribution is strictly prohibited.

using System.Windows.Input;
using MauiControls.Extensions;

namespace MauiControls;

/// <summary>
/// Enterprise-grade ToggleButton control implemented entirely in C# (no XAML).
/// Supports two-state toggling with full styling and command support.
/// </summary>
public class ToggleButton : Border
{
    private readonly Label _label;

    #region Bindable Properties using BindablePropertyExtension

    public static readonly BindableProperty IsToggledProperty =
        BindablePropertyExtension.Create<ToggleButton, bool>(
            defaultValue: false,
            defaultBindingMode: BindingMode.TwoWay,
            propertyChanged: (b, _, n) => ((ToggleButton)b).OnIsToggledChanged((bool)n));

    public static readonly BindableProperty OnTextProperty =
        BindablePropertyExtension.Create<ToggleButton, string>(defaultValue: "ON");

    public static readonly BindableProperty OffTextProperty =
        BindablePropertyExtension.Create<ToggleButton, string>(defaultValue: "OFF");

    public static readonly BindableProperty OnBackgroundColorProperty =
        BindablePropertyExtension.Create<ToggleButton, Color>(defaultValue: Colors.Green);

    public static readonly BindableProperty OffBackgroundColorProperty =
        BindablePropertyExtension.Create<ToggleButton, Color>(defaultValue: Colors.Gray);

    public static readonly BindableProperty OnTextColorProperty =
        BindablePropertyExtension.Create<ToggleButton, Color>(defaultValue: Colors.White);

    public static readonly BindableProperty OffTextColorProperty =
        BindablePropertyExtension.Create<ToggleButton, Color>(defaultValue: Colors.Black);

    public static readonly BindableProperty CommandProperty =
        BindablePropertyExtension.Create<ToggleButton, ICommand>(defaultValue: null);

    public static readonly BindableProperty CommandParameterProperty =
        BindablePropertyExtension.Create<ToggleButton, object>(defaultValue: null);

    #endregion

    #region Public Properties

    public bool IsToggled
    {
        get => (bool)GetValue(IsToggledProperty);
        set => SetValue(IsToggledProperty, value);
    }

    public string OnText
    {
        get => (string)GetValue(OnTextProperty);
        set => SetValue(OnTextProperty, value);
    }

    public string OffText
    {
        get => (string)GetValue(OffTextProperty);
        set => SetValue(OffTextProperty, value);
    }

    public Color OnBackgroundColor
    {
        get => (Color)GetValue(OnBackgroundColorProperty);
        set => SetValue(OnBackgroundColorProperty, value);
    }

    public Color OffBackgroundColor
    {
        get => (Color)GetValue(OffBackgroundColorProperty);
        set => SetValue(OffBackgroundColorProperty, value);
    }

    public Color OnTextColor
    {
        get => (Color)GetValue(OnTextColorProperty);
        set => SetValue(OnTextColorProperty, value);
    }

    public Color OffTextColor
    {
        get => (Color)GetValue(OffTextColorProperty);
        set => SetValue(OffTextColorProperty, value);
    }

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    #endregion

    public ToggleButton()
    {
        _label = new Label
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            FontAttributes = FontAttributes.Bold
        };

        Content = _label;
        Stroke = Colors.Gray;
        StrokeThickness = 1;
        Padding = new Thickness(12, 8);
        BackgroundColor = OffBackgroundColor; // initial state

        // Tap gesture to toggle
        var tap = new TapGestureRecognizer();
        tap.Tapped += OnTapped;
        GestureRecognizers.Add(tap);

        UpdateVisualState();
    }

    private void OnTapped(object sender, EventArgs e)
    {
        IsToggled = !IsToggled;
    }

    private void OnIsToggledChanged(bool newValue)
    {
        UpdateVisualState();

        // Execute command if set
        if (Command?.CanExecute(CommandParameter) == true)
        {
            Command.Execute(CommandParameter);
        }
    }

    private void UpdateVisualState()
    {
        if (IsToggled)
        {
            BackgroundColor = OnBackgroundColor;
            _label.Text = OnText;
            _label.TextColor = OnTextColor;
        }
        else
        {
            BackgroundColor = OffBackgroundColor;
            _label.Text = OffText;
            _label.TextColor = OffTextColor;
        }
    }
}