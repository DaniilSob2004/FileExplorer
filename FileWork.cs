using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.ComponentModel;
using Ookii.Dialogs.Wpf;
using Shell32;
using System.Runtime.InteropServices;

namespace FileExplorer
{
    public static class FileWork
    {
        public static string GetFileType(string filePath)
        {
            return  Path.GetExtension(filePath).ToUpper() + " File";
        }

        public static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;

            double size = bytes;
            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return $"{size:0.##} {suffixes[suffixIndex]}";
        }

        public static long GetFileSize(string filePath)
        {
            FileInfo file = new FileInfo(filePath);
            return file.Length;
        }

        public static long GetFolderSize(string folderPath)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);
            long totalSize = 0;

            foreach (FileInfo file in directoryInfo.GetFiles())
            {
                totalSize += file.Length;
            }
            foreach (DirectoryInfo subDirectory in directoryInfo.GetDirectories())
            {
                totalSize += GetFolderSize(subDirectory.FullName);
            }

            return totalSize;
        }

        public static long GetCountFileInFolder(string folderPath)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);
            long count = 0;

            foreach (FileInfo file in directoryInfo.GetFiles())
            {
                count++;
            }
            foreach (DirectoryInfo subDirectory in directoryInfo.GetDirectories())
            {
                count += GetCountFileInFolder(subDirectory.FullName);
            }

            return count;
        }

        public static long AllCountFiles(List<string> files, List<string> dirs)
        {
            long allCount = 0;

            allCount += files.Count;
            foreach (string dir in dirs)
            {
                allCount += GetCountFileInFolder(dir);
            }

            return allCount;
        }

        public static void OpenFile(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch (Exception)
            {
                MessageBox.Show("Не удалось открыть файл!");
            }
        }

        public static void OpenWindowsCmd(string openDir)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                WorkingDirectory = openDir
            };
            Process.Start(startInfo);
        }

        public static void OpenWindowsPowershell(string openDir)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                WorkingDirectory = openDir
            };
            Process.Start(startInfo);
        }

        public static void DeleteFile(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        public static void DeleteDir(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }

        public static void EditFileName(string path, string newName)
        {
            File.Move(path, Path.Combine(Path.GetDirectoryName(path), newName));
        }

        public static void EditDirName(string path, string newName)
        {
            Directory.Move(path, Path.Combine(Path.GetDirectoryName(path), newName));
        }

        public static void CreateFolder(string path, string name)
        {
            Directory.CreateDirectory(Path.Combine(path, name));
        }

        public static void GetProperties(string path)
        {
            Shell shell = new Shell();
            string dir = Path.GetDirectoryName(path);

            if (dir == null)  // если это путь к диску (C:\, D:\ и тд)
            {
                Folder folder = shell.NameSpace(path);
                Folder myComputer = shell.NameSpace("::{20D04FE0-3AEA-1069-A2D8-08002B30309D}");  // получаем путь к папке "Этот компьютер" ("My Computer")
                foreach (FolderItem fi in myComputer.Items())
                {
                    if (fi.Path == path)
                    {
                        fi.InvokeVerb("Properties");
                        break;
                    }
                }
            }
            else if (dir == "Recycle")  // если это корзина
            {
                Folder folder = shell.NameSpace(10);
                foreach (FolderItem fi in folder.Items())
                {
                    if (fi.Name == Path.GetFileName(path))
                    {
                        fi.InvokeVerb("Properties");
                        break;
                    }
                }
            }
            else  // если это обычные файлы и папки
            {
                Folder folder = shell.NameSpace(Path.GetDirectoryName(path));
                FolderItem folderItem = folder.ParseName(Path.GetFileName(path));
                folderItem.InvokeVerb("Properties");
            }

            Marshal.FinalReleaseComObject(shell);
        }

        public static string CreateZip(DoWorkEventArgs e, BackgroundWorker worker)
        {
            // получение аргументов из объекта e.Argument
            CreateZipArguments arguments = e.Argument as CreateZipArguments;

            // извлечение необходимых аргументов
            List<string> sourceFiles = arguments.SourceFiles;
            List<string> sourceDirs = arguments.SourceDirs;
            string nameDir = arguments.NameDir;

            long allCount = AllCountFiles(sourceFiles, sourceDirs);
            long nowCount = 0;

            string nameZip = $"archive_{DateTime.Now.Hour}_{DateTime.Now.Minute}_{DateTime.Now.Second}.zip";
            using (var zipArchive = ZipFile.Open(Path.Combine(nameDir, nameZip), ZipArchiveMode.Create))
            {
                foreach (string file in sourceFiles)
                {
                    if (worker.CancellationPending)
                    {
                        e.Cancel = true;
                        MessageBox.Show("Архивация отменена!");
                        return Path.Combine(nameDir, nameZip);
                    }
                    else
                    {
                        zipArchive.CreateEntryFromFile(file, Path.GetFileName(file), CompressionLevel.Fastest);
                        worker.ReportProgress((int)((++nowCount) * 100 / allCount));
                    }
                }
                foreach (string directory in sourceDirs)
                {
                    if (!AddDirectoryToZip(zipArchive, directory, Path.GetFileName(directory), ref nowCount, ref allCount, ref worker))
                    {
                        e.Cancel = true;
                        return Path.Combine(nameDir, nameZip);
                    }
                }
            }
            return String.Empty;
        }

        private static bool AddDirectoryToZip(ZipArchive zipArchive, string sourceDirectory, string entryPrefix, ref long nowCount, ref long allCount, ref BackgroundWorker worker)
        {
            foreach (string file in Directory.GetFiles(sourceDirectory))
            {
                if (worker.CancellationPending)
                {
                    MessageBox.Show("Архивация отменена!");
                    return false;
                }
                else
                {
                    zipArchive.CreateEntryFromFile(file, entryPrefix + "/" + Path.GetFileName(file), CompressionLevel.Fastest);
                    worker.ReportProgress((int)((++nowCount) * 100 / allCount));
                }
            }
            foreach (string subdirectory in Directory.GetDirectories(sourceDirectory))
            {
                if (!AddDirectoryToZip(zipArchive, subdirectory, entryPrefix + "/" + Path.GetFileName(subdirectory) + "/", ref nowCount, ref allCount, ref worker))
                    return false;
            }
            return true;
        }
    
        public static string ExtractZipFile(DoWorkEventArgs e, BackgroundWorker worker)
        {
            VistaFolderBrowserDialog openFolderDialog = new VistaFolderBrowserDialog();
            if (openFolderDialog.ShowDialog() == true)
            {
                string zipPath = e.Argument.ToString();
                string newFolder = Path.Combine(openFolderDialog.SelectedPath, Path.GetFileNameWithoutExtension(zipPath));
                string entryExtractPath;

                if (!Directory.Exists(newFolder))
                {
                    Directory.CreateDirectory(newFolder);
                }
                else
                {
                    Directory.CreateDirectory(newFolder + "_copy");
                }

                using (ZipArchive zipArchive = ZipFile.OpenRead(zipPath))
                {
                    long allCount = zipArchive.Entries.Count;
                    long nowCount = 0;

                    foreach (ZipArchiveEntry entry in zipArchive.Entries)
                    {
                        if (worker.CancellationPending)
                        {
                            MessageBox.Show("Распаковка отменена!");
                            e.Cancel = true;
                            return newFolder;
                        }
                        entryExtractPath = Path.Combine(newFolder, entry.FullName);
                        Directory.CreateDirectory(Path.GetDirectoryName(entryExtractPath));
                        entry.ExtractToFile(entryExtractPath, true);

                        worker.ReportProgress((int)((++nowCount) * 100 / allCount));
                    }
                }
            }
            return String.Empty;
        }
    }
}
