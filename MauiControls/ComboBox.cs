// Copyright © 2026 Robert Schoenstein. All rights reserved.
// Unauthorized use, reproduction, or distribution is strictly prohibited.

using System.Collections;
using MauiControls.Extensions;

namespace MauiControls;

/// <summary>
/// Enterprise-grade ComboBox / AutoComplete control.
/// Built entirely in C# with full styling support and debouncing.
/// </summary>
public class ComboBox : VerticalStackLayout, IDisposable
{
    private readonly Entry _entry;
    private readonly CollectionView _collectionView;
    private readonly Button _clearButton;
    private readonly IDispatcherTimer _debounceTimer;
    private readonly CancellationTokenSource _cts = new();

    private bool _suppressFiltering;
    private bool _suppressSelectionFiltering;

    /// <summary>
    /// Occurs when the selected item changes.
    /// </summary>
    public event EventHandler<SelectionChangedEventArgs>? SelectedItemChanged;
    
    /// <summary>
    /// Occurs when the text in the entry changes (after debounce).
    /// </summary>
    public event EventHandler<TextChangedEventArgs>? TextChanged;

    #region Bindable Properties

    public static readonly BindableProperty ItemsSourceProperty =
        BindablePropertyExtension.Create<ComboBox, IEnumerable>(
            propertyChanged: (b, _, n) => ((ComboBox)b)._collectionView.ItemsSource = (IEnumerable?)n);

    public static readonly BindableProperty SelectedItemProperty =
        BindablePropertyExtension.Create<ComboBox, object?>(
            defaultBindingMode: BindingMode.TwoWay,
            propertyChanged: (b, _, n) => ((ComboBox)b)._collectionView.SelectedItem = n);

    public static readonly BindableProperty TextProperty =
        BindablePropertyExtension.Create<ComboBox, string?>(
            defaultBindingMode: BindingMode.TwoWay,
            propertyChanged: (b, _, n) =>
            {
                var self = (ComboBox)b;
                if (!self._suppressFiltering)
                    self._entry.Text = n;
            });

    public static readonly BindableProperty EntryDisplayPathProperty =
        BindablePropertyExtension.Create<ComboBox, string>(defaultValue: string.Empty);

    public static readonly BindableProperty PlaceholderProperty =
        BindablePropertyExtension.Create<ComboBox, string>(
            defaultValue: string.Empty,
            propertyChanged: (b, _, n) => ((ComboBox)b)._entry.Placeholder = (string)n);

    public static readonly BindableProperty IsDropDownOpenProperty =
        BindablePropertyExtension.Create<ComboBox, bool>(
            defaultValue: false,
            propertyChanged: (b, _, n) => ((ComboBox)b)._collectionView.IsVisible = (bool)n);

    public static readonly BindableProperty DebounceMillisecondsProperty =
        BindablePropertyExtension.Create<ComboBox, int>(defaultValue: 300);

    public static readonly BindableProperty IsReadOnlyProperty =
        BindablePropertyExtension.Create<ComboBox, bool>(
            defaultValue: false,
            propertyChanged: (b, _, n) => ((ComboBox)b)._entry.IsReadOnly = (bool)n);

    // Styling
    public static readonly BindableProperty EntryStyleProperty =
        BindablePropertyExtension.Create<ComboBox, Style?>(
            propertyChanged: (b, _, n) => ((ComboBox)b)._entry.Style = (Style?)n);

    public static readonly BindableProperty CollectionViewStyleProperty =
        BindablePropertyExtension.Create<ComboBox, Style?>(
            propertyChanged: (b, _, n) => ((ComboBox)b)._collectionView.Style = (Style?)n);

    public static readonly BindableProperty ClearButtonStyleProperty =
        BindablePropertyExtension.Create<ComboBox, Style?>(
            propertyChanged: (b, _, n) => ((ComboBox)b)._clearButton.Style = (Style?)n);

    public static readonly BindableProperty DropDownBackgroundColorProperty =
        BindablePropertyExtension.Create<ComboBox, Color>(
            defaultValue: Colors.White,
            propertyChanged: (b, _, n) => ((ComboBox)b)._collectionView.BackgroundColor = (Color)n);

    public static readonly BindableProperty IsClearButtonVisibleProperty =
        BindablePropertyExtension.Create<ComboBox, bool>(
            defaultValue: true,
            propertyChanged: (b, _, n) => ((ComboBox)b)._clearButton.IsVisible = (bool)n);
    
    #endregion

