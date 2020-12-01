using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreeSize_WPF.Models
{
    public class MainWindowTree
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public long FreeSpace { get; set; }
        public int Files { get; set; }
        public int Folders { get; set; }
        public DateTime LastModified { get; set; }

        public ObservableCollection<MainWindowTree> SubTrees { get; set; }
    }
}
