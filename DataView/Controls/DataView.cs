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
            var columns = Columns ?? new List<string>();
            var rowHeaders = RowHeaders ?? new List<string>();
            int totalRows = 0;
            int totalCols = columns.Count;
            double[,] array = null;
            double rowHeaderWidth = 80;
            if (ItemsSource is double[,])
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
            double gridWidth = totalCols * ColumnWidth + rowHeaderWidth;
            double gridHeight = (totalRows + 1) * RowHeight;

            int firstRow = (int)(_verticalOffset / RowHeight);
            int visibleRows = (int)(viewport.Height / RowHeight) + 2;
            int lastRow = System.Math.Min(totalRows, firstRow + visibleRows);
            int firstCol = (int)(_horizontalOffset / ColumnWidth);
            int visibleCols = (int)((viewport.Width - rowHeaderWidth) / ColumnWidth) + 2;
            int lastCol = System.Math.Min(totalCols, firstCol + visibleCols);

            // 绘制左上角单元格（不显示文字，底色与表头和行头一致）
            var topLeftRect = new Rect(0, 0, rowHeaderWidth, RowHeight);
            context.FillRectangle(Brushes.LightGray, topLeftRect); // 与表头和偶数行头一致
            // 只绘制右边框和下边框
            context.DrawLine(new Pen(Brushes.Gray, 0.25), topLeftRect.TopRight, topLeftRect.BottomRight); // 右
            context.DrawLine(new Pen(Brushes.Gray, 0.25), topLeftRect.BottomLeft, topLeftRect.BottomRight); // 下
            // 绘制表头（跳过左上角）
            for (int c = firstCol; c < lastCol; c++)
            {
                var rect = new Rect(
                    rowHeaderWidth + c * ColumnWidth - _horizontalOffset,
                    0,
                    ColumnWidth,
                    RowHeight);
                context.FillRectangle(Brushes.LightGray, rect);
                // 只绘制右边框和下边框
                context.DrawLine(new Pen(Brushes.Gray, 0.25), rect.TopRight, rect.BottomRight); // 右
                context.DrawLine(new Pen(Brushes.Gray, 0.25), rect.BottomLeft, rect.BottomRight); // 下
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

            // 绘制数据行和行头
            if (array != null)
            {
                for (int r = firstRow; r < lastRow; r++)
                {
                    if (r >= array.GetLength(0)) break;
                    // 行头
                    var rowRect = new Rect(0, (r - firstRow + 1) * RowHeight, rowHeaderWidth, RowHeight);
                    context.FillRectangle(r % 2 == 0 ? Brushes.LightGray : Brushes.Gainsboro, rowRect);
                    // 只绘制右边框和下边框
                    context.DrawLine(new Pen(Brushes.Gray, 0.25), rowRect.TopRight, rowRect.BottomRight); // 右
                    context.DrawLine(new Pen(Brushes.Gray, 0.25), rowRect.BottomLeft, rowRect.BottomRight); // 下
                    var rowText = rowHeaders.Count > r ? rowHeaders[r] : $"{r + 1}";
                    var formattedRow = new FormattedText(rowText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 14, Brushes.Black);
                    context.DrawText(formattedRow, rowRect.TopLeft + new Point(8, 8));
                    // 数据区
                    for (int c = firstCol; c < lastCol; c++)
                    {
                        if (c >= array.GetLength(1)) break;
                        var rect = new Rect(
                            rowHeaderWidth + c * ColumnWidth - _horizontalOffset,
                            (r - firstRow + 1) * RowHeight,
                            ColumnWidth,
                            RowHeight);
                        context.FillRectangle(r % 2 == 0 ? Brushes.White : Brushes.Beige, rect);
                        // 只绘制右边框和下边框
                        context.DrawLine(new Pen(Brushes.Gray, 0.25), rect.TopRight, rect.BottomRight); // 右
                        context.DrawLine(new Pen(Brushes.Gray, 0.25), rect.BottomLeft, rect.BottomRight); // 下
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
                    // 行头
                    var rowRect = new Rect(0, (r - firstRow + 1) * RowHeight, rowHeaderWidth, RowHeight);
                    context.FillRectangle(r % 2 == 0 ? Brushes.LightGray : Brushes.Gainsboro, rowRect);
                    // 只绘制右边框和下边框
                    context.DrawLine(new Pen(Brushes.Gray, 0.25), rowRect.TopRight, rowRect.BottomRight); // 右
                    context.DrawLine(new Pen(Brushes.Gray, 0.25), rowRect.BottomLeft, rowRect.BottomRight); // 下
                    var rowText = rowHeaders.Count > r ? rowHeaders[r] : $"{r + 1}";
                    var formattedRow = new FormattedText(rowText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 14, Brushes.Black);
                    context.DrawText(formattedRow, rowRect.TopLeft + new Point(8, 8));
                    // 数据区
                    for (int c = firstCol; c < lastCol; c++)
                    {
                        var rect = new Rect(
                            rowHeaderWidth + c * ColumnWidth - _horizontalOffset,
                            (r - firstRow + 1) * RowHeight,
                            ColumnWidth,
                            RowHeight);
                        context.FillRectangle(r % 2 == 0 ? Brushes.White : Brushes.Beige, rect);
                        // 只绘制右边框和下边框
                        context.DrawLine(new Pen(Brushes.Gray, 0.25), rect.TopRight, rect.BottomRight); // 右
                        context.DrawLine(new Pen(Brushes.Gray, 0.25), rect.BottomLeft, rect.BottomRight); // 下
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

            // 滚动条参数
            double scrollbarThickness = 16;
            double scrollbarMargin = 2;
            // 水平滚动条
            if (gridWidth > viewport.Width)
            {
                double trackWidth = viewport.Width - scrollbarThickness;
                double thumbWidth = trackWidth * (viewport.Width / gridWidth);
                thumbWidth = Math.Max(20, thumbWidth); // 最小宽度20
                double maxOffset = gridWidth - viewport.Width;
                double thumbLeft = (maxOffset > 0) ? (trackWidth * (_horizontalOffset / maxOffset)) : 0;
                // 限制thumbLeft不超出track
                thumbLeft = Math.Min(thumbLeft, trackWidth - thumbWidth);
                var trackRect = new Rect(0, viewport.Height - scrollbarThickness, trackWidth, scrollbarThickness - scrollbarMargin);
                var thumbRect = new Rect(thumbLeft, viewport.Height - scrollbarThickness, thumbWidth, scrollbarThickness - scrollbarMargin);
                context.FillRectangle(Brushes.LightGray, trackRect);
                context.FillRectangle(Brushes.Gray, thumbRect);
            }
            // 垂直滚动条
            if (gridHeight > viewport.Height)
            {
                double trackHeight = viewport.Height - scrollbarThickness;
                double thumbHeight = trackHeight * (viewport.Height / gridHeight);
                thumbHeight = Math.Max(20, thumbHeight); // 最小高度20
                double maxOffset = gridHeight - viewport.Height;
                double thumbTop = (maxOffset > 0) ? (trackHeight * (_verticalOffset / maxOffset)) : 0;
                // 限制thumbTop不超出track
                thumbTop = Math.Min(thumbTop, trackHeight - thumbHeight);
                var trackRect = new Rect(viewport.Width - scrollbarThickness, 0, scrollbarThickness - scrollbarMargin, trackHeight);
                var thumbRect = new Rect(viewport.Width - scrollbarThickness, thumbTop, scrollbarThickness - scrollbarMargin, thumbHeight);
                context.FillRectangle(Brushes.LightGray, trackRect);
                context.FillRectangle(Brushes.Gray, thumbRect);
            }
            // 修正右下角缝隙，填充遮挡区域
            if (gridWidth > viewport.Width && gridHeight > viewport.Height)
            {
                var cornerRect = new Rect(viewport.Width - scrollbarThickness, viewport.Height - scrollbarThickness, scrollbarThickness, scrollbarThickness);
                context.FillRectangle(Brushes.White, cornerRect); // 用背景色遮挡
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