    #region Public Properties

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty); 
        set => SetValue(ItemsSourceProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty); 
        set => SetValue(SelectedItemProperty, value);
    }

    public string? Text
    {
        get => (string?)GetValue(TextProperty); 
        set => SetValue(TextProperty, value);
    }

    public string EntryDisplayPath
    {
        get => (string)GetValue(EntryDisplayPathProperty); 
        set => SetValue(EntryDisplayPathProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty); 
        set => SetValue(PlaceholderProperty, value);
    }

    public bool IsDropDownOpen
    {
        get => (bool)GetValue(IsDropDownOpenProperty); 
        set => SetValue(IsDropDownOpenProperty, value);
    }

    public int DebounceMilliseconds
    {
        get => (int)GetValue(DebounceMillisecondsProperty); 
        set => SetValue(DebounceMillisecondsProperty, value);
    }

    public new bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty); 
        set => SetValue(IsReadOnlyProperty, value);
    }

    public bool IsClearButtonVisible
    {
        get => (bool)GetValue(IsClearButtonVisibleProperty); set => SetValue(IsClearButtonVisibleProperty, value);
    }

    public Style? EntryStyle
    {
        get => (Style?)GetValue(EntryStyleProperty); 
        set => SetValue(EntryStyleProperty, value);
    }

    public Style? CollectionViewStyle
    {
        get => (Style?)GetValue(CollectionViewStyleProperty); 
        set => SetValue(CollectionViewStyleProperty, value);
    }

    public Style? ClearButtonStyle
    {
        get => (Style?)GetValue(ClearButtonStyleProperty); 
        set => SetValue(ClearButtonStyleProperty, value);
    }

    public Color DropDownBackgroundColor
    {
        get => (Color)GetValue(DropDownBackgroundColorProperty); 
        set => SetValue(DropDownBackgroundColorProperty, value);
    }

    #endregion
    
    public ComboBox()
    {
        // Step 1: Create the editable Entry
        _entry = new Entry
        {
            Margin = Thickness.Zero,
            Keyboard = Keyboard.Create(KeyboardFlags.None)
        };

        // Step 2: Create the clear Button
        _clearButton = new Button
        {
            Text = "×",
            WidthRequest = 30,
            IsVisible = false,
            VerticalOptions = LayoutOptions.Center
        };
        
        // Step 3: Create the dropdown CollectionView
        _collectionView = new CollectionView
        {
            IsVisible = false,
            Margin = Thickness.Zero,
            HeightRequest = 200,
            SelectionMode = SelectionMode.Single,
            HorizontalOptions = LayoutOptions.Fill
        };

        // Step 4: Modern MAUI-native debounce timer
        _debounceTimer = Dispatcher.CreateTimer();
        _debounceTimer.Interval = TimeSpan.FromMilliseconds(DebounceMilliseconds);
        _debounceTimer.Tick += OnDebounceTimerTick;

        // Wire events (UI thread safe)
        _entry.Focused += (_, _) => IsDropDownOpen = true;
        _entry.Unfocused += (_, _) => IsDropDownOpen = false;
        _entry.TextChanged += OnEntryTextChanged;
        _clearButton.Clicked += (_, _) => { Text = string.Empty; SelectedItem = null; };
        _collectionView.SelectionChanged += OnCollectionViewSelectionChanged;

        // Layout
        Children.Add(_entry);
        Children.Add(_clearButton);
        Children.Add(_collectionView);
    }

    /// <summary>
    /// Clean up timer and cancellation token to prevent memory leaks.
    /// </summary>
    public void Dispose()
    {
        _debounceTimer.Stop();
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
    
    private void OnEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressFiltering)
        {
            return;
        }

        TextChanged?.Invoke(this, e);

        // Auto-toggle clear button visibility
        _clearButton.IsVisible = !string.IsNullOrEmpty(e.NewTextValue);
        
        _debounceTimer.Stop();
        _debounceTimer.Interval = TimeSpan.FromMilliseconds(DebounceMilliseconds);
        _debounceTimer.Start();
    }

    private void OnDebounceTimerTick(object? sender, EventArgs e)
    {
        // Already on UI thread thanks to IDispatcherTimer
        if (string.IsNullOrEmpty(_entry.Text))
        {
            _suppressSelectionFiltering = true;
            _collectionView.SelectedItem = null;
            _suppressSelectionFiltering = false;
        }

        IsDropDownOpen = true;
    }

    private void OnCollectionViewSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelectionFiltering 
            || e.CurrentSelection is not { Count: > 0 } 
            || e.CurrentSelection[0] is not object selectedItem)
        {
            return;
        }

        _suppressFiltering = true;

        Text = !string.IsNullOrEmpty(EntryDisplayPath)
            ? selectedItem.GetType().GetProperty(EntryDisplayPath)?.GetValue(selectedItem)?.ToString() ?? string.Empty
            : selectedItem.ToString() ?? string.Empty;

        _suppressFiltering = false;
        IsDropDownOpen = false;
        
        _entry.Unfocus();

        SelectedItem = selectedItem;
        SelectedItemChanged?.Invoke(this, e);
    }
    
    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        
        _entry.BindingContext = BindingContext;
        _clearButton.BindingContext = BindingContext;
        _collectionView.BindingContext = BindingContext;
    }
}