// Copyright © 2026 Robert Schoenstein. All rights reserved.
// Unauthorized use, reproduction, or distribution is strictly prohibited.

using MauiControls.Converters;

namespace MauiControls.DataGridInternals;

internal sealed class DataGridCell : ContentView
{
    public DataGridColumn Column { get; }
    public bool IsEditing { get; }
    
    internal DataGridCell(View cellContent, Color? backgroundColor, DataGridColumn column, bool isEditing)
    {
        Column = column;
        IsEditing = isEditing;
        
        //TODO: Make sure this doesn't screw with child controls
        BackgroundColor = backgroundColor;
        
        Content = new ContentView
        {
            BackgroundColor = backgroundColor,
            Content = cellContent,
        };
        
        SetupAccessibility();
    }

    internal void UpdateBindings(DataGrid dataGrid)
    {
        if (dataGrid.HeaderBordersVisible)
        {
            SetBinding(BackgroundColorProperty, new Binding(nameof(DataGrid.BorderColor), source: dataGrid));
            SetBinding(PaddingProperty, new Binding(nameof(DataGrid.BorderThickness), converter: new BorderThicknessToCellPaddingConverter(), source: dataGrid));
        }
        else
        {
            RemoveBinding(BackgroundColorProperty);
            RemoveBinding(PaddingProperty);
            Padding = Thickness.Zero;
        }
    }

    internal void UpdateCellBackgroundColor(Color? bgColor)
    {
        //Set this controls background color
        this.BackgroundColor = bgColor;
        
        if (Content is ContentView cv)
        {
            cv.BackgroundColor = bgColor;
        }
        
        //TODO: May want to make sure this cascades down all contentview child controls since cell templates can be utilized.
    }

    internal void UpdateCellTextColor(Color? textColor)
    {
        foreach (var child in ((IVisualTreeElement)this).GetVisualChildren())
        {
            if (child is ContentView cellContent && cellContent.Content is Label label)
            {
                label.TextColor = textColor;
            }
        }
    }
    
    private void SetupAccessibility()
    {
        SemanticProperties.SetDescription(this, $"Cell for column {Column.Title}");
    }
}