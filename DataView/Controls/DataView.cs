using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Input;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System;

namespace DataView.Controls
{
    public interface IDataProvider
    {
        int RowCount { get; }
        int ColumnCount { get; }
        object GetCell(int row, int col);
        IList<string> GetColumnHeaders();
        IList<string> GetRowHeaders();
    }

    public class DataView : Control
    {
        public static readonly StyledProperty<IEnumerable> ItemsSourceProperty =
            AvaloniaProperty.Register<DataView, IEnumerable>(nameof(ItemsSource));
        public IEnumerable ItemsSource
        {
            get => GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly StyledProperty<IList<string>> ColumnsProperty =
            AvaloniaProperty.Register<DataView, IList<string>>(nameof(Columns));
        public IList<string> Columns
        {
            get => GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        public static readonly StyledProperty<double> RowHeightProperty =
            AvaloniaProperty.Register<DataView, double>(nameof(RowHeight), 32);
        public double RowHeight
        {
            get => GetValue(RowHeightProperty);
            set => SetValue(RowHeightProperty, value);
        }

        public static readonly StyledProperty<double> ColumnWidthProperty =
            AvaloniaProperty.Register<DataView, double>(nameof(ColumnWidth), 120);
        public double ColumnWidth
        {
            get => GetValue(ColumnWidthProperty);
            set => SetValue(ColumnWidthProperty, value);
        }

        private double _verticalOffset;
        private double _horizontalOffset;
        private bool _isDraggingVertical;
        private bool _isDraggingHorizontal;
        private Point _dragStartPoint;
        private double _dragStartOffset;

        public static readonly StyledProperty<IList<string>> RowHeadersProperty =
            AvaloniaProperty.Register<DataView, IList<string>>(nameof(RowHeaders));
        public IList<string> RowHeaders
        {
            get => GetValue(RowHeadersProperty);
            set => SetValue(RowHeadersProperty, value);
        }

        public override void Render(DrawingContext context)
        {
            IList<string> columns = Columns ?? new List<string>();
            IList<string> rowHeaders = RowHeaders ?? new List<string>();
            int totalRows = 0;
            int totalCols = columns.Count;
            double[,] array = null;
            double rowHeaderWidth = 80;
            IDataProvider provider = ItemsSource as IDataProvider;
            if (provider != null)
            {
                totalRows = provider.RowCount;
                totalCols = provider.ColumnCount;
                columns = provider.GetColumnHeaders() ?? columns;
                rowHeaders = provider.GetRowHeaders() ?? rowHeaders;
            }
            else if (ItemsSource is double[,])
            {
                array = (double[,])ItemsSource;
                totalRows = array.GetLength(0);
                totalCols = array.GetLength(1);
            }
            else
            {
                var items = ItemsSource?.Cast<object>().ToList() ?? new List<object>();
                totalRows = items.Count;
            }
            var viewport = Bounds;
            double scrollbarThickness = 16;
            double gridWidth = totalCols * ColumnWidth + rowHeaderWidth;
            double gridHeight = (totalRows + 1) * RowHeight;

            // 网格区域划分
            double dataAreaWidth = viewport.Width - (gridHeight > viewport.Height ? scrollbarThickness : 0);
            double dataAreaHeight = viewport.Height - (gridWidth > viewport.Width ? scrollbarThickness : 0);
            double vScrollLeft = dataAreaWidth;
            double hScrollTop = dataAreaHeight;

            // 数据区 (0,0)
            Rect dataArea = new Rect(0, 0, viewport.Width - (gridHeight > viewport.Height ? scrollbarThickness : 0), viewport.Height - (gridWidth > viewport.Width ? scrollbarThickness : 0));
            int firstRow = (int)(_verticalOffset / RowHeight);
            int visibleRows = (int)(dataArea.Height / RowHeight) + 2;
            int lastRow = Math.Min(totalRows, firstRow + visibleRows);
            int firstCol = (int)(_horizontalOffset / ColumnWidth);
            int visibleCols = (int)((dataArea.Width - rowHeaderWidth) / ColumnWidth) + 2;
            int lastCol = Math.Min(totalCols, firstCol + visibleCols);

            // 左上角单元格
            var topLeftRect = new Rect(0, 0, rowHeaderWidth, RowHeight);
            context.FillRectangle(Brushes.LightGray, topLeftRect);
            context.DrawLine(new Pen(Brushes.Gray, 0.25), topLeftRect.TopRight, topLeftRect.BottomRight);
            context.DrawLine(new Pen(Brushes.Gray, 0.25), topLeftRect.BottomLeft, topLeftRect.BottomRight);
            // 表头
            for (int c = firstCol; c < lastCol; c++)
            {
                var rect = new Rect(
                    rowHeaderWidth + c * ColumnWidth - _horizontalOffset,
                    0,
                    ColumnWidth,
                    RowHeight);
                if (rect.Right > dataArea.Right) break;
                context.FillRectangle(Brushes.LightGray, rect);
                context.DrawLine(new Pen(Brushes.Gray, 0.25), rect.TopRight, rect.BottomRight);
                context.DrawLine(new Pen(Brushes.Gray, 0.25), rect.BottomLeft, rect.BottomRight);
                var text = columns.Count > c ? columns[c] : "";
                var formatted = new FormattedText(
                    text,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    16,
                    Brushes.Black);
                context.DrawText(formatted, rect.TopLeft + new Point(8, 8));
            }
            // 数据区
            if (provider != null)
            {
                for (int r = firstRow; r < lastRow; r++)
                {
                    if (r >= provider.RowCount) break;
                    var rowRect = new Rect(0, (r - firstRow + 1) * RowHeight, rowHeaderWidth, RowHeight);
                    if (rowRect.Bottom > dataArea.Bottom) break;
                    context.FillRectangle(r % 2 == 0 ? Brushes.LightGray : Brushes.Gainsboro, rowRect);
                    context.DrawLine(new Pen(Brushes.Gray, 0.25), rowRect.TopRight, rowRect.BottomRight);
                    context.DrawLine(new Pen(Brushes.Gray, 0.25), rowRect.BottomLeft, rowRect.BottomRight);
                    var rowText = rowHeaders.Count > r ? rowHeaders[r] : $"{r + 1}";
                    var formattedRow = new FormattedText(rowText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 14, Brushes.Black);
                    context.DrawText(formattedRow, rowRect.TopLeft + new Point(8, 8));
                    for (int c = firstCol; c < lastCol; c++)
                    {
                        if (c >= provider.ColumnCount) break;
                        var rect = new Rect(rowHeaderWidth + c * ColumnWidth - _horizontalOffset, (r - firstRow + 1) * RowHeight, ColumnWidth, RowHeight);
                        if (rect.Right > dataArea.Right) break;
                        context.FillRectangle(r % 2 == 0 ? Brushes.White : Brushes.Beige, rect);
                        context.DrawLine(new Pen(Brushes.Gray, 0.25), rect.TopRight, rect.BottomRight);
                        context.DrawLine(new Pen(Brushes.Gray, 0.25), rect.BottomLeft, rect.BottomRight);
                        var value = provider.GetCell(r, c)?.ToString() ?? "";
                        var formatted = new FormattedText(value, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 14, Brushes.Black);
                        context.DrawText(formatted, rect.TopLeft + new Point(8, 8));
                    }
                }
            }
            else if (array != null)
            {
                for (int r = firstRow; r < lastRow; r++)
                {
                    if (r >= array.GetLength(0)) break;
                    var rowRect = new Rect(0, (r - firstRow + 1) * RowHeight, rowHeaderWidth, RowHeight);
                    if (rowRect.Bottom > dataArea.Bottom) break;
                    context.FillRectangle(r % 2 == 0 ? Brushes.LightGray : Brushes.Gainsboro, rowRect);
                    context.DrawLine(new Pen(Brushes.Gray, 0.25), rowRect.TopRight, rowRect.BottomRight);
                    context.DrawLine(new Pen(Brushes.Gray, 0.25), rowRect.BottomLeft, rowRect.BottomRight);
                    var rowText = rowHeaders.Count > r ? rowHeaders[r] : $"{r + 1}";
                    var formattedRow = new FormattedText(rowText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 14, Brushes.Black);
                    context.DrawText(formattedRow, rowRect.TopLeft + new Point(8, 8));
                    for (int c = firstCol; c < lastCol; c++)
                    {
                        if (c >= array.GetLength(1)) break;
                        var rect = new Rect(
                            rowHeaderWidth + c * ColumnWidth - _horizontalOffset,
                            (r - firstRow + 1) * RowHeight,
                            ColumnWidth,
                            RowHeight);
                        if (rect.Right > dataArea.Right) break;
                        context.FillRectangle(r % 2 == 0 ? Brushes.White : Brushes.Beige, rect);
                        context.DrawLine(new Pen(Brushes.Gray, 0.25), rect.TopRight, rect.BottomRight);
                        context.DrawLine(new Pen(Brushes.Gray, 0.25), rect.BottomLeft, rect.BottomRight);
                        var value = array[r, c].ToString("F4", CultureInfo.InvariantCulture);
                        var formatted = new FormattedText(
                            value,
                            CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            new Typeface("Segoe UI"),
                            14,
                            Brushes.Black);
                        context.DrawText(formatted, rect.TopLeft + new Point(8, 8));
                    }
                }
            }
            else
            {
                var items = ItemsSource?.Cast<object>().ToList() ?? new List<object>();
                for (int r = firstRow; r < lastRow; r++)
                {
                    if (r >= items.Count) break;
                    var item = items[r];
                    var rowRect = new Rect(0, (r - firstRow + 1) * RowHeight, rowHeaderWidth, RowHeight);
                    if (rowRect.Bottom > dataArea.Bottom) break;
                    context.FillRectangle(r % 2 == 0 ? Brushes.LightGray : Brushes.Gainsboro, rowRect);
                    context.DrawLine(new Pen(Brushes.Gray, 0.25), rowRect.TopRight, rowRect.BottomRight);
                    context.DrawLine(new Pen(Brushes.Gray, 0.25), rowRect.BottomLeft, rowRect.BottomRight);
                    var rowText = rowHeaders.Count > r ? rowHeaders[r] : $"{r + 1}";
                    var formattedRow = new FormattedText(rowText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 14, Brushes.Black);
                    context.DrawText(formattedRow, rowRect.TopLeft + new Point(8, 8));
                    for (int c = firstCol; c < lastCol; c++)
                    {
                        var rect = new Rect(
                            rowHeaderWidth + c * ColumnWidth - _horizontalOffset,
                            (r - firstRow + 1) * RowHeight,
                            ColumnWidth,
                            RowHeight);
                        if (rect.Right > dataArea.Right) break;
                        context.FillRectangle(r % 2 == 0 ? Brushes.White : Brushes.Beige, rect);
                        context.DrawLine(new Pen(Brushes.Gray, 0.25), rect.TopRight, rect.BottomRight);
                        context.DrawLine(new Pen(Brushes.Gray, 0.25), rect.BottomLeft, rect.BottomRight);
                        var prop = item.GetType().GetProperty(columns.Count > c ? columns[c] : "");
                        var value = prop?.GetValue(item)?.ToString() ?? "";
                        var formatted = new FormattedText(
                            value,
                            CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            new Typeface("Segoe UI"),
                            14,
                            Brushes.Black);
                        context.DrawText(formatted, rect.TopLeft + new Point(8, 8));
                    }
                }
            }

            // 纵向滚动条 (0,1)
            if (gridHeight > viewport.Height)
            {
                var vScrollRect = new Rect(vScrollLeft, 0, scrollbarThickness, dataAreaHeight);
                context.FillRectangle(Brushes.LightGray, vScrollRect);
                // 滚动条 thumb
                double trackHeight = dataAreaHeight;
                double thumbHeight = trackHeight * (dataAreaHeight / gridHeight);
                thumbHeight = Math.Max(20, thumbHeight);
                double maxOffset = gridHeight - dataAreaHeight;
                double thumbTop = (maxOffset > 0) ? (trackHeight * (_verticalOffset / maxOffset)) : 0;
                thumbTop = Math.Min(thumbTop, trackHeight - thumbHeight);
                var thumbRect = new Rect(vScrollLeft, thumbTop, scrollbarThickness, thumbHeight);
                context.FillRectangle(Brushes.Gray, thumbRect);
            }
            // 横向滚动条 (1,0)
            if (gridWidth > viewport.Width)
            {
                var hScrollRect = new Rect(0, hScrollTop, dataAreaWidth, scrollbarThickness);
                context.FillRectangle(Brushes.LightGray, hScrollRect);
                // 滚动条 thumb
                double trackWidth = dataAreaWidth;
                double thumbWidth = trackWidth * (dataAreaWidth / gridWidth);
                thumbWidth = Math.Max(20, thumbWidth);
                double maxOffset = gridWidth - dataAreaWidth;
                double thumbLeft = (maxOffset > 0) ? (trackWidth * (_horizontalOffset / maxOffset)) : 0;
                thumbLeft = Math.Min(thumbLeft, trackWidth - thumbWidth);
                var thumbRect = new Rect(thumbLeft, hScrollTop, thumbWidth, scrollbarThickness);
                context.FillRectangle(Brushes.Gray, thumbRect);
            }
            // 右下角填充 (1,1)
            if (gridWidth > viewport.Width && gridHeight > viewport.Height)
            {
                var cornerRect = new Rect(vScrollLeft, hScrollTop, scrollbarThickness, scrollbarThickness);
                context.FillRectangle(Brushes.LightGray, cornerRect);
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            var pt = e.GetPosition(this);
            var viewport = Bounds;
            double scrollbarThickness = 16;
            double gridWidth = (Columns?.Count ?? 0) * ColumnWidth;
            double gridHeight = ((ItemsSource is double[,] arr) ? arr.GetLength(0) : 0) * RowHeight + RowHeight;
            // 水平滚动条
            if (gridWidth > viewport.Width)
            {
                double trackWidth = viewport.Width - scrollbarThickness;
                double thumbWidth = trackWidth * (viewport.Width / gridWidth);
                thumbWidth = Math.Max(20, thumbWidth); // 最小宽度20
                double maxOffset = gridWidth - viewport.Width;
                double thumbLeft = (maxOffset > 0) ? (trackWidth * (_horizontalOffset / maxOffset)) : 0;
                thumbLeft = Math.Min(thumbLeft, trackWidth - thumbWidth);
                var thumbRect = new Rect(thumbLeft, viewport.Height - scrollbarThickness, thumbWidth, scrollbarThickness - 2);
                if (thumbRect.Contains(pt))
                {
                    _isDraggingHorizontal = true;
                    _dragStartPoint = pt;
                    _dragStartOffset = _horizontalOffset;
                    e.Handled = true;
                    return;
                }
            }
            // 垂直滚动条
            if (gridHeight > viewport.Height)
            {
                double trackHeight = viewport.Height - scrollbarThickness;
                double thumbHeight = trackHeight * (viewport.Height / gridHeight);
                thumbHeight = Math.Max(20, thumbHeight); // 最小高度20
                double maxOffset = gridHeight - viewport.Height;
                double thumbTop = (maxOffset > 0) ? (trackHeight * (_verticalOffset / maxOffset)) : 0;
                thumbTop = Math.Min(thumbTop, trackHeight - thumbHeight);
                var thumbRect = new Rect(viewport.Width - scrollbarThickness, thumbTop, scrollbarThickness - 2, thumbHeight);
                if (thumbRect.Contains(pt))
                {
                    _isDraggingVertical = true;
                    _dragStartPoint = pt;
                    _dragStartOffset = _verticalOffset;
                    e.Handled = true;
                    return;
                }
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            _isDraggingHorizontal = false;
            _isDraggingVertical = false;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            var pt = e.GetPosition(this);
            var viewport = Bounds;
            double scrollbarThickness = 16;
            double gridWidth = (Columns?.Count ?? 0) * ColumnWidth;
            double gridHeight = ((ItemsSource is double[,] arr) ? arr.GetLength(0) : 0) * RowHeight + RowHeight;
            if (_isDraggingHorizontal && gridWidth > viewport.Width)
            {
                double trackWidth = viewport.Width - scrollbarThickness;
                double maxOffset = gridWidth - viewport.Width;
                double dx = pt.X - _dragStartPoint.X;
                double thumbMoveRatio = dx / trackWidth;
                _horizontalOffset = Math.Max(0, Math.Min(maxOffset, _dragStartOffset + thumbMoveRatio * maxOffset));
                InvalidateVisual();
                e.Handled = true;
            }
            if (_isDraggingVertical && gridHeight > viewport.Height)
            {
                double trackHeight = viewport.Height - scrollbarThickness;
                double maxOffset = gridHeight - viewport.Height;
                double dy = pt.Y - _dragStartPoint.Y;
                double thumbMoveRatio = dy / trackHeight;
                _verticalOffset = Math.Max(0, Math.Min(maxOffset, _dragStartOffset + thumbMoveRatio * maxOffset));
                InvalidateVisual();
                e.Handled = true;
            }
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            _verticalOffset = System.Math.Max(0, _verticalOffset - e.Delta.Y * RowHeight);
            InvalidateVisual();
            e.Handled = true;
        }
    }
}
