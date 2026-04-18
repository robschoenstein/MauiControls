// Copyright © 2026 Robert Schoenstein. All rights reserved.
// Unauthorized use, reproduction, or distribution is strictly prohibited.

using System.Windows.Input;
using MauiControls.Extensions;

namespace MauiControls;

/// <summary>
/// ToggleButton control.
/// - Defaults to Label control for CheckedText/UncheckedText.
/// - Fully overrideable with any control inheriting from <see cref="View"/> via CheckedContent/UncheckedContent.
/// </summary>
public class ToggleButton : ContentView
{
    #region Bindable Properties

    public static readonly BindableProperty CheckedProperty =
        BindablePropertyExtension.Create<ToggleButton, bool>(
            defaultValue: false,
            defaultBindingMode: BindingMode.TwoWay,
            propertyChanged: OnCheckedChanged);

    public static readonly BindableProperty CheckedTextProperty =
        BindablePropertyExtension.Create<ToggleButton, string>(
            defaultValue: "On",
            propertyChanged: OnTextChanged);

    public static readonly BindableProperty UncheckedTextProperty =
        BindablePropertyExtension.Create<ToggleButton, string>(
            defaultValue: "Off",
            propertyChanged: OnTextChanged);
    
    public static readonly BindableProperty CheckedContentProperty =
        BindablePropertyExtension.Create<ToggleButton, View>(
            defaultValue: null,
            propertyChanged: OnContentChanged);

    public static readonly BindableProperty UncheckedContentProperty =
        BindablePropertyExtension.Create<ToggleButton, View>(
            defaultValue: null,
            propertyChanged: OnContentChanged);

    public static readonly BindableProperty OnBackgroundColorProperty =
        BindablePropertyExtension.Create<ToggleButton, Color>(defaultValue: Colors.Green);

    public static readonly BindableProperty OffBackgroundColorProperty =
        BindablePropertyExtension.Create<ToggleButton, Color>(defaultValue: Colors.Gray);

    public static readonly BindableProperty CheckedTextColorProperty =
        BindablePropertyExtension.Create<ToggleButton, Color>(defaultValue: Colors.White);

    public static readonly BindableProperty UncheckedTextColorProperty =
        BindablePropertyExtension.Create<ToggleButton, Color>(defaultValue: Colors.Black);
    
    public static readonly BindableProperty CornerRadiusProperty =
        BindablePropertyExtension.Create<ToggleButton, CornerRadius>(
            defaultValue: new CornerRadius(8),
            propertyChanged: OnCornerRadiusChanged);
    
    public static readonly BindableProperty CommandProperty =
        BindablePropertyExtension.Create<ToggleButton, ICommand>(defaultValue: null);

    public static readonly BindableProperty CommandParameterProperty =
        BindablePropertyExtension.Create<ToggleButton, object>(defaultValue: null);

    #endregion

    #region Public Properties

    public bool Checked
    {
        get => (bool)GetValue(CheckedProperty);
        set => SetValue(CheckedProperty, value);
    }

    public string CheckedText
    {
        get => (string)GetValue(CheckedTextProperty);
        set => SetValue(CheckedTextProperty, value);
    }

    public string UncheckedText
    {
        get => (string)GetValue(UncheckedTextProperty);
        set => SetValue(UncheckedTextProperty, value);
    }
    
    public View CheckedContent
    {
        get => (View)GetValue(CheckedContentProperty);
        set => SetValue(CheckedContentProperty, value);
    }

    public View UncheckedContent
    {
        get => (View)GetValue(UncheckedContentProperty);
        set => SetValue(UncheckedContentProperty, value);
    }

    public Color CheckedBackgroundColor
    {
        get => (Color)GetValue(OnBackgroundColorProperty);
        set => SetValue(OnBackgroundColorProperty, value);
    }

    public Color UncheckedBackgroundColor
    {
        get => (Color)GetValue(OffBackgroundColorProperty);
        set => SetValue(OffBackgroundColorProperty, value);
    }

    public Color CheckedTextColor
    {
        get => (Color)GetValue(CheckedTextColorProperty);
        set => SetValue(CheckedTextColorProperty, value);
    }

    public Color UncheckedTextColor
    {
        get => (Color)GetValue(UncheckedTextColorProperty);
        set => SetValue(UncheckedTextColorProperty, value);
    }
    
    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
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
        // Tap gesture to toggle
        var tap = new TapGestureRecognizer();
        tap.Tapped += OnTapped;
        GestureRecognizers.Add(tap);

        UpdateVisualState();
    }

    private void OnTapped(object sender, EventArgs e)
    {
        Checked = !Checked;
    }

    private static void OnCheckedChanged(BindableObject obj, bool oldValue, bool newValue)
    {
        var control = (ToggleButton)obj;
        control.UpdateVisualState();
        control.Command?.Execute(control.CommandParameter);
    }
    
    private static void OnTextChanged(BindableObject obj, string oldValue, string newValue)
    {
        var control = (ToggleButton)obj;
        control.UpdateVisualState();
    }

    private static void OnContentChanged(BindableObject obj, View? oldValue, View? newValue)
    {
        var control = (ToggleButton)obj;
        control.UpdateVisualState();
    }

    private static void OnCornerRadiusChanged(BindableObject obj, CornerRadius oldValue, CornerRadius newValue)
    {
        var control = (ToggleButton)obj;
        control.UpdateVisualState();
    }
    
    private void UpdateVisualState()
    {
        if (Checked)
        {
            BackgroundColor = CheckedBackgroundColor;

            Content = CheckedContent ?? new Label 
            { 
                Text = CheckedText, 
                TextColor = CheckedTextColor,
                VerticalOptions = LayoutOptions.Center 
            };
        }
        else
        {
            BackgroundColor = UncheckedBackgroundColor;
            
            Content = UncheckedContent ?? new Label 
            { 
                Text = UncheckedText, 
                TextColor = UncheckedTextColor,
                VerticalOptions = LayoutOptions.Center 
            };
        }
    }
}