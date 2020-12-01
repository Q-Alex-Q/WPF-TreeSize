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
            //Способ 1. Выводим дерево на UI (!!!сделать метод Window_Loaded async)
            //WindowTreeView.ItemsSource = await Task.Run(() => TreeBuilder());

            // Способ 2. Создать async метод через который мы запустим синхронный метод.
            //WindowTreeView.ItemsSource = await TreeBuilderAsync();

            WindowTreeView.ItemsSource = await TreeBuilderParallel();

            //GreedTreeView.ItemsSource = await TreeBuilderAsync();

            // Способ 3. Запускаем в отдельном потоке.(для использования необхдимо разкоментить Dispatcher в TreeBuilder)
            //Thread thread = new Thread(TreeBuilder);
            //thread.Start();
        }

        private async Task<ObservableCollection<MainWindowTree>> TreeBuilderAsync()
        {
            DriveInfo[] drives = DriveInfo.GetDrives().Where(x => x.DriveType == DriveType.Fixed).ToArray(); // Получаем диски.
            var tree = new ObservableCollection<MainWindowTree>();

            //List<Task> tasks = new List<Task>();

            //foreach (var drive in drives)
            //{
            //    tasks.Add(Task.Run(async () => tree.Add(new MainWindowTree()
            //    {
            //        Name = drive.ToString(),
            //        Folders = Directory.GetDirectories(drive.Name).Count(),
            //        Files = Directory.GetFiles(drive.ToString()).Count(),
            //        Size = drive.TotalSize / 1073741824, // Размер в GB ( взять в константу )
            //        FreeSpace = drive.TotalFreeSpace / 1073741824,
            //        LastModified = Directory.GetCreationTime(drive.ToString()),
            //        SubTrees = await GetSubTrees(drive.Name)
            //    })));
            //}

            //await Task.WhenAll(tasks);



            foreach (var drive in drives)
            {
                await Task.Run(async () => tree.Add(new MainWindowTree()
                {
                    Name = drive.ToString(),
                    Folders = Directory.GetDirectories(drive.Name).Count(),
                    Files = Directory.GetFiles(drive.ToString()).Count(),
                    Size = drive.TotalSize / 1000000000, // Размер в GB
                    FreeSpace = drive.TotalFreeSpace / 1000000000,
                    LastModified = Directory.GetCreationTime(drive.ToString()),
                    SubTrees = await GetSubTrees(drive.Name)
                })
                );
            }


            return tree;
        }

        private Task<ObservableCollection<MainWindowTree>> TreeBuilderParallel()
        {
            DriveInfo[] drives = DriveInfo.GetDrives().Where(x => x.DriveType == DriveType.Fixed).ToArray(); // Получаем диски.
            var tree = new ObservableCollection<MainWindowTree>();

            Parallel.ForEach(drives, drive =>
            {
                tree.Add(new MainWindowTree()
                {
                    Name = drive.ToString(),
                    Folders = Directory.GetDirectories(drive.Name).Count(),
                    Files = Directory.GetFiles(drive.ToString()).Count(),
                    Size = drive.TotalSize / 1000000000, // Размер в GB
                    FreeSpace = drive.TotalFreeSpace / 1000000000,
                    LastModified = Directory.GetCreationTime(drive.ToString()),
                    SubTrees = GetTrees(drive.Name)
                });
            });

            return Task.FromResult(tree);
        }


        private async Task<ObservableCollection<MainWindowTree>> TreeBuilder()
        {
            DriveInfo[] drives = DriveInfo.GetDrives().Where(x => x.DriveType == DriveType.Fixed).ToArray(); // Получаем диски.
            var tree = new ObservableCollection<MainWindowTree>(); 

            foreach (var drive in drives) // Строим дерево начиная с главных дисков.
            {
                tree.Add(new MainWindowTree() // Добавляем диски и строим дерево вызывая на SubTrees метод GetSubTrees.
                {
                    Name = drive.ToString(),
                    Folders = Directory.GetDirectories(drive.Name).Count(),
                    //Files = Directory.GetFiles(drive.ToString()).Count(),
                    Size = drive.TotalSize / 1000000000, // Размер в GB
                    FreeSpace = drive.TotalFreeSpace / 1000000000,
                    LastModified = Directory.GetCreationTime(drive.ToString()),
                    SubTrees = await GetSubTrees(drive.Name)
                });
            }

            //Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate () // Для передачи из Thread в главный поток.
            //{
            //    WindowTreeView.ItemsSource = tree;
            //});

            return tree;
        }

        public ObservableCollection<MainWindowTree> GetTrees(string path)
        {
            var allDirectoriesInfo = new DirectoryInfo(path).GetDirectories();
            var allFilesInfo = new DirectoryInfo(path).GetFiles();

            var tree = new ObservableCollection<MainWindowTree>();

            foreach (var dir in allDirectoriesInfo)
            {
                try
                {
                    tree.Add(new MainWindowTree()
                    {
                        Name = dir.Name,
                        Folders = Directory.GetDirectories(dir.FullName).Count(),
                        //Files = GetFilesAmountParallel(dir),
                        //Size = GetDirSizeParallel(dir) / 1073741824,
                        LastModified = Directory.GetCreationTime(dir.ToString()),
                        SubTrees = GetTrees(dir.FullName)
                    });
                }
                catch (UnauthorizedAccessException)
                {

                }
                
            }

            foreach (var file in allFilesInfo)
            {
                try
                {
                    tree.Add(new MainWindowTree()
                    {
                        Name = file.Name,
                        Size = new FileInfo(file.FullName).Length,
                        LastModified = new FileInfo(file.FullName).CreationTime
                    });
                }
                catch (FileNotFoundException)
                {

                }
                catch (UnauthorizedAccessException) { }
                
            }

            return tree;
        }

        public long GetDirSizeParallel(DirectoryInfo dirInfo)
        {
            long dirSize = 0;
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
                dirSize += GetDirSizeParallel(dir);
            }

            return dirSize;
        }

        public int GetFilesAmountParallel(DirectoryInfo dirInfo)
        {
            int filesAmount = dirInfo.GetFiles().Count();

            DirectoryInfo[] subDirs = dirInfo.GetDirectories();

            foreach (var dir in subDirs)
            {
                try
                {
                    filesAmount += GetFilesAmountParallel(dir);
                }
                catch (UnauthorizedAccessException) { filesAmount += 0; }
                catch (FileNotFoundException) { filesAmount += 0; }
            }

            return filesAmount;
        }






















        private async Task<ObservableCollection<MainWindowTree>> GetSubTrees(string path)
        {

            var allDirectoriesInfo = new DirectoryInfo(path).GetDirectories();
            var allFilesInfo = new DirectoryInfo(path).GetFiles();

            var subTree = new ObservableCollection<MainWindowTree>();

            List<Task> tasks = new List<Task>();

            foreach (var dir in allDirectoriesInfo)
            {
                try
                {
                    tasks.Add(Task.Run(async () => subTree.Add(new MainWindowTree()
                    {
                        Name = dir.Name,
                        Folders = Directory.GetDirectories(dir.FullName).Count(),
                        //Files = await GetFilesAmount(dir),
                        Size = await GetDirectorySize(dir) / 1073741824,
                        LastModified = Directory.GetCreationTime(dir.ToString()),
                        SubTrees = await GetSubTrees(dir.FullName)
                    })));
                }
                catch (UnauthorizedAccessException) { }

            }
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception)
            {
            }
              

            foreach (var file in allFilesInfo) // Добавляем файлы в папке.
            {
                try
                {
                    //await Task.Run(async () => subTree.Add(new MainWindowTree() // быстрее но очень нагружает систему.
                    //{
                    //    Name = file.Name,
                    //    Size = new FileInfo(file.FullName).Length,
                    //    LastModified = new FileInfo(file.FullName).CreationTime
                    //}));
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
            }

            return subTree;
        }

        public async Task<long> GetDirectorySize(DirectoryInfo dirInfo) // Метод для подсчёта размера директории ( не очень эффективный )
        {
            long dirSize = 0;
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
            //List<Task> tasks = new List<Task>();

            foreach (var dir in subDirs)
            {
                try
                {
                    //tasks.Add(Task.Run(async () => filesAmount += await GetFilesAmount(dir)));
                    filesAmount += await GetFilesAmount(dir);
                }
                catch (UnauthorizedAccessException) {}
                catch (FileNotFoundException) {}
            }

            //await Task.WhenAll(tasks);

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