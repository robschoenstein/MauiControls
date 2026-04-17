// Copyright © 2026 Robert Schoenstein. All rights reserved.
// Unauthorized use, reproduction, or distribution is strictly prohibited.

using System.Collections;
using System.Timers;
using MauiControls.Extensions;

namespace MauiControls;

/// <summary>
/// Enterprise-grade ComboBox / AutoComplete control.
/// Built entirely in C# with full styling support and debouncing.
/// </summary>
public class ComboBox : VerticalStackLayout
{
    private readonly Entry _entry;
    private readonly CollectionView _collectionView;
    private readonly System.Timers.Timer _debounceTimer;

    private bool _suppressFiltering;
    private bool _suppressSelectionFiltering;

    public event EventHandler<SelectionChangedEventArgs> SelectedItemChanged;
    public event EventHandler<TextChangedEventArgs> TextChanged;

    #region Bindable Properties (using BindablePropertyExtension)

    public static readonly BindableProperty ItemsSourceProperty =
        BindablePropertyExtension.Create<ComboBox, IEnumerable>(
            defaultValue: null,
            propertyChanged: (b, _, n) => ((ComboBox)b)._collectionView.ItemsSource = (IEnumerable)n);

    public static readonly BindableProperty SelectedItemProperty =
        BindablePropertyExtension.Create<ComboBox, object>(
            defaultBindingMode: BindingMode.TwoWay,
            propertyChanged: (b, _, n) => ((ComboBox)b)._collectionView.SelectedItem = n);

    public static readonly BindableProperty EntryDisplayPathProperty =
        BindablePropertyExtension.Create<ComboBox, string>(defaultValue: string.Empty);

    public static readonly BindableProperty PlaceholderProperty =
        BindablePropertyExtension.Create<ComboBox, string>(
            defaultValue: string.Empty,
            propertyChanged: (b, _, n) => ((ComboBox)b)._entry.Placeholder = (string)n);

    public static readonly BindableProperty ListViewHeightRequestProperty =
        BindablePropertyExtension.Create<ComboBox, double>(
            defaultValue: 200d,
            propertyChanged: (b, _, n) => ((ComboBox)b)._collectionView.HeightRequest = (double)n);

    public static readonly BindableProperty IsDropDownOpenProperty =
        BindablePropertyExtension.Create<ComboBox, bool>(
            defaultValue: false,
            propertyChanged: (b, _, n) => ((ComboBox)b)._collectionView.IsVisible = (bool)n);

    public static readonly BindableProperty DebounceMillisecondsProperty =
        BindablePropertyExtension.Create<ComboBox, int>(defaultValue: 300);

    // Styling properties
    public static readonly BindableProperty EntryStyleProperty =
        BindablePropertyExtension.Create<ComboBox, Style>(
            defaultValue: null,
            propertyChanged: (b, _, n) => ((ComboBox)b)._entry.Style = (Style)n);

    public static readonly BindableProperty CollectionViewStyleProperty =
        BindablePropertyExtension.Create<ComboBox, Style>(
            defaultValue: null,
            propertyChanged: (b, _, n) => ((ComboBox)b)._collectionView.Style = (Style)n);

    public static readonly BindableProperty DropDownBackgroundColorProperty =
        BindablePropertyExtension.Create<ComboBox, Color>(
            defaultValue: Colors.White,
            propertyChanged: (b, _, n) => ((ComboBox)b)._collectionView.BackgroundColor = (Color)n);

    #endregion

    #region Public Properties

    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
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

    public double ListViewHeightRequest
    {
        get => (double)GetValue(ListViewHeightRequestProperty);
        set => SetValue(ListViewHeightRequestProperty, value);
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

    public Style EntryStyle
    {
        get => (Style)GetValue(EntryStyleProperty);
        set => SetValue(EntryStyleProperty, value);
    }

    public Style CollectionViewStyle
    {
        get => (Style)GetValue(CollectionViewStyleProperty);
        set => SetValue(CollectionViewStyleProperty, value);
    }

    public Color DropDownBackgroundColor
    {
        get => (Color)GetValue(DropDownBackgroundColorProperty);
        set => SetValue(DropDownBackgroundColorProperty, value);
    }

    #endregion

    public ComboBox()
    {
        _entry = new Entry
        {
            Margin = new Thickness(0),
            Keyboard = Keyboard.Create(KeyboardFlags.None)
        };

        _collectionView = new CollectionView
        {
            IsVisible = false,
            Margin = new Thickness(0),
            HeightRequest = 200,
            SelectionMode = SelectionMode.Single,
            HorizontalOptions = LayoutOptions.Fill
        };

        _debounceTimer = new System.Timers.Timer { AutoReset = false };
        _debounceTimer.Elapsed += OnDebounceTimerElapsed;

        // Wire events
        _entry.Focused += (_, _) => IsDropDownOpen = true;
        _entry.Unfocused += (_, _) => IsDropDownOpen = false;
        _entry.TextChanged += OnEntryTextChanged;
        _collectionView.SelectionChanged += OnCollectionViewSelectionChanged;

        // Bottom border
        var border = new BoxView
        {
            HeightRequest = 1,
            Color = Colors.Black,
            Margin = new Thickness(0)
        };
        border.SetBinding(BoxView.IsVisibleProperty,
            new Binding(nameof(CollectionView.IsVisible), source: _collectionView));

        Children.Add(_entry);
        Children.Add(_collectionView);
        Children.Add(border);
    }

    private void OnEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressFiltering) return;

        TextChanged?.Invoke(this, e);

        _debounceTimer.Stop();
        _debounceTimer.Interval = DebounceMilliseconds;
        _debounceTimer.Start();
    }

    private void OnDebounceTimerElapsed(object sender, ElapsedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (string.IsNullOrEmpty(_entry.Text))
            {
                _suppressSelectionFiltering = true;
                _collectionView.SelectedItem = null;
                _suppressSelectionFiltering = false;
            }

            IsDropDownOpen = true;
        });
    }

    private void OnCollectionViewSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection == null 
            || e.CurrentSelection.Count == 0
            || e.CurrentSelection.FirstOrDefault() == null)
        {
            return;
        }
        
        var selectedItem = e.CurrentSelection.FirstOrDefault();
        
        if (selectedItem == null)
        {
            return;
        }
        
        if (_suppressSelectionFiltering)
        {
            return;
        }

        _suppressFiltering = true;

        if (!string.IsNullOrEmpty(EntryDisplayPath))
        {
            var prop = selectedItem.GetType().GetProperty(EntryDisplayPath);
            _entry.Text = prop?.GetValue(selectedItem)?.ToString() ?? string.Empty;
        }
        else
        {
            var text = selectedItem.ToString() ?? string.Empty;
                
            _entry.Text = text;
        }

        _suppressFiltering = false;
        IsDropDownOpen = false;
        _entry.Unfocus();

        SelectedItemChanged?.Invoke(this, e);
    }

    public new bool Focus() => _entry.Focus();
    public new void Unfocus() => _entry.Unfocus();
}