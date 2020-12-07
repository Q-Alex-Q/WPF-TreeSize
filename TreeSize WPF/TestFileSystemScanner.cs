using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TreeSize_WPF.Models;

namespace TreeSize_WPF
{
    public class TestFileSystemScanner
    {
        public async Task<List<MainWindowTree>> GetFileSystemInfoAsync(string path)
        {
            var result = new List<MainWindowTree>();
            var tasks = Directory
                .GetDirectories(path)
                .Select(folder => Task.Run(() => GetInfoAsync(folder)));

            var directories = await Task.WhenAll(tasks);
            result.AddRange(directories);
            result.AddRange(GetFiles(path));

            return result;
        }

        private async Task<MainWindowTree> GetInfoAsync(string path)
        {
            var directoryInfo = new MainWindowTree();

            var directories = await GetDirectoriesAsync(path);
            var files = GetFiles(path);

            directoryInfo.SubTrees.AddRange(directories);
            directoryInfo.SubTrees.AddRange(files);
            
            directoryInfo.Name = Path.GetFileName(path);
            directoryInfo.Size = directoryInfo.SubTrees.Sum(item => item.Size);
            directoryInfo.Files = directories.Sum(dir => dir.Files) + files.Count;
            directoryInfo.Folders = directories.Sum(dir => dir.Folders) + directories.Count;
            directoryInfo.LastModified = new DirectoryInfo(path).CreationTime;
            return directoryInfo;
        }

        private async Task<List<MainWindowTree>> GetDirectoriesAsync(string path)
        {
            var result = new List<MainWindowTree>();
            var directories = GetDirectoryNames(path);

            foreach (var directory in directories)
            {
                var direcotryInfo = await GetInfoAsync(directory);
                result.Add(direcotryInfo);
            }
            return result;
        }

        private List<MainWindowTree> GetFiles(string path)
        {
            var result = new List<MainWindowTree>();
            var files = GetFileNames(path);

            foreach (var file in files)
            {
                var fileInfo = GetFileInfo(file);
                result.Add(fileInfo);
            }
            return result;
        }

        private string[] GetDirectoryNames(string path)
        {
            try
            {
                return Directory.GetDirectories(path);
            }
            catch
            {
                return new string[0];
            }
        }

        private string[] GetFileNames(string path)
        {
            try
            {
                return Directory.GetFiles(path);
            }
            catch
            {
                return new string[0];
            }
        }

        private MainWindowTree GetFileInfo(string path)
        {
            var fileInfo = new FileInfo(path);
            return new MainWindowTree
            {
                Name = fileInfo.Name,
                Size = FileLength(fileInfo),
                LastModified = fileInfo.CreationTime                
            };
        }

        private long FileLength(FileInfo info)
        {
            try
            {
                return info.Length;
            }
            catch
            {
                return 0;
            }
        }
    }
}
