using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;
using MauiControls.Collections;
using MauiControls.DataGridInternals;
using MauiControls.DataSorting;
using MauiControls.Extensions;
using Font = Microsoft.Maui.Font;

namespace MauiControls;

public class DataGrid : Grid
{
    private static readonly SortedSet<int> DefaultPageSizeSet = [5, 10, 50, 100, 200, 1000];

    private readonly WeakEventManager _itemSelectedEventManager = new();
    private readonly WeakEventManager _refreshingEventManager = new();
    private readonly WeakEventManager _rowsBackgroundColorPaletteChangedEventManager = new();
    private readonly WeakEventManager _rowsTextColorPaletteChangedEventManager = new();
    private readonly SortedSet<int> _pageSizeList = [.. DefaultPageSizeSet];
    private readonly ConcurrentDictionary<string, PropertyInfo?> _propertyCache = [];
    private readonly Lock _reloadLock = new();
    private readonly Lock _sortAndPaginateLock = new();

    private DataGridColumn? _sortedColumn;
    private HashSet<object>? _internalItemsHashSet;

    #region DisplayControls

    private RowDefinition _headerRowDefinition;
    private RowDefinition _contentRowDefinition;
    private RowDefinition _footerRowDefinition;

    // ====================== Header =====================
    private DataGridHeaderRow _dataGridHeaderRow;

    // ================ Data Row Container ===============
    private RefreshView _refreshView;
    private CollectionView _rowCollectionView;

    private Grid _footerGrid;

    // ====================== Footer =====================
    // ------------------- Footer Left -------------------
    private HorizontalStackLayout _pageSizeLayout;
    private Label _perPageText;

    private Picker _pagePicker;

    // ------------------- Footer Right -------------------
    private HorizontalStackLayout _pagingLayout;
    private Label _pageText;
    private Label _pageNumber;
    private Stepper _pagingStepper;

    #endregion

    #region Events

    /// <summary>
    /// Occurs when an item is selected in the DataGrid.
    /// </summary>
    public event EventHandler<SelectionChangedEventArgs> ItemSelected
    {
        add => _itemSelectedEventManager.AddEventHandler(value);
        remove => _itemSelectedEventManager.RemoveEventHandler(value);
    }

    /// <summary>
    /// Occurs when the DataGrid is being refreshed.
    /// </summary>
    public event EventHandler Refreshing
    {
        add => _refreshingEventManager.AddEventHandler(value);
        remove => _refreshingEventManager.RemoveEventHandler(value);
    }

    /// <summary>
    /// Occurs when the <see cref="RowsBackgroundColorPalette"/> of the DataGrid is changed.
    /// </summary>
    internal event EventHandler RowsBackgroundColorPaletteChanged
    {
        add => _rowsBackgroundColorPaletteChangedEventManager.AddEventHandler(value);
        remove => _rowsBackgroundColorPaletteChangedEventManager.RemoveEventHandler(value);
    }

    /// <summary>
    /// Occurs when the <see cref="RowsTextColorPalette"/> of the DataGrid is changed.
    /// </summary>
    internal event EventHandler RowsTextColorPaletteChanged
    {
        add => _rowsTextColorPaletteChangedEventManager.AddEventHandler(value);
        remove => _rowsTextColorPaletteChangedEventManager.RemoveEventHandler(value);
    }

    #endregion

    #region Properties

    /// <summary>
    /// Default style for the header label
    /// </summary>
    internal static Style DefaultHeaderLabelStyle { get; } = new Style(typeof(Label))
    {
        Setters =
        {
            new Setter { Property = Label.FontAttributesProperty, Value = FontAttributes.Bold },
            new Setter { Property = Label.HorizontalOptionsProperty, Value = LayoutOptions.Center },
            new Setter { Property = Label.VerticalOptionsProperty, Value = LayoutOptions.Center },
            new Setter { Property = Label.TextColorProperty, Value = Colors.Black },
            new Setter { Property = Label.LineBreakModeProperty, Value = LineBreakMode.TailTruncation }
        }
    };

    /// <summary>
    /// Default style for the header filter entry
    /// </summary>
    internal static Style DefaultHeaderFilterStyle { get; } = new Style(typeof(Entry))
    {
        Setters =
        {
            new Setter { Property = Entry.TextColorProperty, Value = Colors.Black },
            new Setter { Property = Entry.PlaceholderColorProperty, Value = Colors.Black },
        }
    };

    /// <summary>
    /// Default style for the sort icon
    /// </summary>
    internal static Style DefaultSortIconStyle { get; } = new Style(typeof(Polygon))
    {
        Setters =
        {
            new Setter { Property = Polygon.AspectProperty, Value = Stretch.Uniform },
            new Setter { Property = Polygon.FillProperty, Value = Colors.Black },
            new Setter
            {
                Property = Polygon.PointsProperty, Value = new PointCollection
                {
                    new Point(50, 0),
                    new Point(0, 80),
                    new Point(100, 80)
                }
            },
            new Setter { Property = Polygon.MarginProperty, Value = new Thickness(0, 0, 3, 0) },
            new Setter { Property = Polygon.MaximumHeightRequestProperty, Value = 10 }
        }
    };

    /// <summary>
    /// Default style for the data grid paging control
    /// </summary>
    public static Style DefaultPaginationStepperStyle { get; } = new Style(typeof(Stepper))
    {
        Setters =
        {
            new Setter { Property = Stepper.MarginProperty, Value = new Thickness(5) },
            new Setter { Property = Stepper.VerticalOptionsProperty, Value = LayoutOptions.Center },
            new Setter
            {
                Property = Stepper.BackgroundColorProperty, Value = new OnPlatform<Color>()
                {
                    Platforms =
                    {
                        new On
                        {
                            Platform = new List<string> { "WinUI" },
                            Value = Colors.Black
                        }
                    }
                }
            }
        }
    };

    /// <summary>
    /// Gets or sets selected Row color.
    /// </summary>
    public Color ActiveRowColor
    {
        get => (Color)GetValue(ActiveRowColorProperty);
        set => SetValue(ActiveRowColorProperty, value);
    }

    /// <summary>
    /// Gets or sets the background color of the column header
    /// Default value is <see cref="Colors.White"/>.
    /// </summary>
    public Color HeaderBackground
    {
        get => (Color)GetValue(HeaderBackgroundProperty);
        set => SetValue(HeaderBackgroundProperty, value);
    }

    /// <summary>
    /// Gets or sets backgroundColor of the footer that contains pagination elements
    /// Default value is <see cref="Colors.White"/>.
    /// </summary>
    public Color FooterBackground
    {
        get => (Color)GetValue(FooterBackgroundProperty);
        set => SetValue(FooterBackgroundProperty, value);
    }

