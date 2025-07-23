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
    /// ���������ṩ�߽ӿڣ�֧�����з��ʺͱ�ͷ��ȡ��
    /// </summary>
    public interface IDataProvider : IEnumerable
    {
        /// <summary>
        /// ��ȡ���ݵ���������
        /// </summary>
        int RowCount { get; }

        /// <summary>
        /// ��ȡ���ݵ���������
        /// </summary>
        int ColumnCount { get; }

        /// <summary>
        /// ��ȡָ����Ԫ������ݡ�
        /// </summary>
        /// <param name="row">��������</param>
        /// <param name="col">��������</param>
        /// <returns>��Ԫ�����</returns>
        object GetCell(int row, int col);

        /// <summary>
        /// ��ȡ������ͷ�����б�
        /// </summary>
        /// <returns>��ͷ�ַ����б�</returns>
        IList<string> GetColumnHeaders();

        /// <summary>
        /// ��ȡ������ͷ�����б�
        /// </summary>
        /// <returns>��ͷ�ַ����б�</returns>
        IList<string> GetRowHeaders();
    }

    /// <summary>
    /// ������ͼ�ؼ���֧�ֶ�ά�����Ⱦ����������ͷ����ͷ��ʾ��
    /// </summary>
    public class DataView : Control
    {
        /// <summary>
        /// �󶨵�����Դ���ԣ�֧�� IEnumerable �� IDataProvider��
        /// </summary>
        public static readonly StyledProperty<IEnumerable> ItemsSourceProperty =
            AvaloniaProperty.Register<DataView, IEnumerable>(nameof(ItemsSource));

        /// <summary>
        /// ��ȡ����������Դ��
        /// </summary>
        public IEnumerable ItemsSource
        {
            get => GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        /// <summary>
        /// ��ͷ�������ԡ�
        /// </summary>
        public static readonly StyledProperty<IList<string>> ColumnsProperty =
            AvaloniaProperty.Register<DataView, IList<string>>(nameof(Columns));

        /// <summary>
        /// ��ȡ��������ͷ���ϡ�
        /// </summary>
        public IList<string> Columns
        {
            get => GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        /// <summary>
        /// �и����ԣ�Ĭ��ֵΪ 32��
        /// </summary>
        public static readonly StyledProperty<double> RowHeightProperty =
            AvaloniaProperty.Register<DataView, double>(nameof(RowHeight), 32);

        /// <summary>
        /// ��ȡ������ÿ�и߶ȡ�
        /// </summary>
        public double RowHeight
        {
            get => GetValue(RowHeightProperty);
            set => SetValue(RowHeightProperty, value);
        }

        /// <summary>
        /// �п����ԣ�Ĭ��ֵΪ 120��
        /// </summary>
        public static readonly StyledProperty<double> ColumnWidthProperty =
            AvaloniaProperty.Register<DataView, double>(nameof(ColumnWidth), 120);

        /// <summary>
        /// ��ȡ������ÿ�п�ȡ�
        /// </summary>
        public double ColumnWidth
        {
            get => GetValue(ColumnWidthProperty);
            set => SetValue(ColumnWidthProperty, value);
        }

        /// <summary>
        /// ��ǰ��ֱ����ƫ������
        /// </summary>
        private double _verticalOffset;

        /// <summary>
        /// ��ǰˮƽ����ƫ������
        /// </summary>
        private double _horizontalOffset;

        /// <summary>
        /// �Ƿ������϶������������
        /// </summary>
        private bool _isDraggingVertical;

        /// <summary>
        /// �Ƿ������϶������������
        /// </summary>
        private bool _isDraggingHorizontal;

        /// <summary>
        /// �϶���ʼ�����ꡣ
        /// </summary>
        private Point _dragStartPoint;

        /// <summary>
        /// �϶���ʼƫ������
        /// </summary>
        private double _dragStartOffset;

        /// <summary>
        /// ��ͷ�������ԡ�
        /// </summary>
        public static readonly StyledProperty<IList<string>> RowHeadersProperty =
            AvaloniaProperty.Register<DataView, IList<string>>(nameof(RowHeaders));

        /// <summary>
        /// ��ȡ��������ͷ���ϡ�
        /// </summary>
        public IList<string> RowHeaders
        {
            get => GetValue(RowHeadersProperty);
            set => SetValue(RowHeadersProperty, value);
        }

        /// <summary>
        /// ����������ʾ��С��λ�����ԣ�Ĭ��4λ��
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
        /// ��ȡ����������������ʾ��С��λ����
        /// </summary>
        public int DecimalPlaces
        {
            get => GetValue(DecimalPlacesProperty);
            set => SetValue(DecimalPlacesProperty, value);
        }

        /// <summary>
        /// ��Ⱦ�ؼ����ݣ�������񡢱�ͷ����ͷ���������ȡ�
        /// </summary>
        /// <param name="context">���������ġ�</param>
        public override void Render(DrawingContext context)
        {
            // ��ȡ��ͷ����ͷ���ϣ���δ������ʹ�ÿ��б�
            IList<string> columns = Columns ?? new List<string>();
            IList<string> rowHeaders = RowHeaders ?? new List<string>();
            int totalRows = 0; // ������
            int totalCols = columns.Count; // ������
            double[,] array = null; // ���ڴ洢��ά��������Դ
            double rowHeaderWidth = 80; // ��ͷ���
            // ���Խ� ItemsSource ת��Ϊ IDataProvider
            IDataProvider provider = ItemsSource as IDataProvider;
            if (provider != null)
            {
                // ʹ�� IDataProvider �ṩ���������ͱ�ͷ
                totalRows = provider.RowCount;
                totalCols = provider.ColumnCount;
                columns = provider.GetColumnHeaders() ?? columns;
                rowHeaders = provider.GetRowHeaders() ?? rowHeaders;
            }
            else if (ItemsSource is double[,])
            {
                // �������ԴΪ��ά���飬��ȡ��������
                array = (double[,])ItemsSource;
                totalRows = array.GetLength(0);
                totalCols = array.GetLength(1);
            }
            else
            {
                // ���� IEnumerable ���ͣ���Ԫ������������
                var items = ItemsSource?.Cast<object>().ToList() ?? new List<object>();
                totalRows = items.Count;
            }
            // ��ȡ�ؼ���������
            var viewport = Bounds;
            double scrollbarThickness = 16; // ���������
            double gridWidth = totalCols * ColumnWidth + rowHeaderWidth; // �����ܿ��
            double gridHeight = (totalRows + 1) * RowHeight; // �����ܸ߶ȣ�����ͷ��

            // ������������ߣ����ǹ�����ռ��
            double dataAreaWidth = viewport.Width - (gridHeight > viewport.Height ? scrollbarThickness : 0);
            double dataAreaHeight = viewport.Height - (gridWidth > viewport.Width ? scrollbarThickness : 0);
            double vScrollLeft = dataAreaWidth; // ������������λ��
            double hScrollTop = dataAreaHeight; // �������������λ��

            // ���������� (0,0)
            Rect dataArea = new Rect(0, 0, viewport.Width - (gridHeight > viewport.Height ? scrollbarThickness : 0), viewport.Height - (gridWidth > viewport.Width ? scrollbarThickness : 0));
            // �������С��ɼ�������ĩ������
            int firstRow = (int)(_verticalOffset / RowHeight);
            int visibleRows = (int)(dataArea.Height / RowHeight) + 2;
            int lastRow = Math.Min(totalRows, firstRow + visibleRows);
            // �������С��ɼ�������ĩ������
            int firstCol = (int)(_horizontalOffset / ColumnWidth);
            int visibleCols = (int)((dataArea.Width - rowHeaderWidth) / ColumnWidth) + 2;
            int lastCol = Math.Min(totalCols, firstCol + visibleCols);

            // �������Ͻǵ�Ԫ��
            var topLeftRect = new Rect(0, 0, rowHeaderWidth, RowHeight);
            context.FillRectangle(Brushes.LightGray, topLeftRect);
            context.DrawLine(new Pen(Brushes.Gray, 0.25), topLeftRect.TopRight, topLeftRect.BottomRight);
            context.DrawLine(new Pen(Brushes.Gray, 0.25), topLeftRect.BottomLeft, topLeftRect.BottomRight);

            // ���Ʊ�ͷ
            for (int c = firstCol; c < lastCol; c++)
            {
                // ���㵱ǰ�б�ͷ��������
                var rect = new Rect(
                    rowHeaderWidth + c * ColumnWidth - _horizontalOffset,
                    0,
                    ColumnWidth,
                    RowHeight);
                // ����������������ѭ��
                if (rect.Right > dataArea.Right) break;
                context.FillRectangle(Brushes.LightGray, rect);
                context.DrawLine(new Pen(Brushes.Gray, 0.25), rect.TopRight, rect.BottomRight);
                context.DrawLine(new Pen(Brushes.Gray, 0.25), rect.BottomLeft, rect.BottomRight);
                // ��ȡ��ͷ�ı�
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

            // ����������
            if (provider != null)
            {
                // ʹ�� IDataProvider ��Ⱦ����
                for (int r = firstRow; r < lastRow; r++)
                {
                    // ��������Դ����������
                    if (r >= provider.RowCount) break;
                    // ������ͷ����
                    var rowRect = new Rect(0, (r - firstRow + 1) * RowHeight, rowHeaderWidth, RowHeight);
                    if (rowRect.Bottom > dataArea.Bottom) break;
                    context.FillRectangle(r % 2 == 0 ? Brushes.LightGray : Brushes.Gainsboro, rowRect);
                    context.DrawLine(new Pen(Brushes.Gray, 0.25), rowRect.TopRight, rowRect.BottomRight);
                    context.DrawLine(new Pen(Brushes.Gray, 0.25), rowRect.BottomLeft, rowRect.BottomRight);
                    // ��ȡ��ͷ�ı�
                    var rowText = rowHeaders.Count > r ? rowHeaders[r] : $"{r + 1}";
                    var formattedRow = new FormattedText(rowText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 14, Brushes.Black);
                    context.DrawText(formattedRow, rowRect.TopLeft + new Point(8, 8));
                    // ����ÿһ�����ݵ�Ԫ��
                    for (int c = firstCol; c < lastCol; c++)
                    {
                        if (c >= provider.ColumnCount) break;
                        var rect = new Rect(rowHeaderWidth + c * ColumnWidth - _horizontalOffset, (r - firstRow + 1) * RowHeight, ColumnWidth, RowHeight);
                        if (rect.Right > dataArea.Right) break;
                        context.FillRectangle(r % 2 == 0 ? Brushes.White : Brushes.Beige, rect);
                        context.DrawLine(new Pen(Brushes.Gray, 0.25), rect.TopRight, rect.BottomRight);
                        context.DrawLine(new Pen(Brushes.Gray, 0.25), rect.BottomLeft, rect.BottomRight);
                        // ��ȡ��Ԫ�����ݲ���ʽ��
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
                // ʹ�ö�ά������Ⱦ����
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
                        // ��ʽ����Ԫ����ֵΪָ��С��λ
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
                // ʹ�� IEnumerable ��Ⱦ����
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
                        // ͨ�������ȡ����ֵ
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

            // ������������� (0,1)
            if (gridHeight > viewport.Height)
            {
                var vScrollRect = new Rect(vScrollLeft, 0, scrollbarThickness, dataAreaHeight);
                context.FillRectangle(Brushes.LightGray, vScrollRect);
                // ������������� thumb ����
                double trackHeight = dataAreaHeight;
                double thumbHeight = trackHeight * (dataAreaHeight / gridHeight);
                thumbHeight = Math.Max(20, thumbHeight);
                double maxOffset = gridHeight - dataAreaHeight;
                double thumbTop = (maxOffset > 0) ? (trackHeight * (_verticalOffset / maxOffset)) : 0;
                thumbTop = Math.Min(thumbTop, trackHeight - thumbHeight);
                var thumbRect = new Rect(vScrollLeft, thumbTop, scrollbarThickness, thumbHeight);
                context.FillRectangle(Brushes.Gray, thumbRect);
            }
            // ���ƺ�������� (1,0)
            if (gridWidth > viewport.Width)
            {
                var hScrollRect = new Rect(0, hScrollTop, dataAreaWidth, scrollbarThickness);
                context.FillRectangle(Brushes.LightGray, hScrollRect);
                // ������������ thumb ����
                double trackWidth = dataAreaWidth;
                double thumbWidth = trackWidth * (dataAreaWidth / gridWidth);
                thumbWidth = Math.Max(20, thumbWidth);
                double maxOffset = gridWidth - dataAreaWidth;
                double thumbLeft = (maxOffset > 0) ? (trackWidth * (_horizontalOffset / maxOffset)) : 0;
                thumbLeft = Math.Min(thumbLeft, trackWidth - thumbWidth);
                var thumbRect = new Rect(thumbLeft, hScrollTop, thumbWidth, scrollbarThickness);
                context.FillRectangle(Brushes.Gray, thumbRect);
            }
            // �������½�������� (1,1)
            if (gridWidth > viewport.Width && gridHeight > viewport.Height)
            {
                var cornerRect = new Rect(vScrollLeft, hScrollTop, scrollbarThickness, scrollbarThickness);
                context.FillRectangle(Brushes.LightGray, cornerRect);
            }
        }

        /// <summary>
        /// ������갴���¼���ʵ�ֹ������϶����ܡ�
        /// </summary>
        /// <param name="e">��갴���¼�������</param>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            // ��ȡ�����λ��
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
                // ʹ�� IDataProvider ��ȡ������
                totalRows = provider.RowCount;
                totalCols = provider.ColumnCount;
            }
            else if (ItemsSource is double[,])
            {
                // ʹ�ö�ά�����ȡ������
                array = (double[,])ItemsSource;
                totalRows = array.GetLength(0);
                totalCols = array.GetLength(1);
            }
            else
            {
                // ʹ�� IEnumerable ��ȡ����
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
            // ����Ƿ������������ thumb
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
                    // ��ʼ�϶����������
                    _isDraggingHorizontal = true;
                    _dragStartPoint = pt;
                    _dragStartOffset = _horizontalOffset;
                    e.Handled = true;
                    return;
                }
            }
            // ����Ƿ������������ thumb
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
                    // ��ʼ�϶����������
                    _isDraggingVertical = true;
                    _dragStartPoint = pt;
                    _dragStartOffset = _verticalOffset;
                    e.Handled = true;
                    return;
                }
            }
        }

        /// <summary>
        /// ��������ͷ��¼��������������϶���
        /// </summary>
        /// <param name="e">����ͷ��¼�������</param>
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            // �������������������϶�
            _isDraggingHorizontal = false;
            _isDraggingVertical = false;
        }

        /// <summary>
        /// ��������ƶ��¼���ʵ�ֹ������϶�ʱ��ƫ�������¡�
        /// </summary>
        /// <param name="e">����ƶ��¼�������</param>
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            // ��ȡ��굱ǰλ��
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
                // ʹ�� IDataProvider ��ȡ������
                totalRows = provider.RowCount;
                totalCols = provider.ColumnCount;
            }
            else if (ItemsSource is double[,])
            {
                // ʹ�ö�ά�����ȡ������
                array = (double[,])ItemsSource;
                totalRows = array.GetLength(0);
                totalCols = array.GetLength(1);
            }
            else
            {
                // ʹ�� IEnumerable ��ȡ����
                var items = ItemsSource?.Cast<object>().ToList() ?? new List<object>();
                totalRows = items.Count;
            }
            double rowHeaderWidth = 80;
            double gridWidth = totalCols * ColumnWidth + rowHeaderWidth;
            double gridHeight = (totalRows + 1) * RowHeight;
            double dataAreaWidth = viewport.Width - (gridHeight > viewport.Height ? scrollbarThickness : 0);
            double dataAreaHeight = viewport.Height - (gridWidth > viewport.Width ? scrollbarThickness : 0);
            // �������������϶�
            if (_isDraggingHorizontal && gridWidth > viewport.Width)
            {
                double trackWidth = dataAreaWidth;
                double maxOffset = gridWidth - dataAreaWidth;
                double dx = pt.X - _dragStartPoint.X;
                double thumbMoveRatio = dx / trackWidth;
                // ����ˮƽƫ��������������Ч��Χ
                _horizontalOffset = Math.Max(0, Math.Min(maxOffset, _dragStartOffset + thumbMoveRatio * maxOffset));
                InvalidateVisual();
                e.Handled = true;
            }
            // ��������������϶�
            if (_isDraggingVertical && gridHeight > viewport.Height)
            {
                double trackHeight = dataAreaHeight;
                double maxOffset = gridHeight - dataAreaHeight;
                double dy = pt.Y - _dragStartPoint.Y;
                double thumbMoveRatio = dy / trackHeight;
                // ���´�ֱƫ��������������Ч��Χ
                _verticalOffset = Math.Max(0, Math.Min(maxOffset, _dragStartOffset + thumbMoveRatio * maxOffset));
                InvalidateVisual();
                e.Handled = true;
            }
        }

        /// <summary>
        /// �����������¼���ʵ�ִ�ֱ������
        /// </summary>
        /// <param name="e">�������¼�������</param>
        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            // ���ݹ��ַ��������ֱƫ����
            _verticalOffset = System.Math.Max(0, _verticalOffset - e.Delta.Y * RowHeight);
            InvalidateVisual();
            e.Handled = true;
        }
    }
}