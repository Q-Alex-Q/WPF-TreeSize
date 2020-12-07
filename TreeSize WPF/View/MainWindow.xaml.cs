using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using TreeSize_WPF.Models;

namespace TreeSize_WPF.View
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly TestFileSystemScanner _dirScanner;
        public MainWindow()
        {
            InitializeComponent();
            _dirScanner = new TestFileSystemScanner();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) // Метод который выполняется при запуске приложения.
        {
            
        }

        private async void Pusk_Click(object sender, RoutedEventArgs e)
        {
            ((Button)sender).IsEnabled = false;
            DriveInfo[] drives = DriveInfo.GetDrives().Where(x => x.DriveType == DriveType.Fixed).ToArray();
            var hardDrives = new List<MainWindowTree>();

            foreach(var hardDrive in drives)
            {
                var driveInfo = new MainWindowTree
                {
                    Name = hardDrive.Name,
                    SubTrees = await _dirScanner.GetFileSystemInfoAsync(hardDrive.Name)
                };
                hardDrives.Add(driveInfo);
            }
            WindowTreeView.ItemsSource = hardDrives;
            ((Button)sender).IsEnabled = true;
        }
    }
}