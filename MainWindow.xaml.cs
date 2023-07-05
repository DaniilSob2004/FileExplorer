using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.IO;
using System.ComponentModel;
using System.IO.Compression;

namespace FileExplorer
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        enum TypeListView { Base, ThisPC, Recycle };

        BackgroundWorker worker, workerExtract;
        ProgressWindow progressWindow, progressWindowExtract;

        TypeListView typeListView;
        TreeViewPath treeViewPath;
        BaseListView baseListView;
        FolderNavigation folderNavigation;
        string openDir;

        bool isChangeFileName;
        string oldName;


        public MainWindow()
        {
            InitializeComponent();

            worker = new BackgroundWorker() { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
            worker.DoWork += Worker_DoWork;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            worker.ProgressChanged += Worker_ProgressChanged;

            workerExtract = new BackgroundWorker() { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
            workerExtract.DoWork += WorkerExtract_DoWork;
            workerExtract.RunWorkerCompleted += WorkerExtract_RunWorkerCompleted;
            workerExtract.ProgressChanged += WorkerExtract_ProgressChanged;

            treeViewPath = new TreeViewPath(this);
            MyStatusItems.SetParent(this);
            folderNavigation = new FolderNavigation();

            ChangeTypeListView(TypeListView.ThisPC);
            OpenDir = "This PC";
            folderNavigation.AddPathNavigation(OpenDir);

            isChangeFileName = false;
            oldName = "";
        }


        // реализация интерфейса INotifyPropertyChanged, для привязки свойства OpenDir и элемента TextBox.Text
        public string OpenDir
        {
            get { return openDir; }
            set
            {
                if (openDir != value)
                {
                    openDir = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("OpenDir"));
                }
            }
        }
 
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, e);
        }


        private void ChangeTypeListView(TypeListView typeListView)
        {
            this.typeListView = typeListView;
            switch (typeListView)
            {
                case TypeListView.Base:
                    baseListView = ListViewPath.GetInstance(this);
                    break;

                case TypeListView.ThisPC:
                    baseListView = ListViewPC.GetInstance(this);
                    break;

                case TypeListView.Recycle:
                    baseListView = ListViewRecycle.GetInstance(this);
                    break;
            }
            baseListView.StartSettings();
            baseListView.DrawDir(OpenDir);
        }


        // обработчики для первого MenuItem
        private void MenuItemOpenCmd_Click(object sender, RoutedEventArgs e)
        {
            FileWork.OpenWindowsCmd(OpenDir);
        }

        private void MenuItemOpenPowershell_Click_1(object sender, RoutedEventArgs e)
        {
            FileWork.OpenWindowsPowershell(OpenDir);
        }

        private void MenuItemClose_Click_2(object sender, RoutedEventArgs e)
        {
            Close();
        }


        // обработчики для TreeView
        internal void NewItem_MouseLeave(object sender, MouseEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)sender;
            if (item != null)
            {
                item.Background = TreeViewPath.brush;
                e.Handled = true;
            }
        }

        internal void NewItem_MouseEnter(object sender, MouseEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)sender;
            if (item != null)
            {
                item.Background = Brushes.DimGray;
                e.Handled = true;
            }
        }

        internal void TreeViewItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)sender;
            if (item != null)
            {
                item.IsExpanded = true;

                if (treeViewPath.GetHeader(item) == "This PC")
                {
                    if (typeListView != TypeListView.ThisPC)
                    {
                        OpenDir = "This PC";
                        ChangeTypeListView(TypeListView.ThisPC);
                    }
                }
                else if (treeViewPath.GetHeader(item) == "Recycle")
                {
                    if (typeListView != TypeListView.Recycle)
                    {
                        OpenDir = "Recycle";
                        ChangeTypeListView(TypeListView.Recycle);
                    }
                }
                //else if (treeViewPath.GetHeader(item) == "Quick Access") { }
                else
                {
                    OpenDir = treeViewPath.GetTag(item);
                    if (typeListView != TypeListView.Base)
                        ChangeTypeListView(TypeListView.Base);
                    else
                        baseListView.DrawDir(OpenDir);
                }

                folderNavigation.AddPathNavigation(OpenDir);  // история навигации

                e.Handled = true;
            }
        }

        internal void NewItem_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)sender;
            if (item != null)
            {
                treeViewPath.DrawDir(item, item.Tag + "");
                string tag = treeViewPath.GetTag(item);
                if (tag != "")
                {
                    baseListView.DrawDir(tag);
                }
                e.Handled = true;
            }
        }

        internal void NewItem_Collapsed(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)sender;
            if (item != null)
            {
                if ((item.Parent as TreeView) == null)
                {
                    item.Items.Clear();
                    treeViewPath.AddPassItem(item);
                    e.Handled = true;
                }
            }
        }


        // обработчик для кнопки обновления
        internal void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            treeViewPath.UpdateDrawDir();
            baseListView.UpdateDrawDir();
        }


        // обработчики и метод для ListView
        internal void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            baseListView.SelectedItems();
        }

        internal void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // используем метод FindAncestor<T>, чтобы найти предка элемента, который является экземпляром класса
            ListViewItem listViewItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);

            if (listViewItem != null)  // клик произошел на элементе ListView
            {
                string filename = ((MyItem)listView.SelectedItem).Item2;
                string filePath = System.IO.Path.Combine(OpenDir, filename);
                if (File.Exists(filePath))
                {
                    FileWork.OpenFile(filePath);
                }
                else if(Directory.Exists(filePath))
                {
                    if (typeListView != TypeListView.Recycle)
                    {
                        OpenDir = filePath;
                        if (typeListView == TypeListView.Base)
                        {
                            baseListView.DrawDir(OpenDir);
                        }
                        else if (typeListView == TypeListView.ThisPC)
                        {
                            ChangeTypeListView(TypeListView.Base);
                        }
                        folderNavigation.AddPathNavigation(OpenDir);  // история навигации
                    }
                }
                else
                {
                    MessageBox.Show("Невозможно открыть файл/папку!", "Message", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T target) return target;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }


        // обработчики и метод для кнопок навигации
        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            UpdateNavigation(folderNavigation.Back());
        }

        private void ForwardBtn_Click(object sender, RoutedEventArgs e)
        {
            UpdateNavigation(folderNavigation.Forward());
        }

        private void UpBtn_Click(object sender, RoutedEventArgs e)
        {
            string path = folderNavigation.Up(OpenDir);
            if (path != "") UpdateNavigation(path);
        }

        private void UpdateNavigation(string path)
        {
            OpenDir = path;
            if (path == "Recycle")
            {
                ChangeTypeListView(TypeListView.Recycle);
            }
            else if (path == "This PC")
            {
                ChangeTypeListView(TypeListView.ThisPC);
            }
            else if (path == "Quick Access") { }
            else
            {
                if (typeListView != TypeListView.Base)
                    ChangeTypeListView(TypeListView.Base);
                else
                    baseListView.DrawDir(OpenDir);
            }
        }


        // обработчик и BackgroundWorker для комманды Zip и ExtractZip
        private void Zip_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (typeListView == TypeListView.Base)
            {
                string path;
                List<string> files = new List<string>(), dirs = new List<string>();
                foreach (var item in listView.SelectedItems)
                {
                    path = System.IO.Path.Combine(OpenDir, ((MyItem)item).Item2);
                    if (File.Exists(path))  // если это файл
                    {
                        files.Add(path);
                    }
                    else  // если это папка
                    {
                        dirs.Add(path);
                    }
                }

                if (!worker.IsBusy)
                {
                    // запуск backroundWorker
                    CreateZipArguments arguments = new CreateZipArguments(files, dirs, OpenDir);  // передача аргументов в виде объекта
                    worker.RunWorkerAsync(arguments);  // запуск фонового процесса с передачей аргументов
                }
                else MessageBox.Show("Подождите пожалуйста, происходит отмена архивации!");
            }
            else
            {
                MessageBox.Show("Невозможно выполнить архивацию!", "Message", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Worker_DoWork(object? sender, DoWorkEventArgs e)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    progressWindow = new ProgressWindow(this, "Zip");
                    progressWindow.Show();
                });
                FileWork.DeleteFile(FileWork.CreateZip(e, worker));
            }
            catch (UnauthorizedAccessException)
            {
                var answer = MessageBox.Show("Нельзя сохранять в этой директории .zip файлы\nХотите разместить на рабочем столе?", "Error", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (answer == MessageBoxResult.Yes)
                {
                    ((CreateZipArguments)e.Argument).NameDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    FileWork.DeleteFile(FileWork.CreateZip(e, worker));
                }
            }
        }

        private void Worker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            progressWindow.textBoxValue.Text = e.ProgressPercentage.ToString() + "%";
            progressWindow.progressBar.Value = e.ProgressPercentage;
        }

        private void Worker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            progressWindow.Close();
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message + "\n" + e.Error.Data + "\n" + e.Error.InnerException);
            }
            else MessageBox.Show("Архивация прошла успешно!");
            baseListView.UpdateDrawDir();
        }

        public void CloseProgressWindow(string typeWorker)
        {
            if (typeWorker == "Zip") worker.CancelAsync();
            else workerExtract.CancelAsync();
        }


        private void ExtractZip_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (typeListView == TypeListView.Base && listView.SelectedItems.Count == 1)
            {
                string zipPath = System.IO.Path.Combine(OpenDir, ((MyItem)listView.SelectedItems[0]).Item2);
                if (System.IO.Path.GetExtension(zipPath) == ".zip")
                {
                    if (!worker.IsBusy)
                    {
                        workerExtract.RunWorkerAsync(zipPath);  // запуск фонового процесса с передачей аргумента
                    }
                    else MessageBox.Show("Подождите пожалуйста, происходит отмена распаковки!");
                    return;
                }
            }
            MessageBox.Show("Невозможно распаковать!", "Message", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void WorkerExtract_DoWork(object? sender, DoWorkEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                progressWindowExtract = new ProgressWindow(this, "Extract");
                progressWindowExtract.Show();
            });
            FileWork.DeleteDir(FileWork.ExtractZipFile(e, workerExtract));
        }

        private void WorkerExtract_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            progressWindowExtract.textBoxValue.Text = e.ProgressPercentage.ToString() + "%";
            progressWindowExtract.progressBar.Value = e.ProgressPercentage;
        }

        private void WorkerExtract_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            progressWindowExtract.Close();
            if (e.Cancelled)
            {
                MessageBox.Show("Cancelled!");
            }
            else if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message + "\n" + e.Error.Data + "\n" + e.Error.InnerException);
            }
            else
            {
                MessageBox.Show("Распаковка архива прошла успешно!");
            }
            baseListView.UpdateDrawDir();
        }


        // обработчики для изменения названия элемента в ListView
        private void TextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (typeListView == TypeListView.Base)
            {
                RenamePath_Executed(sender, null);
            }
            else
            {
                string text = ((TextBox)sender).Text;
                foreach (var item in listView.Items)
                {
                    if (((MyItem)item).Item2 == text)
                    {
                        listView.SelectedItem = item;
                        return;
                    }
                }
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (isChangeFileName)
            {
                TextBox textBox = (TextBox)sender;
                try
                {
                    if (File.Exists(System.IO.Path.Combine(OpenDir, oldName)))
                    {
                        if (System.IO.Path.GetExtension(oldName) != System.IO.Path.GetExtension(textBox.Text))
                        {
                            var answer = MessageBox.Show("Вы действительно хотите изменить расширение файла?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (answer == MessageBoxResult.Yes)
                                FileWork.EditFileName(System.IO.Path.Combine(OpenDir, oldName), textBox.Text);
                        }
                        else FileWork.EditFileName(System.IO.Path.Combine(OpenDir, oldName), textBox.Text);
                    }
                    else
                        FileWork.EditDirName(System.IO.Path.Combine(OpenDir, oldName), textBox.Text);
                }
                catch (Exception) {}
                finally
                {
                    ((TextBox)sender).Style = (Style)(this.Resources["myEditBoxStyle"]);  // назначаем стиль
                    baseListView.UpdateDrawDir();
                }
                isChangeFileName = false;
            }
        }


        // обработчик для комманды Properties
        private void Properties_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (listView.SelectedItems.Count == 1)
                FileWork.GetProperties(System.IO.Path.Combine(OpenDir, ((MyItem)listView.SelectedItem).Item2));
        }


        // обработчик для комманды AllSelected, NoneSelected
        private void AllSelected_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            listView.SelectAll();
        }

        private void NoneSelected_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            listView.UnselectAll();
        }


        // обработчик для комманды RenamePath, SaveRenamePath и FindVisualChild который ищет нужный TextBox
        private void RenamePath_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (typeListView == TypeListView.Base && listView.SelectedItems.Count <= 1)
            {
                TextBox textBox = null;
                if (sender is TextBox text)
                {
                    textBox = text;
                }
                else
                {
                    DependencyObject container = listView.ItemContainerGenerator.ContainerFromItem(listView.SelectedItem);
                    TextBox findText = FindVisualChild(container);
                    if (findText != null) textBox = findText;
                }
                textBox.Focus();
                textBox.SelectAll();
                textBox.Style = null;  // снимаем стиль
                oldName = textBox.Text;
                isChangeFileName = true;
            }
            else
            {
                MessageBox.Show("Нельзя изменить название!", "Message", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SaveRenamePath_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // если это TextBox из ListView (т.к. они находятся в StackPanel)
            if (Keyboard.FocusedElement is TextBox text && text.Parent is StackPanel)
            {
                listView.Focus();
            }
        }

        private TextBox FindVisualChild(DependencyObject parent)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is TextBox typedChild) return typedChild;

                TextBox foundChild = FindVisualChild(child);
                if (foundChild != null) return foundChild;
            }
            return null;
        }


        // обработчик для комманды NewFolder
        private void NewFolder_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (typeListView == TypeListView.Base)
            {
                try
                {
                    string dirdefaultName = $"new_folder_{DateTime.Now.Hour}_{DateTime.Now.Minute}_{DateTime.Now.Second}";
                    Directory.CreateDirectory(System.IO.Path.Combine(OpenDir, dirdefaultName));
                    baseListView.UpdateDrawDir();
                }
                catch (Exception) {}
            }
            else
            {
                MessageBox.Show("Невозможно создать папку!", "Message", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    public class CreateZipArguments
    {
        public List<string> SourceFiles { get; }
        public List<string> SourceDirs { get; }
        public string NameDir { get; set; }

        public CreateZipArguments(List<string> sourceFiles, List<string> sourceDirs, string nameDir)
        {
            SourceFiles = sourceFiles;
            SourceDirs = sourceDirs;
            NameDir = nameDir;
        }
    }

    public class FileCommands
    {
        static FileCommands()
        {
            Zip = new RoutedCommand("Zip", typeof(FileCommands));
            ExtractZip = new RoutedCommand("ExtractZip", typeof(FileCommands));
            Properties = new RoutedCommand("Properties", typeof(FileCommands));
            AllSelected = new RoutedCommand("AllSelected", typeof(FileCommands));
            NoneSelected = new RoutedCommand("NoneSelected", typeof(FileCommands));
            RenamePath = new RoutedCommand("RenamePath", typeof(FileCommands));
            SaveRenamePath = new RoutedCommand("NoneSelected", typeof(FileCommands));
            NewFolder = new RoutedCommand("NewFolder", typeof(FileCommands));
        }
        public static RoutedCommand Zip { get; set; }
        public static RoutedCommand ExtractZip { get; set; }
        public static RoutedCommand Properties { get; set; }
        public static RoutedCommand AllSelected { get; set; }
        public static RoutedCommand NoneSelected { get; set; }
        public static RoutedCommand RenamePath { get; set; }
        public static RoutedCommand SaveRenamePath { get; set; }
        public static RoutedCommand NewFolder { get; set; }
    }
}
