using Avalonia.Controls;
using DataView.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataView.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public double[,] DataSource { get; private set; }
        public IList<string> Columns { get; }
        public IList<string> RowHeaders { get; }

        [ObservableProperty]
        private int progress;

        public IDataProvider DataSource2 { get; private set; }

        public MainWindowViewModel()
        {
            int rowCount = int.MaxValue / 10000;
            int colCount = 10000;
            DataSource = new double[rowCount, colCount];
            Columns = new List<string>(colCount);
            RowHeaders = new List<string>(rowCount);
            for (int c = 0; c < colCount; c++)
            {
                Columns.Add($"Col {c + 1}");
            }
            DataSource2 = new BasicDataProvider(DataSource, Columns, RowHeaders);
            Task.Run(async () =>
            {
                var rand = new Random();
                for (int r = 0; r < rowCount; r++)
                {
                    RowHeaders.Add($"{r + 1}");
                    for (int c = 0; c < colCount; c++)
                    {
                        double val = rand.NextDouble() * 398 - 199;
                        DataSource[r, c] = Math.Round(val, 4);
                    }
                    if (r % 100 == 0)
                    {
                        var value = (int)((r + 1) * 100.0 / rowCount);
                        await Dispatcher.UIThread.InvokeAsync(() => Progress = value);
                        await Task.Yield();
                    }
                }
                await Dispatcher.UIThread.InvokeAsync(() => Progress = 100);
            });
        }
    }
}
