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
    /// <summary>
    /// 定义数据提供者接口，支持行列访问和表头获取。
    /// </summary>
    public interface IDataProvider : IEnumerable
    {
        /// <summary>
        /// 获取数据的总行数。
        /// </summary>
        int RowCount { get; }

        /// <summary>
        /// 获取数据的总列数。
        /// </summary>
        int ColumnCount { get; }

        /// <summary>
        /// 获取指定单元格的数据。
        /// </summary>
        /// <param name="row">行索引。</param>
        /// <param name="col">列索引。</param>
        /// <returns>单元格对象。</returns>
        object GetCell(int row, int col);

        /// <summary>
        /// 获取所有列头名称列表。
        /// </summary>
        /// <returns>列头字符串列表。</returns>
        IList<string> GetColumnHeaders();

        /// <summary>
        /// 获取所有行头名称列表。
        /// </summary>
        /// <returns>行头字符串列表。</returns>
        IList<string> GetRowHeaders();
    }

    /// <summary>
    /// 数据视图控件，支持二维表格渲染、滚动、表头与行头显示。
    /// </summary>
    public class DataView : Control
    {
        /// <summary>
        /// 绑定的数据源属性，支持 IEnumerable 或 IDataProvider。
        /// </summary>
        public static readonly StyledProperty<IEnumerable> ItemsSourceProperty =
            AvaloniaProperty.Register<DataView, IEnumerable>(nameof(ItemsSource));

        /// <summary>
        /// 获取或设置数据源。
        /// </summary>
        public IEnumerable ItemsSource
        {
            get => GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        /// <summary>
        /// 列头集合属性。
        /// </summary>
        public static readonly StyledProperty<IList<string>> ColumnsProperty =
            AvaloniaProperty.Register<DataView, IList<string>>(nameof(Columns));

        /// <summary>
        /// 获取或设置列头集合。
        /// </summary>
        public IList<string> Columns
        {
            get => GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        /// <summary>
        /// 行高属性，默认值为 32。
        /// </summary>
        public static readonly StyledProperty<double> RowHeightProperty =
            AvaloniaProperty.Register<DataView, double>(nameof(RowHeight), 32);

        /// <summary>
        /// 获取或设置每行高度。
        /// </summary>
        public double RowHeight
        {
            get => GetValue(RowHeightProperty);
            set => SetValue(RowHeightProperty, value);
        }

        /// <summary>
        /// 列宽属性，默认值为 120。
        /// </summary>
        public static readonly StyledProperty<double> ColumnWidthProperty =
            AvaloniaProperty.Register<DataView, double>(nameof(ColumnWidth), 120);

        /// <summary>
        /// 获取或设置每列宽度。
        /// </summary>
        public double ColumnWidth
        {
            get => GetValue(ColumnWidthProperty);
            set => SetValue(ColumnWidthProperty, value);
        }

        /// <summary>
        /// 当前垂直滚动偏移量。
        /// </summary>
        private double _verticalOffset;

        /// <summary>
        /// 当前水平滚动偏移量。
        /// </summary>
        private double _horizontalOffset;

        /// <summary>
        /// 是否正在拖动纵向滚动条。
        /// </summary>
        private bool _isDraggingVertical;

        /// <summary>
        /// 是否正在拖动横向滚动条。
        /// </summary>
        private bool _isDraggingHorizontal;

        /// <summary>
        /// 拖动起始点坐标。
        /// </summary>
        private Point _dragStartPoint;

        /// <summary>
        /// 拖动起始偏移量。
        /// </summary>
        private double _dragStartOffset;

        /// <summary>
        /// 行头集合属性。
        /// </summary>
        public static readonly StyledProperty<IList<string>> RowHeadersProperty =
            AvaloniaProperty.Register<DataView, IList<string>>(nameof(RowHeaders));

        /// <summary>
        /// 获取或设置行头集合。
        /// </summary>
        public IList<string> RowHeaders
        {
            get => GetValue(RowHeadersProperty);
            set => SetValue(RowHeadersProperty, value);
        }

        /// <summary>
        /// 数字类型显示的小数位数属性，默认4位。
        /// </summary>
        public static readonly StyledProperty<int> DecimalPlacesProperty =
            AvaloniaProperty.Register<DataView, int>(nameof(DecimalPlaces), 4);

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == DecimalPlacesProperty)
            {
                InvalidateVisual();
            }
        }

        /// <summary>
        /// 获取或设置数字类型显示的小数位数。
        /// </summary>
        public int DecimalPlaces
        {
            get => GetValue(DecimalPlacesProperty);
            set => SetValue(DecimalPlacesProperty, value);
        }

        /// <summary>
        /// 渲染控件内容，包括表格、表头、行头、滚动条等。
        /// </summary>
        /// <param name="context">绘制上下文。</param>
        public override void Render(DrawingContext context)
        {
            // 获取列头和行头集合，若未设置则使用空列表
            IList<string> columns = Columns ?? new List<string>();
            IList<string> rowHeaders = RowHeaders ?? new List<string>();
            int totalRows = 0; // 总行数
            int totalCols = columns.Count; // 总列数
            double[,] array = null; // 用于存储二维数组数据源
            double rowHeaderWidth = 80; // 行头宽度
            // 尝试将 ItemsSource 转换为 IDataProvider
            IDataProvider provider = ItemsSource as IDataProvider;
            if (provider != null)
            {
                // 使用 IDataProvider 提供的行列数和表头
                totalRows = provider.RowCount;
                totalCols = provider.ColumnCount;
                columns = provider.GetColumnHeaders() ?? columns;
                rowHeaders = provider.GetRowHeaders() ?? rowHeaders;
            }
            else if (ItemsSource is double[,])
            {
                // 如果数据源为二维数组，获取其行列数
                array = (double[,])ItemsSource;
                totalRows = array.GetLength(0);
                totalCols = array.GetLength(1);
            }
            else
            {
                // 其他 IEnumerable 类型，按元素数量计行数
                var items = ItemsSource?.Cast<object>().ToList() ?? new List<object>();
                totalRows = items.Count;
            }
            // 获取控件可视区域
            var viewport = Bounds;
            double scrollbarThickness = 16; // 滚动条厚度
            double gridWidth = totalCols * ColumnWidth + rowHeaderWidth; // 网格总宽度
            double gridHeight = (totalRows + 1) * RowHeight; // 网格总高度（含表头）

            // 计算数据区宽高，考虑滚动条占用
            double dataAreaWidth = viewport.Width - (gridHeight > viewport.Height ? scrollbarThickness : 0);
            double dataAreaHeight = viewport.Height - (gridWidth > viewport.Width ? scrollbarThickness : 0);
            double vScrollLeft = dataAreaWidth; // 纵向滚动条左侧位置
            double hScrollTop = dataAreaHeight; // 横向滚动条顶部位置

            // 数据区矩形 (0,0)
            Rect dataArea = new Rect(0, 0, viewport.Width - (gridHeight > viewport.Height ? scrollbarThickness : 0), viewport.Height - (gridWidth > viewport.Width ? scrollbarThickness : 0));
            // 计算首行、可见行数、末行索引
            int firstRow = (int)(_verticalOffset / RowHeight);
            int visibleRows = (int)(dataArea.Height / RowHeight) + 2;
            int lastRow = Math.Min(totalRows, firstRow + visibleRows);
            // 计算首列、可见列数、末列索引
            int firstCol = (int)(_horizontalOffset / ColumnWidth);
            int visibleCols = (int)((dataArea.Width - rowHeaderWidth) / ColumnWidth) + 2;
            int lastCol = Math.Min(totalCols, firstCol + visibleCols);

            // 绘制左上角单元格
            var topLeftRect = new Rect(0, 0, rowHeaderWidth, RowHeight);
            context.FillRectangle(Brushes.LightGray, topLeftRect);
            context.DrawLine(new Pen(Brushes.Gray, 0.25), topLeftRect.TopRight, topLeftRect.BottomRight);
            context.DrawLine(new Pen(Brushes.Gray, 0.25), topLeftRect.BottomLeft, topLeftRect.BottomRight);

            // 绘制表头
            for (int c = firstCol; c < lastCol; c++)
            {
                // 计算当前列表头矩形区域
                var rect = new Rect(
                    rowHeaderWidth + c * ColumnWidth - _horizontalOffset,
                    0,
                    ColumnWidth,
                    RowHeight);
                // 超出数据区则跳出循环
                if (rect.Right > dataArea.Right) break;
                context.FillRectangle(Brushes.LightGray, rect);
                context.DrawLine(new Pen(Brushes.Gray, 0.25), rect.TopRight, rect.BottomRight);
                context.DrawLine(new Pen(Brushes.Gray, 0.25), rect.BottomLeft, rect.BottomRight);
                // 获取列头文本
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

            // 绘制数据区
            if (provider != null)
            {
                // 使用 IDataProvider 渲染数据
                for (int r = firstRow; r < lastRow; r++)
                {
                    // 超出数据源行数则跳出
                    if (r >= provider.RowCount) break;
                    // 绘制行头区域
                    var rowRect = new Rect(0, (r - firstRow + 1) * RowHeight, rowHeaderWidth, RowHeight);
                    if (rowRect.Bottom > dataArea.Bottom) break;
                    context.FillRectangle(r % 2 == 0 ? Brushes.LightGray : Brushes.Gainsboro, rowRect);
                    context.DrawLine(new Pen(Brushes.Gray, 0.25), rowRect.TopRight, rowRect.BottomRight);
                    context.DrawLine(new Pen(Brushes.Gray, 0.25), rowRect.BottomLeft, rowRect.BottomRight);
                    // 获取行头文本
                    var rowText = rowHeaders.Count > r ? rowHeaders[r] : $"{r + 1}";
                    var formattedRow = new FormattedText(rowText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 14, Brushes.Black);
                    context.DrawText(formattedRow, rowRect.TopLeft + new Point(8, 8));
                    // 绘制每一列数据单元格
                    for (int c = firstCol; c < lastCol; c++)
                    {
                        if (c >= provider.ColumnCount) break;
                        var rect = new Rect(rowHeaderWidth + c * ColumnWidth - _horizontalOffset, (r - firstRow + 1) * RowHeight, ColumnWidth, RowHeight);
                        if (rect.Right > dataArea.Right) break;
                        context.FillRectangle(r % 2 == 0 ? Brushes.White : Brushes.Beige, rect);
                        context.DrawLine(new Pen(Brushes.Gray, 0.25), rect.TopRight, rect.BottomRight);
                        context.DrawLine(new Pen(Brushes.Gray, 0.25), rect.BottomLeft, rect.BottomRight);
                        // 获取单元格数据并格式化
                        var cellObj = provider.GetCell(r, c);
                        string value;
                        if (cellObj is double d)
                            value = d.ToString($"F{DecimalPlaces}", CultureInfo.InvariantCulture);
                        else if (cellObj is float f)
                            value = f.ToString($"F{DecimalPlaces}", CultureInfo.InvariantCulture);
                        else
                            value = cellObj?.ToString() ?? "";
                        var formatted = new FormattedText(value, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 14, Brushes.Black);
                        context.DrawText(formatted, rect.TopLeft + new Point(8, 8));
                    }
                }
            }
            else if (array != null)
            {
                // 使用二维数组渲染数据
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
                        // 格式化单元格数值为指定小数位
                        var value = array[r, c].ToString($"F{DecimalPlaces}", CultureInfo.InvariantCulture);
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
                // 使用 IEnumerable 渲染数据
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
                        // 通过反射获取属性值
                        var prop = item.GetType().GetProperty(columns.Count > c ? columns[c] : "");
                        var propValue = prop?.GetValue(item);
                        string value;
                        if (propValue is double d)
                            value = d.ToString($"F{DecimalPlaces}", CultureInfo.InvariantCulture);
                        else if (propValue is float f)
                            value = f.ToString($"F{DecimalPlaces}", CultureInfo.InvariantCulture);
                        else
                            value = propValue?.ToString() ?? "";
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

            // 绘制纵向滚动条 (0,1)
            if (gridHeight > viewport.Height)
            {
                var vScrollRect = new Rect(vScrollLeft, 0, scrollbarThickness, dataAreaHeight);
                context.FillRectangle(Brushes.LightGray, vScrollRect);
                // 计算纵向滚动条 thumb 区域
                double trackHeight = dataAreaHeight;
                double thumbHeight = trackHeight * (dataAreaHeight / gridHeight);
                thumbHeight = Math.Max(20, thumbHeight);
                double maxOffset = gridHeight - dataAreaHeight;
                double thumbTop = (maxOffset > 0) ? (trackHeight * (_verticalOffset / maxOffset)) : 0;
                thumbTop = Math.Min(thumbTop, trackHeight - thumbHeight);
                var thumbRect = new Rect(vScrollLeft, thumbTop, scrollbarThickness, thumbHeight);
                context.FillRectangle(Brushes.Gray, thumbRect);
            }
            // 绘制横向滚动条 (1,0)
            if (gridWidth > viewport.Width)
            {
                var hScrollRect = new Rect(0, hScrollTop, dataAreaWidth, scrollbarThickness);
                context.FillRectangle(Brushes.LightGray, hScrollRect);
                // 计算横向滚动条 thumb 区域
                double trackWidth = dataAreaWidth;
                double thumbWidth = trackWidth * (dataAreaWidth / gridWidth);
                thumbWidth = Math.Max(20, thumbWidth);
                double maxOffset = gridWidth - dataAreaWidth;
                double thumbLeft = (maxOffset > 0) ? (trackWidth * (_horizontalOffset / maxOffset)) : 0;
                thumbLeft = Math.Min(thumbLeft, trackWidth - thumbWidth);
                var thumbRect = new Rect(thumbLeft, hScrollTop, thumbWidth, scrollbarThickness);
                context.FillRectangle(Brushes.Gray, thumbRect);
            }
            // 绘制右下角填充区域 (1,1)
            if (gridWidth > viewport.Width && gridHeight > viewport.Height)
            {
                var cornerRect = new Rect(vScrollLeft, hScrollTop, scrollbarThickness, scrollbarThickness);
                context.FillRectangle(Brushes.LightGray, cornerRect);
            }
        }

        /// <summary>
        /// 处理鼠标按下事件，实现滚动条拖动功能。
        /// </summary>
        /// <param name="e">鼠标按下事件参数。</param>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            // 获取鼠标点击位置
            var pt = e.GetPosition(this);
            var viewport = Bounds;
            double scrollbarThickness = 16;
            IList<string> columns = Columns ?? new List<string>();
            IList<string> rowHeaders = RowHeaders ?? new List<string>();
            int totalRows = 0;
            int totalCols = columns.Count;
            double[,] array = null;
            IDataProvider provider = ItemsSource as IDataProvider;
            if (provider != null)
            {
                // 使用 IDataProvider 获取行列数
                totalRows = provider.RowCount;
                totalCols = provider.ColumnCount;
            }
            else if (ItemsSource is double[,])
            {
                // 使用二维数组获取行列数
                array = (double[,])ItemsSource;
                totalRows = array.GetLength(0);
                totalCols = array.GetLength(1);
            }
            else
            {
                // 使用 IEnumerable 获取行数
                var items = ItemsSource?.Cast<object>().ToList() ?? new List<object>();
                totalRows = items.Count;
            }
            double rowHeaderWidth = 80;
            double gridWidth = totalCols * ColumnWidth + rowHeaderWidth;
            double gridHeight = (totalRows + 1) * RowHeight;
            double dataAreaWidth = viewport.Width - (gridHeight > viewport.Height ? scrollbarThickness : 0);
            double dataAreaHeight = viewport.Height - (gridWidth > viewport.Width ? scrollbarThickness : 0);
            double vScrollLeft = dataAreaWidth;
            double hScrollTop = dataAreaHeight;
            // 检查是否点击横向滚动条 thumb
            if (gridWidth > viewport.Width)
            {
                double trackWidth = dataAreaWidth;
                double thumbWidth = trackWidth * (dataAreaWidth / gridWidth);
                thumbWidth = Math.Max(20, thumbWidth);
                double maxOffset = gridWidth - dataAreaWidth;
                double thumbLeft = (maxOffset > 0) ? (trackWidth * (_horizontalOffset / maxOffset)) : 0;
                thumbLeft = Math.Min(thumbLeft, trackWidth - thumbWidth);
                var thumbRect = new Rect(thumbLeft, hScrollTop, thumbWidth, scrollbarThickness);
                if (thumbRect.Contains(pt))
                {
                    // 开始拖动横向滚动条
                    _isDraggingHorizontal = true;
                    _dragStartPoint = pt;
                    _dragStartOffset = _horizontalOffset;
                    e.Handled = true;
                    return;
                }
            }
            // 检查是否点击纵向滚动条 thumb
            if (gridHeight > viewport.Height)
            {
                double trackHeight = dataAreaHeight;
                double thumbHeight = trackHeight * (dataAreaHeight / gridHeight);
                thumbHeight = Math.Max(20, thumbHeight);
                double maxOffset = gridHeight - dataAreaHeight;
                double thumbTop = (maxOffset > 0) ? (trackHeight * (_verticalOffset / maxOffset)) : 0;
                thumbTop = Math.Min(thumbTop, trackHeight - thumbHeight);
                var thumbRect = new Rect(vScrollLeft, thumbTop, scrollbarThickness, thumbHeight);
                if (thumbRect.Contains(pt))
                {
                    // 开始拖动纵向滚动条
                    _isDraggingVertical = true;
                    _dragStartPoint = pt;
                    _dragStartOffset = _verticalOffset;
                    e.Handled = true;
                    return;
                }
            }
        }

        /// <summary>
        /// 处理鼠标释放事件，结束滚动条拖动。
        /// </summary>
        /// <param name="e">鼠标释放事件参数。</param>
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            // 结束横向和纵向滚动条拖动
            _isDraggingHorizontal = false;
            _isDraggingVertical = false;
        }

        /// <summary>
        /// 处理鼠标移动事件，实现滚动条拖动时的偏移量更新。
        /// </summary>
        /// <param name="e">鼠标移动事件参数。</param>
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            // 获取鼠标当前位置
            var pt = e.GetPosition(this);
            var viewport = Bounds;
            double scrollbarThickness = 16;
            IList<string> columns = Columns ?? new List<string>();
            int totalCols = columns.Count;
            int totalRows = 0;
            double[,] array = null;
            IDataProvider provider = ItemsSource as IDataProvider;
            if (provider != null)
            {
                // 使用 IDataProvider 获取行列数
                totalRows = provider.RowCount;
                totalCols = provider.ColumnCount;
            }
            else if (ItemsSource is double[,])
            {
                // 使用二维数组获取行列数
                array = (double[,])ItemsSource;
                totalRows = array.GetLength(0);
                totalCols = array.GetLength(1);
            }
            else
            {
                // 使用 IEnumerable 获取行数
                var items = ItemsSource?.Cast<object>().ToList() ?? new List<object>();
                totalRows = items.Count;
            }
            double rowHeaderWidth = 80;
            double gridWidth = totalCols * ColumnWidth + rowHeaderWidth;
            double gridHeight = (totalRows + 1) * RowHeight;
            double dataAreaWidth = viewport.Width - (gridHeight > viewport.Height ? scrollbarThickness : 0);
            double dataAreaHeight = viewport.Height - (gridWidth > viewport.Width ? scrollbarThickness : 0);
            // 处理横向滚动条拖动
            if (_isDraggingHorizontal && gridWidth > viewport.Width)
            {
                double trackWidth = dataAreaWidth;
                double maxOffset = gridWidth - dataAreaWidth;
                double dx = pt.X - _dragStartPoint.X;
                double thumbMoveRatio = dx / trackWidth;
                // 更新水平偏移量，限制在有效范围
                _horizontalOffset = Math.Max(0, Math.Min(maxOffset, _dragStartOffset + thumbMoveRatio * maxOffset));
                InvalidateVisual();
                e.Handled = true;
            }
            // 处理纵向滚动条拖动
            if (_isDraggingVertical && gridHeight > viewport.Height)
            {
                double trackHeight = dataAreaHeight;
                double maxOffset = gridHeight - dataAreaHeight;
                double dy = pt.Y - _dragStartPoint.Y;
                double thumbMoveRatio = dy / trackHeight;
                // 更新垂直偏移量，限制在有效范围
                _verticalOffset = Math.Max(0, Math.Min(maxOffset, _dragStartOffset + thumbMoveRatio * maxOffset));
                InvalidateVisual();
                e.Handled = true;
            }
        }

        /// <summary>
        /// 处理鼠标滚轮事件，实现垂直滚动。
        /// </summary>
        /// <param name="e">鼠标滚轮事件参数。</param>
        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            // 根据滚轮方向调整垂直偏移量
            _verticalOffset = System.Math.Max(0, _verticalOffset - e.Delta.Y * RowHeight);
            InvalidateVisual();
            e.Handled = true;
        }
    }
}