// Copyright © 2026 Robert Schoenstein. All rights reserved.
// Unauthorized use, reproduction, or distribution is strictly prohibited.

using System.Windows.Input;
using MauiControls.Extensions;
using Microsoft.Maui.Controls.Shapes;

namespace MauiControls;

/// <summary>
/// ToggleButton control.
/// - Defaults to Label control for CheckedText/UncheckedText.
/// - Fully overrideable with any control inheriting from <see cref="View"/> via CheckedContent/UncheckedContent.
/// </summary>
public class ToggleButton : ContentView
{
    private readonly Border _border = new();
    
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

    public static readonly BindableProperty CheckedBackgroundColorProperty =
        BindablePropertyExtension.Create<ToggleButton, Color>(
            defaultValue: Colors.Green);

    public static readonly BindableProperty UncheckedBackgroundColorProperty =
        BindablePropertyExtension.Create<ToggleButton, Color>(
            defaultValue: Colors.Gray);

    public static readonly BindableProperty CheckedTextColorProperty =
        BindablePropertyExtension.Create<ToggleButton, Color>(
            defaultValue: Colors.White);

    public static readonly BindableProperty UncheckedTextColorProperty =
        BindablePropertyExtension.Create<ToggleButton, Color>(
            defaultValue: Colors.Black);
    
    public static readonly BindableProperty CornerRadiusProperty =
        BindablePropertyExtension.Create<ToggleButton, CornerRadius>(
            defaultValue: new CornerRadius(8),
            propertyChanged: OnCornerRadiusChanged);
    
    public static readonly BindableProperty BorderColorProperty =
        BindablePropertyExtension.Create<ToggleButton, Color>(
            defaultValue: Colors.Transparent);

    public static readonly BindableProperty BorderThicknessProperty =
        BindablePropertyExtension.Create<ToggleButton, Thickness>(
            defaultValue: Thickness.Zero);

    public new static readonly BindableProperty PaddingProperty =
        BindablePropertyExtension.Create<ToggleButton, Thickness>(
            defaultValue: new Thickness(12, 8));

    public static readonly BindableProperty FontSizeProperty =
        BindablePropertyExtension.Create<ToggleButton, double>(
            defaultValue: 14.0);
    
    public static readonly BindableProperty CommandProperty =
        BindablePropertyExtension.Create<ToggleButton, ICommand>();

    public static readonly BindableProperty CommandParameterProperty =
        BindablePropertyExtension.Create<ToggleButton, object>();

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
        get => (Color)GetValue(CheckedBackgroundColorProperty);
        set => SetValue(CheckedBackgroundColorProperty, value);
    }

    public Color UncheckedBackgroundColor
    {
        get => (Color)GetValue(UncheckedBackgroundColorProperty);
        set => SetValue(UncheckedBackgroundColorProperty, value);
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

    public Color BorderColor
    {
        get => (Color)GetValue(BorderColorProperty); 
        set => SetValue(BorderColorProperty, value);
    }

    public Thickness BorderThickness
    {
        get => (Thickness)GetValue(BorderThicknessProperty); 
        set => SetValue(BorderThicknessProperty, value);
    }
    
    public new Thickness Padding
    {
        get => (Thickness)GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
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
        Content = _border;
        
        // Tap gesture to toggle
        var tap = new TapGestureRecognizer();
        tap.Tapped += OnTapped;
        GestureRecognizers.Add(tap);

        UpdateVisualState();
    }

    private void OnTapped(object? sender, EventArgs e)
    {
        if (!IsEnabled)
        {
            return;
        }

        // Toggle the state
        Checked = !Checked;

        // Execute command if present (respect CanExecute)
        Command?.Execute(CommandParameter);
    }

    private static void OnCheckedChanged(BindableObject obj, bool oldValue, bool newValue)
    {
        OnVisualPropertyChanged(obj, oldValue, newValue);
    }
    
    private static void OnTextChanged(BindableObject obj, string oldValue, string newValue)
    {
        OnVisualPropertyChanged(obj, oldValue, newValue);
    }

    private static void OnContentChanged(BindableObject obj, View? oldValue, View? newValue)
    {
        OnVisualPropertyChanged(obj, oldValue, newValue);
    }
    
    private static void OnCornerRadiusChanged(BindableObject obj, CornerRadius oldValue, CornerRadius newValue)
    {
        OnVisualPropertyChanged(obj, oldValue, newValue);
    }
    
    private static void OnVisualPropertyChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        if (bindable is ToggleButton control)
        {
            control.UpdateVisualState();
        }
    }
    
    /// <summary>
    /// Updates the visual appearance based on Checked / IsEnabled state.
    /// Called whenever Checked, content, colors, or styling properties change.
    /// </summary>
    private void UpdateVisualState()
    {
        // Choose background color based on Checked state
        _border.BackgroundColor = Checked ? CheckedBackgroundColor : UncheckedBackgroundColor;

        // Choose content (custom View or fallback Label)
        View innerContent;
        
        if (Checked && CheckedContent != null)
        {
            innerContent = CheckedContent;
        }
        else if (!Checked && UncheckedContent != null)
        {
            innerContent = UncheckedContent;
        }
        else
        {
            // Fallback Label
            innerContent = new Label
            {
                Text = Checked ? CheckedText : UncheckedText,
                TextColor = Checked ? CheckedTextColor : UncheckedTextColor,
                FontSize = FontSize,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center
            };
        }
        
        _border.Content = innerContent;

        // Apply all styling (corner radius, border, padding, font)
        _border.Padding = Padding;
        _border.Stroke = BorderColor;
        _border.StrokeThickness = BorderThickness.Left; // uniform thickness (most common use case)
        _border.StrokeShape = new RoundRectangle
        {
            CornerRadius = CornerRadius
        };

        // Disabled visual feedback
        Opacity = IsEnabled ? 1.0 : 0.6;
    }
}