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
        public double[,] DataSource1 { get; private set; }
        public IList<string> Columns { get; }
        public IList<string> RowHeaders { get; }

        [ObservableProperty]
        private int progress;

        public IDataProvider DataSource2 { get; private set; }
        public IList<Person> DataSource3 { get; }
        public IList<string> Columns3 { get; }

        [ObservableProperty]
        private int decimalPlaces = 4;

        public MainWindowViewModel()
        {
            int rowCount = int.MaxValue / 10000;
            int colCount = 10000;
            DataSource1 = new double[rowCount, colCount];
            Columns = new List<string>(colCount);
            RowHeaders = new List<string>(rowCount);
            for (int c = 0; c < colCount; c++)
            {
                Columns.Add($"Col {c + 1}");
            }
            DataSource2 = new BasicDataProvider(DataSource1, Columns, RowHeaders);
            Task.Run(async () =>
            {
                var rand = new Random();
                for (int r = 0; r < rowCount; r++)
                {
                    RowHeaders.Add($"{r + 1}");
                    for (int c = 0; c < colCount; c++)
                    {
                        double val = rand.NextDouble() * 398 - 199;
                        DataSource1[r, c] = Math.Round(val, 4);
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

            // DataSource3 示例
            DataSource3 = new List<Person>
            {
                new Person { Name = "Alice", Age = 30, City = "Beijing" },
                new Person { Name = "Bob", Age = 25, City = "Shanghai" },
                new Person { Name = "Charlie", Age = 28, City = "Guangzhou" }
            };
            Columns3 = new List<string> { "Name", "Age", "City" };
        }
    }
}
