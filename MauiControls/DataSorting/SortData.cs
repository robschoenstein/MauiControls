// Copyright © 2026 Robert Schoenstein. All rights reserved.
// Unauthorized use, reproduction, or distribution is strictly prohibited.

using System.ComponentModel;
using MauiControls.Converters;

namespace MauiControls.DataSorting;

/// <summary>
/// Creates SortData for <see cref="DataGrid"/>.
/// </summary>
/// <param name="index">The index of the column to sort on.</param>
/// <param name="order">The direction to sort.</param>
[TypeConverter(typeof(SortDataTypeConverter))]
public sealed class SortData(int index, SortDirection order)
{
    #region Properties

    /// <summary>
    /// Gets or sets sorting order for the column.
    /// </summary>
    public SortDirection Order { get; set; } = order;

    /// <summary>
    /// Gets or sets column Index to sort.
    /// </summary>
    public int Index { get; set; } = index;

    #endregion Properties

    /// <summary>
    /// Implicitly converts an integer to a SortData object.
    /// </summary>
    /// <param name="index">The column index.</param>
    /// <returns>A SortData object.</returns>
    public static implicit operator SortData(int index) => FromInt32(index);

    /// <summary>
    /// Creates a SortData object from an integer index. Negative indicies mean a descending sort.
    /// </summary>
    /// <param name="index">The column index.</param>
    /// <returns>A SortData object.</returns>
    public static SortData FromInt32(int index)
    {
        var order = index < 0 ? SortDirection.Descending : SortDirection.Ascending;

        return new(Math.Abs(index), order);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is SortData other && other.Index == Index && other.Order == Order;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Index, Order);
}