using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace DataView.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // 10000行，100列的数据源，值为-199~199的随机数，保留4位小数
        public double[,] DataSource { get; private set; }
        public IList<string> Columns { get; }
        public IList<string> RowHeaders { get; }

        private int _progress;
        public int Progress
        {
            get => _progress;
            set { _progress = value; OnPropertyChanged(); }
        }

        public MainWindowViewModel()
        {
            int rowCount = int.MaxValue / 10000;
            int colCount = 10000;
            DataSource = new double[rowCount, colCount];
            Columns = new List<string>(colCount);
            RowHeaders = new List<string>(rowCount);
            var rand = new Random();
            for (int c = 0; c < colCount; c++)
            {
                Columns.Add($"Col {c + 1}");
            }
            Task.Run(async () =>
            {
                for (int r = 0; r < rowCount; r++)
                {
                    RowHeaders.Add($"{r + 1}");
                    for (int c = 0; c < colCount; c++)
                    {
                        double val = rand.NextDouble() * 398 - 199; // [-199, 199]
                        DataSource[r, c] = Math.Round(val, 4);
                    }
                    if (r % 100 == 0)
                    {
                        Progress = (int)((r + 1) * 100.0 / rowCount);
                        await Task.Yield();
                    }
                }
                Progress = 100;
            });
        }
    }
}