    /// <summary>
    /// Gets or sets textColor of the footer that contains pagination elements
    /// Default value is <see cref="Colors.Black"/>.
    /// </summary>
    public Color FooterTextColor
    {
        get => (Color)GetValue(FooterTextColorProperty);
        set => SetValue(FooterTextColorProperty, value);
    }

    /// <summary>
    /// Gets or sets border color
    /// Default Value is <see cref="Colors.Black"/>.
    /// </summary>
    public Color BorderColor
    {
        get => (Color)GetValue(BorderColorProperty);
        set => SetValue(BorderColorProperty, value);
    }

    /// <summary>
    /// Gets or sets <see cref="ItemSizingStrategy"/>
    /// Default Value is <see cref="ItemSizingStrategy.MeasureFirstItem"/>.
    /// </summary>
    public ItemSizingStrategy ItemSizingStrategy
    {
        get => (ItemSizingStrategy)GetValue(ItemSizingStrategyProperty);
        set => SetValue(ItemSizingStrategyProperty, value);
    }

    /// <summary>
    /// Gets or sets the row to set to edit mode.
    /// </summary>
    public object RowToEdit
    {
        get => GetValue(RowToEditProperty);
        set => SetValue(RowToEditProperty, value);
    }

    /// <summary>
    /// Gets or sets background color of the rows. It repeats colors consecutively for rows.
    /// </summary>
    public IColorProvider RowsBackgroundColorPalette
    {
        get => (IColorProvider)GetValue(RowsBackgroundColorPaletteProperty);
        set => SetValue(RowsBackgroundColorPaletteProperty, value);
    }

    /// <summary>
    /// Gets or sets text color of the rows. It repeats colors consecutively for rows.
    /// </summary>
    public IColorProvider RowsTextColorPalette
    {
        get => (IColorProvider)GetValue(RowsTextColorPaletteProperty);
        set => SetValue(RowsTextColorPaletteProperty, value);
    }

