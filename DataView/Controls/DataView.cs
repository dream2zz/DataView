using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Input;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

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

        public override void Render(DrawingContext context)
        {
            var items = ItemsSource?.Cast<object>().ToList() ?? new List<object>();
            var columns = Columns ?? new List<string>();
            int totalRows = items.Count;
            int totalCols = columns.Count;
            var viewport = Bounds;
            double gridWidth = totalCols * ColumnWidth;
            double gridHeight = (totalRows + 1) * RowHeight;

            // 虚拟化：只渲染可见区域
            int firstRow = (int)(_verticalOffset / RowHeight);
            int visibleRows = (int)(viewport.Height / RowHeight) + 2;
            int lastRow = System.Math.Min(totalRows, firstRow + visibleRows);
            int firstCol = (int)(_horizontalOffset / ColumnWidth);
            int visibleCols = (int)(viewport.Width / ColumnWidth) + 2;
            int lastCol = System.Math.Min(totalCols, firstCol + visibleCols);

            // 绘制表头
            for (int c = firstCol; c < lastCol; c++)
            {
                var rect = new Rect(
                    c * ColumnWidth - _horizontalOffset,
                    0,
                    ColumnWidth,
                    RowHeight);
                context.FillRectangle(Brushes.LightGray, rect);
                context.DrawRectangle(new Pen(Brushes.Gray, 1), rect);
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

            // 绘制数据行
            for (int r = firstRow; r < lastRow; r++)
            {
                if (r >= items.Count) break;
                var item = items[r];
                for (int c = firstCol; c < lastCol; c++)
                {
                    var rect = new Rect(
                        c * ColumnWidth - _horizontalOffset,
                        (r - firstRow + 1) * RowHeight,
                        ColumnWidth,
                        RowHeight);
                    context.FillRectangle(r % 2 == 0 ? Brushes.White : Brushes.Beige, rect);
                    context.DrawRectangle(new Pen(Brushes.Gray, 1), rect);
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

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            _verticalOffset = System.Math.Max(0, _verticalOffset - e.Delta.Y * RowHeight);
            InvalidateVisual();
            e.Handled = true;
        }
    }
}
