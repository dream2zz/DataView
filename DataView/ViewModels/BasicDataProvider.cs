using System.Collections.Generic;
using DataView.Controls;

namespace DataView.ViewModels
{
    public class BasicDataProvider : IDataProvider
    {
        private readonly double[,] dataSource;
        private readonly IList<string> columns;
        private readonly IList<string> rowHeaders;

        public BasicDataProvider(double[,] dataSource, IList<string> columns, IList<string> rowHeaders)
        {
            this.dataSource = dataSource;
            this.columns = columns;
            this.rowHeaders = rowHeaders;
        }

        public int RowCount => dataSource.GetLength(0);
        public int ColumnCount => dataSource.GetLength(1);
        public object GetCell(int row, int col) => dataSource[row, col];
        public IList<string> GetColumnHeaders() => columns;
        public IList<string> GetRowHeaders() => rowHeaders;

        // IEnumerable implementation
        public System.Collections.IEnumerator GetEnumerator()
        {
            for (int r = 0; r < dataSource.GetLength(0); r++)
                for (int c = 0; c < dataSource.GetLength(1); c++)
                    yield return dataSource[r, c];
        }
    }
}