    /// <summary>
    /// Gets or sets executes the command when a row is tapped. Works with selection disabled.
    /// </summary>
    public ICommand RowTappedCommand
    {
        get => (ICommand)GetValue(RowTappedCommandProperty);
        set => SetValue(RowTappedCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets ItemsSource of the DataGrid.
    /// </summary>
    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// Gets or sets columns of the DataGrid.
    /// </summary>
    public ObservableCollection<DataGridColumn> Columns
    {
        get => (ObservableCollection<DataGridColumn>)GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    /// <summary>
    /// Gets or sets font size of the cells.
    /// It does not sets header font size. Use <see cref="HeaderLabelStyle"/> to set header font size.
    /// </summary>
    [TypeConverter(typeof(FontSizeConverter))]
    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the font family.
    /// It does not sets header font family. Use <see cref="HeaderLabelStyle"/> to set header font size.
    /// </summary>
    public string FontFamily
    {
        get => (string)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize
    {
        get => (int)GetValue(PageSizeProperty);
        set => SetValue(PageSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the list of available page sizes.
    /// </summary>
    public IList<int> PageSizeList
    {
        get => (IList<int>)GetValue(PageSizeListProperty);
        set => SetValue(PageSizeListProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the page size picker is visible.
    /// </summary>
    public bool PageSizeVisible
    {
        get => (bool)GetValue(PageSizeVisibleProperty);
        set => SetValue(PageSizeVisibleProperty, value);
    }

    /// <summary>
    /// Gets or sets the pagination stepper style.
    /// </summary>
    public Style? PaginationStepperStyle
    {
        get => (Style?)GetValue(PaginationStepperStyleProperty);
        set => SetValue(PaginationStepperStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the row height.
    /// </summary>
    public int RowHeight
    {
        get => (int)GetValue(RowHeightProperty);
        set => SetValue(RowHeightProperty, value);
    }

    /// <summary>
    /// Gets or sets footer height.
    /// </summary>
    public int FooterHeight
    {
        get => (int)GetValue(FooterHeightProperty);
        set => SetValue(FooterHeightProperty, value);
    }

    /// <summary>
    /// Gets or sets header height.
    /// </summary>
    public int HeaderHeight
    {
        get => (int)GetValue(HeaderHeightProperty);
        set => SetValue(HeaderHeightProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether sorting is enabled.
    /// Obsolete. Use <see cref="SortingEnabled"/> instead.
    /// </summary>
    [Obsolete("IsSortable is obsolete. Please use SortingEnabled instead.")]
    public bool IsSortable
    {
        get => (bool)GetValue(SortingEnabledProperty);
        set => SetValue(SortingEnabledProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets if the grid allows sorting. Default value is true.
    /// Sortable columns must implement <see cref="IComparable"/>
    /// If you want to enable or disable sorting for specific column please use <see cref="DataGridColumn.SortingEnabled"/> property.
    /// </summary>
    public bool SortingEnabled
    {
        get => (bool)GetValue(SortingEnabledProperty);
        set => SetValue(SortingEnabledProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets if the grid allows filtering. Default value is false.
    /// If you want to enable or disable filtering for specific column please use <see cref="DataGridColumn.FilteringEnabled"/> property.
    /// </summary>
    public bool FilteringEnabled
    {
        get => (bool)GetValue(FilteringEnabledProperty);
        set => SetValue(FilteringEnabledProperty, value);
    }

    /// <summary>
    /// Gets or sets the page number. Default value is 1.
    /// </summary>
    public int PageNumber
    {
        get => (int)GetValue(PageNumberProperty);
        set => SetValue(PageNumberProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether pagination is enabled in the DataGrid.
    /// Default value is False.
    /// </summary>
    public bool PaginationEnabled
    {
        get => (bool)GetValue(PaginationEnabledProperty);
        set => SetValue(PaginationEnabledProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether sets whether selection is enabled for the DataGrid.
    /// Default value is true.
    /// </summary>
    [Obsolete($"SelectionEnabled is obsolete. Please use {nameof(SelectionMode)} instead.")]
    public bool SelectionEnabled
    {
        get => (bool)GetValue(SelectionEnabledProperty);
        set => SetValue(SelectionEnabledProperty, value);
    }

    /// <summary>
    /// Gets or sets set the SelectionMode for the DataGrid.
    /// Default value is Single.
    /// </summary>
    public SelectionMode SelectionMode
    {
        get => (SelectionMode)GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

    /// <summary>
    /// Gets or sets the selected item.
    /// </summary>
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>
    /// Gets or sets the selected items.
    /// </summary>
    public IList<object> SelectedItems
    {
        get => (IList<object>)GetValue(SelectedItemsProperty);
        set => SetValue(SelectedItemsProperty, value);
    }

    /// <summary>
    /// Gets or sets the command to execute when refreshing via a pull gesture.
    /// </summary>
    public ICommand PullToRefreshCommand
    {
        get => (ICommand)GetValue(PullToRefreshCommandProperty);
        set => SetValue(PullToRefreshCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the parameter to pass to the <see cref="PullToRefreshCommand"/>.
    /// </summary>
    public object PullToRefreshCommandParameter
    {
        get => GetValue(PullToRefreshCommandParameterProperty);
        set => SetValue(PullToRefreshCommandParameterProperty, value);
    }

    /// <summary>
    /// Gets or sets the spinner color to use while refreshing.
    /// </summary>
    public Color RefreshColor
    {
        get => (Color)GetValue(RefreshColorProperty);
        set => SetValue(RefreshColorProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to display an ActivityIndicator.
    /// </summary>
    public bool IsRefreshing
    {
        get => (bool)GetValue(IsRefreshingProperty);
        set => SetValue(IsRefreshingProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether refreshing the DataGrid by a pull down command is enabled.
    /// </summary>
    public bool RefreshingEnabled
    {
        get => (bool)GetValue(RefreshingEnabledProperty);
        set => SetValue(RefreshingEnabledProperty, value);
    }

    /// <summary>
    /// Gets or sets border thickness for cells.
    /// </summary>
    public Thickness BorderThickness
    {
        get => (Thickness)GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to show the borders of header cells.
    /// Default value is true.
    /// </summary>
    public bool HeaderBordersVisible
    {
        get => (bool)GetValue(HeaderBordersVisibleProperty);
        set => SetValue(HeaderBordersVisibleProperty, value);
    }

    /// <summary>
    /// Gets or sets column index and sorting order for the DataGrid.
    /// </summary>
    public SortData? SortedColumnIndex
    {
        get => (SortData?)GetValue(SortedColumnIndexProperty);
        set => SetValue(SortedColumnIndexProperty, value);
    }

    /// <summary>
    /// Gets or sets style of the header label.
    /// Style's <see cref="Style.TargetType"/> must be Label.
    /// </summary>
    public Style HeaderLabelStyle
    {
        get => (Style)GetValue(HeaderLabelStyleProperty);
        set => SetValue(HeaderLabelStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets style of the header label.
    /// Style's <see cref="Style.TargetType"/> must be Label.
    /// </summary>
    public Style HeaderFilterStyle
    {
        get => (Style)GetValue(HeaderFilterStyleProperty);
        set => SetValue(HeaderFilterStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets sort icon.
    /// </summary>
    public Polygon SortIcon
    {
        get => (Polygon)GetValue(SortIconProperty);
        set => SetValue(SortIconProperty, value);
    }

    /// <summary>
    /// Gets or sets style of the sort icon
    /// Style's <see cref="Style.TargetType"/> must be Polygon.
    /// </summary>
    public Style SortIconStyle
    {
        get => (Style)GetValue(SortIconStyleProperty);
        set => SetValue(SortIconStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets view to show when there is no data to display.
    /// </summary>
    public View NoDataView
    {
        get => (View)GetValue(NoDataViewProperty);
        set => SetValue(NoDataViewProperty, value);
    }

    /// <summary>
    /// Gets the page count.
    /// </summary>
    public int PageCount
    {
        get;
        private set
        {
            if (value > 0)
            {
                field = value;
                _pagingStepper.IsEnabled = value > 1;

                if (value > 1)
                {
                    _pagingStepper.Maximum = value;
                }

                if (PageNumber > value)
                {
                    PageNumber = value;
                }
            }
            else
            {
                // Handle case where there is no data (value == 0) and assume 1 blank page
                // If (value < 0) something is wrong and try to fail gracefully by assuming 1 blank page
                field = 1;
                _pagingStepper.IsEnabled = false;
                PageNumber = 1;
            }
        }
    }

    /// <summary>
    /// Gets or sets the customized text for the 'Page' label in the pagination section.
    /// </summary>
    public string PageText
    {
        get => (string)GetValue(PageTextProperty);
        set => SetValue(PageTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the customized text for the 'Per Page' label in the pagination section.
    /// </summary>
    public string PerPageText
    {
        get => (string)GetValue(PerPageTextProperty);
        set => SetValue(PerPageTextProperty, value);
    }

    internal ObservableRangeCollection<object> InternalItems { get; } = [];

    #endregion

    #region Bindable Properties

    /// <summary>
    /// Gets or sets the color of the active row.
    /// </summary>
    public static readonly BindableProperty ActiveRowColorProperty =
        BindablePropertyExtension.Create<DataGrid, Color>(Color.FromRgb(128, 144, 160));

    /// <summary>
    /// Gets or sets the background color of the header.
    /// </summary>
    public static readonly BindableProperty HeaderBackgroundProperty =
        BindablePropertyExtension.Create<DataGrid, Color>(
            defaultValue: Colors.White,
            propertyChanged: (b, _, n) =>
            {
                if (b is DataGrid self && self._dataGridHeaderRow != null && !self.HeaderBordersVisible)
                {
                    foreach (var child in self._dataGridHeaderRow.Children)
                    {
                        if (child is DataGridCell cell)
                        {
                            cell.UpdateCellBackgroundColor(n);
                        }
                    }
                }
            });

    /// <summary>
    /// Gets or sets the Row Tapped Command.
    /// </summary>
    public static readonly BindableProperty RowTappedCommandProperty =
        BindablePropertyExtension.Create<DataGrid, ICommand>();

    /// <summary>
    /// Gets or sets the background color of the footer.
    /// </summary>
    public static readonly BindableProperty FooterBackgroundProperty =
        BindablePropertyExtension.Create<DataGrid, Color>(Colors.White);

    /// <summary>
    /// Gets or sets the text color of the footer.
    /// </summary>
    public static readonly BindableProperty FooterTextColorProperty =
        BindablePropertyExtension.Create<DataGrid, Color>(Colors.Black);

    /// <summary>
    /// Gets or sets the color of the border.
    /// </summary>
    public static readonly BindableProperty BorderColorProperty =
        BindablePropertyExtension.Create<DataGrid, Color>(
            defaultValue: Colors.Black,
            propertyChanged: (b, _, _) =>
            {
                var self = (DataGrid)b;

                if (self._dataGridHeaderRow != null && self.HeaderBordersVisible)
                {
                    self._dataGridHeaderRow.InitializeHeaderRow();
                }
            });

    /// <summary>
    /// Gets or sets the ItemSizingStrategy for the data grid.
    /// </summary>
    public static readonly BindableProperty ItemSizingStrategyProperty =
        BindablePropertyExtension.Create<DataGrid, ItemSizingStrategy>(ItemSizingStrategy.MeasureFirstItem);

    /// <summary>
    /// Gets or sets the row to edit.
    /// </summary>
    public static readonly BindableProperty RowToEditProperty =
        BindablePropertyExtension.Create<DataGrid, object>();

    /// <summary>
    /// Gets or sets the background color palette for the rows.
    /// </summary>
    public static readonly BindableProperty RowsBackgroundColorPaletteProperty =
        BindablePropertyExtension.Create<DataGrid, IColorProvider>(
            propertyChanged: (b, _, _) =>
            {
                if (b is DataGrid self)
                {
                    self._rowsBackgroundColorPaletteChangedEventManager.HandleEvent(self, EventArgs.Empty,
                        nameof(RowsBackgroundColorPaletteChanged));
                }
            },
            defaultValueCreator: _ => new PaletteCollection { Colors.White });

    /// <summary>
    /// Gets or sets the text color palette for the rows.
    /// </summary>
    public static readonly BindableProperty RowsTextColorPaletteProperty =
        BindablePropertyExtension.Create<DataGrid, IColorProvider>(
            propertyChanged: (b, _, _) =>
            {
                if (b is DataGrid self)
                {
                    self._rowsTextColorPaletteChangedEventManager.HandleEvent(self, EventArgs.Empty,
                        nameof(RowsTextColorPaletteChanged));
                }
            },
            defaultValueCreator: _ => new PaletteCollection { Colors.Black });

    /// <summary>
    /// Gets or sets the Columns for the DataGrid.
    /// </summary>
    public static readonly BindableProperty ColumnsProperty =
        BindablePropertyExtension.Create<DataGrid, ObservableCollection<DataGridColumn>>(
            propertyChanged: (b, o, n) =>
            {
                if (b is not DataGrid self)
                {
                    return;
                }

                if (o != null)
                {
                    o.CollectionChanged -= self.OnColumnsChanged;

                    foreach (var oldColumn in o)
                    {
                        oldColumn.SizeChanged -= self.OnColumnSizeChanged;
                    }
                }

                if (n != null)
                {
                    n.CollectionChanged += self.OnColumnsChanged;

                    foreach (var newColumn in n)
                    {
                        newColumn.SizeChanged += self.OnColumnSizeChanged;
                    }
                }

                self.Initialize();
            },
            defaultValueCreator: _ => []); // Note: defaultValueCreator needed to prevent errors during navigation

    /// <summary>
    /// Gets or sets the ItemsSource for the DataGrid.
    /// </summary>
    public static readonly BindableProperty ItemsSourceProperty =
        BindablePropertyExtension.Create<DataGrid, IEnumerable>(
            propertyChanged: (b, o, n) =>
            {
                if (b is not DataGrid self)
                {
                    return;
                }

                // Reset internal hash set, used for fast lookups
                self._internalItemsHashSet = null;

                // Unsubscribe from old collection's change event
                if (o is INotifyCollectionChanged oldCollection)
                {
                    oldCollection.CollectionChanged -= self.OnItemsSourceCollectionChanged;
                }

                // Subscribe to new collection's change event and update properties
                if (n is INotifyCollectionChanged newCollection)
                {
                    newCollection.CollectionChanged += self.OnItemsSourceCollectionChanged;
                }

                self._dataGridHeaderRow.InitializeHeaderRow(true);
                self.SortFilterAndPaginate();

                // Reset SelectedItem if it's not in the new collection
                if (self.SelectedItem != null && !self.GetInternalItems().Contains(self.SelectedItem))
                {
                    self.SelectedItem = null;
                }
            });

    /// <summary>
    /// Gets or sets a value indicating whether pagination is enabled in the DataGrid.
    /// </summary>
    public static readonly BindableProperty PaginationEnabledProperty =
        BindablePropertyExtension.Create<DataGrid, bool>(
            defaultValue: false,
            propertyChanged: (b, _, _) =>
            {
                if (b is DataGrid self)
                {
                    self.SortFilterAndPaginate();
                }
            });

    /// <summary>
    /// Gets or sets the text for the page label in the DataGrid.
    /// </summary>
    public static readonly BindableProperty PageTextProperty =
        BindablePropertyExtension.Create<DataGrid, string>(
            defaultValue: "Page:",
            propertyChanged: (b, _, _) =>
            {
                if (b is DataGrid self)
                {
                    self.OnPropertyChanged(nameof(PageText));
                }
            });

    /// <summary>
    /// Gets or sets the localized text for the per page label.
    /// </summary>
    public static readonly BindableProperty PerPageTextProperty =
        BindablePropertyExtension.Create<DataGrid, string>(
            defaultValue: "# per page:",
            propertyChanged: (b, _, _) =>
            {
                if (b is DataGrid self)
                {
                    self.OnPropertyChanged(nameof(PerPageText));
                }
            });

    /// <summary>
    /// Gets or sets the page count for the DataGrid.
    /// </summary>
    public static readonly BindableProperty PageCountProperty =
        BindablePropertyExtension.Create<DataGrid, int>(1, BindingMode.OneWayToSource);

    /// <summary>
    /// Gets or sets the page size for the DataGrid.
    /// </summary>
    public static readonly BindableProperty PageSizeProperty =
        BindablePropertyExtension.Create<DataGrid, int>(
            defaultValue: 100,
            BindingMode.TwoWay,
            validateValue: (_, v) => v > 0,
            propertyChanged: (b, _, _) =>
            {
                if (b is DataGrid self)
                {
                    self.PageNumber = 1;
                    self.SortFilterAndPaginate();
                    self.UpdatePageSizeList();
                }
            });

    /// <summary>
    /// Gets or sets the list of available page sizes for the DataGrid.
    /// </summary>
    public static readonly BindableProperty PageSizeListProperty =
        BindablePropertyExtension.Create<DataGrid, IList<int>>(
            propertyChanged: (b, _, _) =>
            {
                if (b is DataGrid self)
                {
                    self.UpdatePageSizeList();
                }
            },
            defaultValueCreator: _ => [.. DefaultPageSizeSet!]);

    /// <summary>
    /// Gets or sets a value indicating whether the page size is visible in the DataGrid.
    /// </summary>
    public static readonly BindableProperty PageSizeVisibleProperty =
        BindablePropertyExtension.Create<DataGrid, bool>(true);

    /// <summary>
    /// Gets or sets the list of available page sizes for the DataGrid.
    /// </summary>
    public static readonly BindableProperty PaginationStepperStyleProperty =
        BindablePropertyExtension.Create<DataGrid, Style>(
            defaultValue: DefaultPaginationStepperStyle);

    /// <summary>
    /// Gets or sets the row height for the DataGrid.
    /// </summary>
    public static readonly BindableProperty RowHeightProperty =
        BindablePropertyExtension.Create<DataGrid, int>(40);

    /// <summary>
    /// Gets or sets the height of the footer in the DataGrid.
    /// </summary>
    public static readonly BindableProperty FooterHeightProperty =
        BindablePropertyExtension.Create<DataGrid, int>(DeviceInfo.Platform == DevicePlatform.Android ? 50 : 40);

    /// <summary>
    /// Gets or sets the height of the header in the DataGrid.
    /// </summary>
    public static readonly BindableProperty HeaderHeightProperty =
        BindablePropertyExtension.Create<DataGrid, int>(40);

    /// <summary>
    /// Gets or sets a value indicating whether the DataGrid allows sorting.
    /// </summary>
    public static readonly BindableProperty SortingEnabledProperty =
        BindablePropertyExtension.Create<DataGrid, bool>(
            defaultValue: true,
            propertyChanged: (b, _, _) =>
            {
                if (b is DataGrid self)
                {
                    self._dataGridHeaderRow.InitializeHeaderRow(true);
                }
            });

    /// <summary>
    /// Gets or sets a value indicating whether the DataGrid allows filtering.
    /// </summary>
    public static readonly BindableProperty FilteringEnabledProperty =
        BindablePropertyExtension.Create<DataGrid, bool>(
            defaultValue: false,
            propertyChanged: (b, _, _) =>
            {
                if (b is DataGrid self)
                {
                    self._dataGridHeaderRow.InitializeHeaderRow(true);
                }
            });

    /// <summary>
    /// Obsolete. Use <see cref="SortingEnabledProperty"/> instead.
    /// </summary>
    [Obsolete("IsSortableProperty is obsolete. Please use SortingEnabledProperty instead.")]
    public static readonly BindableProperty IsSortableProperty = SortingEnabledProperty;

    /// <summary>
    /// Gets or sets the font size for the DataGrid.
    /// </summary>
    public static readonly BindableProperty FontSizeProperty =
        BindablePropertyExtension.Create<DataGrid, double>(13.0);

    /// <summary>
    /// Gets or sets the font family for the DataGrid.
    /// </summary>
    public static readonly BindableProperty FontFamilyProperty =
        BindablePropertyExtension.Create<DataGrid, string>(Font.Default.Family);

    /// <summary>
    /// Gets or sets the selected item in the DataGrid.
    /// </summary>
    public static readonly BindableProperty SelectedItemProperty =
        BindablePropertyExtension.Create<DataGrid, object>(
            defaultValue: null,
            BindingMode.TwoWay,
            propertyChanged: (b, _, n) =>
            {
                if (b is DataGrid self && self._rowCollectionView.SelectedItem != n)
                {
                    self._rowCollectionView.SelectedItem = n;
                }
            },
            coerceValue: (b, v) =>
            {
                if (v is null || b is not DataGrid self || self.SelectionMode == SelectionMode.None)
                {
                    return null;
                }

                if (self.GetInternalItems().Contains(v))
                {
                    return v;
                }

                return null;
            });

    /// <summary>
    /// Gets or sets the selected items in the DataGrid.
    /// </summary>
    public static readonly BindableProperty SelectedItemsProperty =
        BindablePropertyExtension.Create<DataGrid, IList<object>>(
            defaultBindingMode: BindingMode.TwoWay,
            propertyChanged: (b, _, n) =>
            {
                var self = (DataGrid)b;

                if (self._rowCollectionView != null && self._rowCollectionView.SelectedItems != n)
                {
                    self._rowCollectionView.SelectedItems = n;
                }
            },
            coerceValue: (b, v) =>
            {
                if (b is not DataGrid self)
                {
                    throw new InvalidOperationException("SelectedItems can only be set on a DataGrid");
                }

                if (v is null || self.SelectionMode == SelectionMode.None)
                {
                    self.SelectedItems.Clear();
                    return self.SelectedItems;
                }

                if (v is not IList<object> selectedItems)
                {
                    throw new InvalidCastException($"{nameof(SelectedItems)} must be of type IList<object>");
                }

                var internalItems = self.GetInternalItems(v.Count);

                foreach (var selectedItem in selectedItems)
                {
                    if (!internalItems.Contains(selectedItem))
                    {
                        _ = selectedItems.Remove(selectedItem);
                    }
                }

                return selectedItems;
            },
            defaultValueCreator: _ => []);

    /// <summary>
    /// Gets or sets a value indicating whether selection is enabled in the DataGrid.
    /// Default value is true.
    /// </summary>
    [Obsolete($"SelectionEnabled is obsolete. Please use {nameof(SelectionMode)} instead.")]
    public static readonly BindableProperty SelectionEnabledProperty =
        BindablePropertyExtension.Create<DataGrid, bool>(
            defaultValue: true,
            propertyChanged: (b, _, n) =>
            {
                if (!n && b is DataGrid self)
                {
                    self.SelectedItem = null;
                    self.SelectedItems.Clear();

                    if (self.SelectionMode != SelectionMode.None)
                    {
                        self.SelectionMode = SelectionMode.None;
                    }
                }
            });

    /// <summary>
    /// Gets or sets a value indicating whether selection is enabled in the DataGrid.
    /// </summary>
    public static readonly BindableProperty SelectionModeProperty =
        BindablePropertyExtension.Create<DataGrid, SelectionMode>(
            defaultValue: SelectionMode.Single,
            BindingMode.TwoWay,
            propertyChanged: (b, _, n) =>
            {
                var self = (DataGrid)b;

                switch (n)
                {
                    case SelectionMode.None:
                        self.SelectedItem = null;
                        self.SelectedItems.Clear();
                        break;
                    case SelectionMode.Single:
                        self.SelectedItems.Clear();
                        break;
                    case SelectionMode.Multiple:
                        self.SelectedItem = null;
                        break;
                }

                self._rowCollectionView?.SelectionMode = n;
            });

    /// <summary>
    /// Gets or sets a value indicating whether refreshing is enabled in the DataGrid.
    /// </summary>
    public static readonly BindableProperty RefreshingEnabledProperty =
        BindablePropertyExtension.Create<DataGrid, bool>(
            defaultValue: true,
            propertyChanged: (b, _, n) =>
            {
                if (b is not DataGrid self)
                {
                    return;
                }

                if (self.PullToRefreshCommand?.CanExecute(n) != true)
                {
                    Debug.WriteLine("RefreshView cannot be executed.");
                }
            });

    /// <summary>
    /// Gets or sets the command to execute when the data grid is pulled to refresh.
    /// </summary>
    public static readonly BindableProperty PullToRefreshCommandProperty =
        BindablePropertyExtension.Create<DataGrid, ICommand>(
            propertyChanged: (b, _, n) =>
            {
                if (b is not DataGrid self)
                {
                    return;
                }

                if (n == null)
                {
                    self._refreshView.Command = null;
                }
                else
                {
                    self._refreshView.Command = n;
                    if (!self._refreshView.Command.CanExecute(self.RefreshingEnabled))
                    {
                        Debug.WriteLine("RefreshView cannot be executed.");
                    }
                }
            });

    /// <summary>
    /// Gets or sets the parameter to pass to the <see cref="PullToRefreshCommand"/>.
    /// </summary>
    public static readonly BindableProperty PullToRefreshCommandParameterProperty =
        BindablePropertyExtension.Create<DataGrid, object>();

    /// <summary>
    /// Gets or sets the spinner color to use while refreshing.
    /// </summary>
    public static readonly BindableProperty RefreshColorProperty =
        BindablePropertyExtension.Create<DataGrid, Color>(Colors.Purple);

    /// <summary>
    /// Gets or sets a value indicating whether the DataGrid is refreshing.
    /// </summary>
    public static readonly BindableProperty IsRefreshingProperty =
        BindablePropertyExtension.Create<DataGrid, bool>(false, BindingMode.TwoWay);

    /// <summary>
    /// Gets or sets the thickness of the border around the DataGrid.
    /// </summary>
    public static readonly BindableProperty BorderThicknessProperty =
        BindablePropertyExtension.Create<DataGrid, Thickness>(new Thickness(1), BindingMode.TwoWay);

    /// <summary>
    /// Gets or sets a value indicating whether the header borders are visible in the DataGrid.
    /// </summary>
    public static readonly BindableProperty HeaderBordersVisibleProperty =
        BindablePropertyExtension.Create<DataGrid, bool>(
            defaultValue: true,
            propertyChanged: (b, _, _) =>
            {
                if (b is DataGrid self)
                {
                    self._dataGridHeaderRow.InitializeHeaderRow();
                }
            });

    /// <summary>
    /// Gets or sets the index of the sorted column in the DataGrid.
    /// </summary>
    public static readonly BindableProperty SortedColumnIndexProperty =
        BindablePropertyExtension.Create<DataGrid, SortData?>(
            defaultValue: null,
            BindingMode.TwoWay,
            validateValue: (b, v) =>
            {
                var self = (DataGrid)b;

                if (!self.IsLoaded || self.Columns == null)
                {
                    return true;
                }

                return self.CanSort(v);
            },
            propertyChanged: (b, _, n) =>
            {
                if (b is DataGrid self)
                {
                    if (n != null && n.Index < self.Columns.Count)
                    {
                        self._sortedColumn = self.Columns[n.Index];
                    }

                    self.SortFilterAndPaginate(n);
                }
            });

    /// <summary>
    /// Gets or sets the current page number in the DataGrid.
    /// </summary>
    public static readonly BindableProperty PageNumberProperty =
        BindablePropertyExtension.Create<DataGrid, int>(
            defaultValue: 1,
            BindingMode.TwoWay,
            validateValue: (b, v) =>
            {
                if (v < 0)
                {
                    return false;
                }
                else if (b is DataGrid self)
                {
                    return v == 1 || v <= self.PageCount;
                }

                return false;
            },
            propertyChanged: (b, _, _) =>
            {
                if (b is DataGrid self)
                {
                    self.SortFilterAndPaginate();
                }
            });

    /// <summary>
    /// Gets or sets the style for the header labels in the DataGrid.
    /// </summary>
    public static readonly BindableProperty HeaderLabelStyleProperty =
        BindablePropertyExtension.Create<DataGrid, Style>(
            defaultValue: DefaultHeaderLabelStyle);

    /// <summary>
    /// Gets or sets the style for the column filters in the DataGrid.
    /// </summary>
    public static readonly BindableProperty HeaderFilterStyleProperty =
        BindablePropertyExtension.Create<DataGrid, Style>(
            defaultValue: DefaultHeaderFilterStyle);

    /// <summary>
    /// Gets or sets the sort icons for the DataGrid.
    /// </summary>
    public static readonly BindableProperty SortIconProperty =
        BindablePropertyExtension.Create<DataGrid, Polygon>();

    /// <summary>
    /// Gets or sets the style for the sort icons in the DataGrid.
    /// </summary>
    public static readonly BindableProperty SortIconStyleProperty =
        BindablePropertyExtension.Create<DataGrid, Style>(
            propertyChanged: (b, _, n) =>
            {
                if (b is DataGrid self)
                {
                    foreach (var column in self.Columns)
                    {
                        if (n is null)
                        {
                            column.SortingIcon.Style = DefaultSortIconStyle;
                        }
                        else
                        {
                            column.SortingIcon.Style = n;
                        }
                    }
                }
            });

    /// <summary>
    /// Gets or sets the view to be displayed when the DataGrid has no data.
    /// </summary>
    public static readonly BindableProperty NoDataViewProperty =
        BindablePropertyExtension.Create<DataGrid, View>(
            propertyChanged: (b, _, n) =>
            {
                if (b is DataGrid self)
                {
                    self._rowCollectionView.EmptyView = n;
                }
            });

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="DataGrid_Xaml"/> class.
    /// </summary>
    public DataGrid()
    {
        _headerRowDefinition = new RowDefinition(GridLength.Auto);
        _contentRowDefinition = new RowDefinition(GridLength.Star);
        _footerRowDefinition = new RowDefinition(GridLength.Auto);

        this.RowDefinitions.Add(_headerRowDefinition);
        this.RowDefinitions.Add(_contentRowDefinition);
        this.RowDefinitions.Add(_footerRowDefinition);

        CreateHeader();
        CreateRowContentContainer();
        CreateFooter();

        //TODO: Left off here. Need to finish creating the control. Use ComboBox.cs as an example.
        //TODO: Initialize controls (region: DisplayControls).
    }

    /// <summary>
    /// Scrolls to the row.
    /// </summary>
    /// <param name="item">Item to scroll.</param>
    /// <param name="position">Position of the row in screen.</param>
    /// <param name="animated">animated.</param>
    public void ScrollTo(object item, ScrollToPosition position, bool animated = true) =>
        _rowCollectionView.ScrollTo(item, position: position, animate: animated);

    internal void Initialize()
    {
        if (!IsLoaded)
        {
            return;
        }

        lock (_reloadLock)
        {
            UpdatePageSizeList();

            _dataGridHeaderRow.InitializeHeaderRow();
        }
    }

    internal void SortFilterAndPaginate(SortData? sortData = null)
    {
        if (ItemsSource is null)
        {
            return;
        }

        lock (_sortAndPaginateLock)
        {
            sortData ??= SortedColumnIndex;

            var originalItems = ItemsSource as IList<object> ?? [.. ItemsSource.Cast<object>()];

            if (originalItems.Count == 0)
            {
                PageCount = 1;
                InternalItems.Clear();
                return;
            }

            var filteredItems = CanFilter() ? GetFilteredItems(originalItems) : originalItems;

            var sortedItems = CanSort(sortData) ? GetSortedItems(filteredItems, sortData!) : filteredItems;

            var paginatedItems = PaginationEnabled ? GetPaginatedItems(sortedItems) : sortedItems;

            PageCount = (int)Math.Ceiling(filteredItems.Count / (double)PageSize);

            InternalItems.ReplaceRange(paginatedItems);
        }
    }

    /// <inheritdoc/>
    protected override void OnParentSet()
    {
        base.OnParentSet();

        if (Parent is null)
        {
            Loaded -= OnLoaded;
        }
        else
        {
            Loaded -= OnLoaded;
            Loaded += OnLoaded;
        }

        if (Parent is null)
        {
            _rowCollectionView.SelectionChanged -= OnSelectionChanged;
        }
        else
        {
            _rowCollectionView.SelectionChanged -= OnSelectionChanged;
            _rowCollectionView.SelectionChanged += OnSelectionChanged;
        }

        if (Parent is null)
        {
            _refreshView.Refreshing -= OnRefreshing;
        }
        else
        {
            _refreshView.Refreshing -= OnRefreshing;
            _refreshView.Refreshing += OnRefreshing;
        }

        if (Parent is null)
        {
            foreach (var column in Columns)
            {
                column.SizeChanged -= OnColumnSizeChanged;
            }

            Columns.CollectionChanged -= OnColumnsChanged;
        }
        else
        {
            foreach (var column in Columns)
            {
                column.SizeChanged -= OnColumnSizeChanged;
                column.SizeChanged += OnColumnSizeChanged;
            }

            Columns.CollectionChanged -= OnColumnsChanged;
            Columns.CollectionChanged += OnColumnsChanged;
        }
    }

    /// <inheritdoc/>
    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        _dataGridHeaderRow.InitializeHeaderRow();
    }

    private void OnLoaded(object? sender, EventArgs e) => Initialize();

    private void OnColumnsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var newSortedColumnIndex = RegenerateSortedColumnIndex();

        if (newSortedColumnIndex != SortedColumnIndex)
        {
            // This will do a SortAndPaginate via the propertyChanged event of the SortedColumnIndexProperty
            SortedColumnIndex = newSortedColumnIndex;
        }

        Initialize();
    }

    private void OnColumnSizeChanged(object? sender, EventArgs e) => Initialize();

    private void OnRefreshing(object? sender, EventArgs e) =>
        _refreshingEventManager.HandleEvent(this, e, nameof(Refreshing));

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _itemSelectedEventManager.HandleEvent(this, e, nameof(ItemSelected));
        RowTappedCommand?.Execute(e);
    }

    private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _internalItemsHashSet = null;
        SortFilterAndPaginate();
    }

    private void CreateHeader()
    {
        // Data Grid Header Row
        _dataGridHeaderRow = new DataGridHeaderRow
        {
            BindingContext = this,
            DataGrid = this
        };

        _dataGridHeaderRow.SetBinding(DataGridHeaderRow.HeightRequestProperty,
            "HeaderHeight");

        this.Add(_dataGridHeaderRow, 0, 0);
    }

    private void CreateRowContentContainer()
    {
        // Refresh View
        _refreshView = new RefreshView
        {
            BindingContext = this
        };

        _refreshView.SetBinding(RefreshView.CommandProperty,
            "PullToRefreshCommand");
        _refreshView.SetBinding(RefreshView.CommandParameterProperty,
            "PullToRefreshCommandParameter");
        _refreshView.SetBinding(RefreshView.RefreshColorProperty,
            "RefreshColor");
        _refreshView.SetBinding(RefreshView.IsRefreshingProperty,
            "IsRefreshing");
        _refreshView.SetBinding(RefreshView.IsEnabledProperty,
            "RefreshingEnabled", BindingMode.TwoWay);

        this.Add(_refreshView, 0, 1);

        // Row Collection View
        _rowCollectionView = new CollectionView
        {
            BindingContext = this
        };

        //TODO: May want a two-way binding if we add deletion to the grid...
        _rowCollectionView.ItemsSource = InternalItems;

        _rowCollectionView.SetBinding(CollectionView.BackgroundColorProperty,
            "BackgroundColor");
        _rowCollectionView.SetBinding(SelectableItemsView.SelectedItemProperty,
            "SelectedItem", BindingMode.TwoWay);
        _rowCollectionView.SetBinding(SelectableItemsView.SelectionChangedCommandProperty,
            "SelectedItems", BindingMode.TwoWay);
        _rowCollectionView.SetBinding(StructuredItemsView.ItemSizingStrategyProperty,
            "ItemSizingStrategy");
        _rowCollectionView.SetBinding(SelectableItemsView.SelectionChangedCommandProperty,
            "SelectionMode");

        var innerCollectionItemTemplate = new DataTemplate(() =>
        {
            var dataGridRow = new DataGridRow
            {
                BindingContext = this,
                DataGrid = this
            };

            dataGridRow.SetBinding(DataGridRow.RowToEditProperty, "RowToEdit");
            dataGridRow.SetBinding(DataGridRow.HeightRequestProperty, "RowHeight");

            return dataGridRow;
        });

        _rowCollectionView.ItemTemplate = innerCollectionItemTemplate;

        _refreshView.Content = _rowCollectionView;
    }

    private void CreateFooter()
    {
        // Footer Grid
        _footerGrid = new Grid();

        var leftColumnDefinition = new ColumnDefinition(GridLength.Auto);
        var middleColumnDefinition = new ColumnDefinition(GridLength.Star);
        var rightColumnDefinition = new ColumnDefinition(GridLength.Auto);

        _footerGrid.AddColumnDefinition(leftColumnDefinition);
        _footerGrid.AddColumnDefinition(middleColumnDefinition);
        _footerGrid.AddColumnDefinition(rightColumnDefinition);

        this.Add(_footerGrid, 0, 2);

        //--------------------- Page Size (left column) -------------------------
        // Page Size Layout 
        _pageSizeLayout = new HorizontalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            BindingContext = this
        };

        _pageSizeLayout.SetBinding(HorizontalStackLayout.IsVisibleProperty,
            "PageSizeVisible");

        _footerGrid.Add(_pageSizeLayout, 0, 0);

        // Page Text Label
        _perPageText = new Label
        {
            BindingContext = this,
            Margin = new Thickness(0, 0, 0, 0),
            VerticalOptions = LayoutOptions.Center
        };

        _perPageText.SetBinding(Label.TextProperty,
            "PerPageText");
        _perPageText.SetBinding(Label.TextColorProperty,
            "FooterTextColor");

        _pageSizeLayout.Add(_perPageText);

        // Page Picker
        _pagePicker = new Picker
        {
            BindingContext = this,
            MinimumWidthRequest = 50.0d
        };

        _pagePicker.SetBinding(Picker.ItemsSourceProperty,
            "PageSizeList", BindingMode.TwoWay);
        _pagePicker.SetBinding(Picker.SelectedItemProperty,
            "PageSize");
        _pagePicker.SetBinding(Picker.TextColorProperty,
            "FooterTextColor");
        _pagePicker.SetBinding(Picker.TitleColorProperty,
            "FooterTextColor");

        _pageSizeLayout.Add(_pagePicker);

        //--------------------- Paging (right column) --------------------------
        // Paging Layout
        _pagingLayout = new HorizontalStackLayout
        {
            BindingContext = this,
            VerticalOptions = LayoutOptions.Center
        };

        _footerGrid.Add(_pagingLayout, 2, 0);

        // Page Text Label
        _pageText = new Label
        {
            BindingContext = this,
            Margin = new Thickness(0, 0, 5, 0),
            VerticalTextAlignment = TextAlignment.Center
        };

        _pageText.SetBinding(Label.TextProperty,
            "PageText");
        _pageText.SetBinding(Label.TextColorProperty,
            "TextColor");

        _pagingLayout.Add(_pageText);

        // Page Number Label
        _pageNumber = new Label
        {
            BindingContext = this,
            VerticalTextAlignment = TextAlignment.Center
        };

        _pageNumber.SetBinding(Label.TextProperty,
            "PageText");
        _pageNumber.SetBinding(Label.TextColorProperty,
            "FooterTextColor");

        _pagingLayout.Add(_pageNumber);

        // Paging Stepper
        _pagingStepper = new Stepper
        {
            BindingContext = this,
            Minimum = 1
        };

        _pagingStepper.SetBinding(Stepper.ValueProperty,
            "PageNumber");
        _pagingStepper.SetBinding(Stepper.StyleProperty,
            "PaginationStepperStyle");

        _pagingLayout.Add(_pagingStepper);
    }

    private ICollection<object> GetInternalItems(int lookupCount = 1)
    {
        if (_internalItemsHashSet != null)
        {
            return _internalItemsHashSet;
        }

        if (lookupCount <= 1)
        {
            return InternalItems;
        }

        return _internalItemsHashSet = [.. InternalItems];
    }

    private SortData? RegenerateSortedColumnIndex()
    {
        if (_sortedColumn == null || SortedColumnIndex == null)
        {
            return SortedColumnIndex;
        }

        var newSortedColumnIndex = Columns.IndexOf(_sortedColumn);

        if (newSortedColumnIndex == -1)
        {
            return null;
        }

        return new(newSortedColumnIndex, SortedColumnIndex.Order);
    }

    private bool CanFilter() => FilteringEnabled && Columns.Any(c => c.FilteringEnabled);

    private bool CanSort(SortData? sortData)
    {
        if (sortData is null)
        {
            Debug.WriteLine("No sort data");
            return false;
        }

        if (InternalItems.Count == 0)
        {
            Debug.WriteLine("There are no items to sort");
            return false;
        }

        if (!SortingEnabled)
        {
            Debug.WriteLine("DataGrid is not sortable");
            return false;
        }

        if (Columns.Count < 1)
        {
            Debug.WriteLine("There are no columns on this DataGrid.");
            return false;
        }

        if (sortData.Index >= Columns.Count)
        {
            Debug.WriteLine("Sort index is out of range");
            return false;
        }

        var columnToSort = Columns[sortData.Index];

        if (columnToSort.PropertyName == null)
        {
            Debug.WriteLine($"Please set the {nameof(columnToSort.PropertyName)} of the column");
            return false;
        }

        if (!columnToSort.SortingEnabled)
        {
            Debug.WriteLine($"{columnToSort.PropertyName} column does not have sorting enabled");
            return false;
        }

        if (!columnToSort.IsSortable())
        {
            Debug.WriteLine($"{columnToSort.PropertyName} column is not sortable");
            return false;
        }

        return true;
    }

    private IEnumerable<object> GetSortedItems(IList<object> unsortedItems, SortData sortData)
    {
        _sortedColumn ??= Columns[sortData.Index];

        foreach (var column in Columns)
        {
            if (column == _sortedColumn)
            {
                column.SortDirection = sortData.Order;
                column.SortingIconContainer.IsVisible = true;
            }
            else
            {
                column.SortDirection = SortDirection.None;
                column.SortingIconContainer.IsVisible = false;
            }
        }


        List<object> items = new() { };

        if (_sortedColumn == null)
        {
            return items;
        }

        switch (sortData.Order)
        {
            case SortDirection.Ascending:
            {
                _ = _sortedColumn.SortingIcon.RotateToAsync(0);

                items = unsortedItems.OrderBy(x => x.GetValueByPath(_sortedColumn.PropertyName)).ToList();
                break;
            }
            case SortDirection.Descending:
            {
                _ = _sortedColumn.SortingIcon.RotateToAsync(180);

                items = unsortedItems.OrderByDescending(x => x.GetValueByPath(_sortedColumn.PropertyName)).ToList();
                break;
            }
            default:
            {
                return unsortedItems;
            }
        }

        return items;
    }

    private IList<object> GetFilteredItems(IList<object> originalItems)
    {
        var filteredItems = originalItems.AsEnumerable();

        foreach (var column in Columns)
        {
            if (!column.FilteringEnabled || string.IsNullOrEmpty(column.FilterText))
            {
                continue;
            }

            filteredItems = filteredItems.Where(item => FilterItem(item, column));
        }

        return [.. filteredItems];
    }

    private bool FilterItem(object item, DataGridColumn column)
    {
        try
        {
            if (string.IsNullOrEmpty(column.FilterText))
            {
                return true;
            }

            var itemType = item.GetType();
            var cacheKey = $"{itemType.FullName}|{column.PropertyName}";

            if (!_propertyCache.TryGetValue(cacheKey, out var property))
            {
                property = itemType.GetProperty(column.PropertyName);
                _propertyCache[cacheKey] = property;
            }

            if (property == null || property.PropertyType == typeof(object))
            {
                return false;
            }

            var value = property.GetValue(item)?.ToString();
            return value?.Contains(column.FilterText, StringComparison.OrdinalIgnoreCase) == true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return false;
        }
    }

    private IEnumerable<object> GetPaginatedItems(IEnumerable<object> unpaginatedItems)
    {
        var skip = (PageNumber - 1) * PageSize;

        return unpaginatedItems.Skip(skip).Take(PageSize);
    }

    /// <summary>
    /// Checks if PageSizeList contains the new PageSize value, so that it shows in the dropdown.
    /// </summary>
    private void UpdatePageSizeList()
    {
        if (_pageSizeList.Contains(PageSize))
        {
            return;
        }

        if (_pageSizeList.Add(PageSize))
        {
            PageSizeList = [.. _pageSizeList];
            OnPropertyChanged(nameof(PageSizeList));
            OnPropertyChanged(nameof(PageSize));
        }
    }
}