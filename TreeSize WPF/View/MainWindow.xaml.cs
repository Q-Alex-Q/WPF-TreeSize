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
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e) // Метод который выполняется при запуске приложения.
        {
            WindowTreeView.ItemsSource = await TreeBuilderAsync();
        }

        private Task<List<MainWindowTree>> TreeBuilderAsync()
        {
            DriveInfo[] drives = DriveInfo.GetDrives().Where(x => x.DriveType == DriveType.Fixed).ToArray(); // Получаем диски.
            var tree = new List<MainWindowTree>();
            const int GB = 1000000000;

            foreach (var drive in drives)
            {
                Task.Run(async () => tree.Add(new MainWindowTree()
                {
                    Name = drive.ToString(),
                    Folders = Directory.GetDirectories(drive.Name).Count(),
                    Files = Directory.GetFiles(drive.ToString()).Count(),
                    Size = drive.TotalSize / GB, // Размер в GB
                    FreeSpace = drive.TotalFreeSpace / GB,
                    LastModified = Directory.GetCreationTime(drive.ToString()),
                    SubTrees = await GetSubTrees(drive.Name)
                })
                );
            }

            return Task.FromResult(tree);
        }

        private Task<List<MainWindowTree>> GetSubTrees(string path)
        {
            
            var allDirectoriesInfo = new DirectoryInfo(path).GetDirectories();
            var allFilesInfo = new DirectoryInfo(path).GetFiles();

            var subTree = new List<MainWindowTree>();

            foreach (var dir in allDirectoriesInfo)
            {
                try
                {
                    Task.Run(async () => subTree.Add(new MainWindowTree()
                    {
                        Name = dir.Name,
                        Folders = Directory.GetDirectories(dir.FullName).Count(),
                        Files = await GetFilesAmount(dir),
                        Size = await GetDirectorySize(dir) / 1073741824,
                        LastModified = Directory.GetCreationTime(dir.ToString()),
                        SubTrees = await GetSubTrees(dir.FullName)
                    }));
                }
                catch (UnauthorizedAccessException) { }

            }

            foreach (var file in allFilesInfo) // Добавляем файлы в папке.
            {
                try
                {
                    subTree.Add(new MainWindowTree()
                    {
                        Name = file.Name,
                        Size = new FileInfo(file.FullName).Length,
                        LastModified = new FileInfo(file.FullName).CreationTime
                    });

                }
                catch (FileNotFoundException)
                {// может возникать из за того что путь к файлу больше 259 символов. РЕШИТЬ, НЕЛЬЗЯ ПОМИЛОВАТЬ!

                }
                catch (UnauthorizedAccessException) { }
            }

            return Task.FromResult(subTree);
        }

        public async Task<decimal> GetDirectorySize(DirectoryInfo dirInfo) // Метод для подсчёта размера директории ( не очень эффективный )
        {
            decimal dirSize = 0;
            FileInfo[] dirFiles = dirInfo.GetFiles();

            //Add file sizes.
            foreach (FileInfo file in dirFiles)
            {
                dirSize += GetFileLenght(file);
            }

            // Add subdirectory sizes.
            DirectoryInfo[] subDirs = dirInfo.GetDirectories();

            foreach (DirectoryInfo dir in subDirs)
            { 
                dirSize += await GetDirectorySize(dir);
            }

            return dirSize;
        }

        public async Task<int> GetFilesAmount(DirectoryInfo dirInfo) // Метод для подсчёта колличества файлов в директории.
        {
            int filesAmount = dirInfo.GetFiles().Count();
            
            DirectoryInfo[] subDirs = dirInfo.GetDirectories();

            foreach (var dir in subDirs)
            {
                try
                {
                    filesAmount += await GetFilesAmount(dir);
                }
                catch (UnauthorizedAccessException) {}
                catch (FileNotFoundException) {}
            }

            return filesAmount;
        }

        public long GetFileLenght(FileInfo file)
        {
            try
            {
                return file.Length;
            }
            catch (FileNotFoundException) { return 0; }
            catch (IOException) { return 0; }
        }

    }
}