using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace DataView.ViewModels
{
    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string City { get; set; }
    }

    public partial class MainWindowViewModel : ViewModelBase
    {
        public string Greeting { get; } = "Welcome to Avalonia!";

        public ObservableCollection<Person> People { get; } = new ObservableCollection<Person>
        {
            new Person { Name = "Alice", Age = 30, City = "Beijing" },
            new Person { Name = "Bob", Age = 25, City = "Shanghai" },
            new Person { Name = "Carol", Age = 28, City = "Guangzhou" },
            new Person { Name = "David", Age = 35, City = "Shenzhen" },
            // 可继续添加更多数据以测试虚拟化性能
        };

        public IList<string> Columns { get; } = new List<string> { "Name", "Age", "City" };
    }
}
