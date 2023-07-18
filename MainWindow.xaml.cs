using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.ComponentModel;

namespace FileExplorer
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        enum TypeListView { Base, ThisPC, Recycle };  // перечисления для корней дерева

        BackgroundWorker worker, workerExtract;  // доп. потоки для архивации и распаковки
        ProgressWindow progressWindow, progressWindowExtract;  // доп. окна для архивации и распаковки

        TypeListView typeListView;  // какой корневой элемент дерева сейчас открыт
        TreeViewPath treeViewPath;  // для отображения дерева
        BaseListView baseListView;  // ссылка на базовый класс для отображения списка
        FolderNavigation folderNavigation;  // для навигации по списку
        string openDir;  // какая директория сейчас открыта

        bool isChangeFileName;  // изменяется ли сейчас название файла/папки
        string oldName;  // старое название файла/папки


        public MainWindow()
        {
            InitializeComponent();
            StartSettings();
        }

        private void StartSettings()
        {
            // настройка BackgroundWorker-а для архивации
            worker = new BackgroundWorker() { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
            worker.DoWork += Worker_DoWork;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            worker.ProgressChanged += Worker_ProgressChanged;

            // настройка BackgroundWorker-а для распаковки
            workerExtract = new BackgroundWorker() { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
            workerExtract.DoWork += WorkerExtract_DoWork;
            workerExtract.RunWorkerCompleted += WorkerExtract_RunWorkerCompleted;
            workerExtract.ProgressChanged += WorkerExtract_ProgressChanged;

            treeViewPath = new TreeViewPath(this);
            MyStatusItems.SetParent(this);  // настраиваем родителя для MyStatusItems
            folderNavigation = new FolderNavigation();

            ChangeTypeListView(TypeListView.ThisPC);  // по умолчанию будет отображаться элемент дерева ThisPC
            OpenDir = "This PC";  // открытая директория
            folderNavigation.AddPathNavigation(OpenDir);  // добавляем в навигацию что посетили директорию ThisPC

            isChangeFileName = false;
            oldName = "";
        }


        // реализация интерфейса INotifyPropertyChanged, для привязки свойства OpenDir и элемента TextBox.Text
        public string OpenDir
        {
            get => openDir;
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
            // меняем открытую директорию у дерева

            this.typeListView = typeListView;
            switch (typeListView)
            {
                // с помощью полиморфизма и паттерна 'Одиночка', получаем экземпляр объекта списка, который необходим
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
            baseListView.StartSettings();  // начальные настройки для отображения списка
            baseListView.DrawDir(OpenDir);  // отображение списка, передаём название директории которая сейчас открыта
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
            // меняем цвет элемента дерева, если курсор вышел из элемента
            TreeViewItem item = (TreeViewItem)sender;
            if (item != null)
            {
                item.Background = TreeViewPath.brush;
                e.Handled = true;
            }
        }

        internal void NewItem_MouseEnter(object sender, MouseEventArgs e)
        {
            // меняем цвет элемента дерева, если на него попал курсор
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
                item.IsExpanded = true;  // раскрываем элемент дерева

                if (treeViewPath.GetHeader(item) == "This PC")  // если нажато по "This PC" и это не второй раз подряд, то меняем тип и открываем указанную директорию
                {
                    if (typeListView != TypeListView.ThisPC)
                    {
                        OpenDir = "This PC";
                        ChangeTypeListView(TypeListView.ThisPC);
                    }
                }
                else if (treeViewPath.GetHeader(item) == "Recycle")  // если нажато по "Recycle" и это не второй раз подряд, то меняем тип и открываем указанную директорию
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
                    if (typeListView != TypeListView.Base)  // если нажали не второй раз подряд, то меняем тип и открываем указанную директорию
                        ChangeTypeListView(TypeListView.Base);
                    else
                        baseListView.DrawDir(OpenDir);  // отображаем указанную директорию
                }

                folderNavigation.AddPathNavigation(OpenDir);  // добавляем директорию в историю навигации
                e.Handled = true;
            }
        }

        internal void NewItem_Expanded(object sender, RoutedEventArgs e)
        {
            // при расширении элемента дерева, отображаем содержимое директории в дереве и списке
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
            // при скрытии элемента дерева
            TreeViewItem item = (TreeViewItem)sender;
            if (item != null)
            {
                if ((item.Parent as TreeView) == null)  // если родитель элемента жто не корень дерева
                {
                    item.Items.Clear();  // очищаем
                    treeViewPath.AddPassItem(item);  // добаляем заглушку
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
            // используем метод FindAncestor, чтобы найти предка элемента, который является экземпляром класса
            ListViewItem listViewItem = FindAncestor((DependencyObject)e.OriginalSource);

            if (listViewItem != null)  // клик произошел на элементе ListView
            {
                string filename = ((MyItem)listView.SelectedItem).Item2;  // узнаём название элемента (Item2 объекта MyItem это название)
                string filePath = Path.Combine(OpenDir, filename);

                if (File.Exists(filePath)) FileWork.OpenFile(filePath);  // если файл существует то открываем как приложение
                else if(Directory.Exists(filePath))  // если это директория
                {
                    if (typeListView != TypeListView.Recycle)  // если это не корзина
                    {
                        OpenDir = filePath;
                        if (typeListView == TypeListView.Base) baseListView.DrawDir(OpenDir);
                        else if (typeListView == TypeListView.ThisPC) ChangeTypeListView(TypeListView.Base);
                        folderNavigation.AddPathNavigation(OpenDir);  // история навигации
                    }
                }
                else MessageBox.Show("Невозможно открыть файл/папку!", "Message", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private static ListViewItem FindAncestor(DependencyObject current)
        {
            // находим предка ListViewItem элемента current
            while (current != null)
            {
                if (current is ListViewItem target) return target;
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
            // обновляем директорию при навигации
            OpenDir = path;

            if (path == "Recycle") ChangeTypeListView(TypeListView.Recycle);
            else if (path == "This PC") ChangeTypeListView(TypeListView.ThisPC);
            else if (path == "Quick Access") { }
            else
            {
                if (typeListView != TypeListView.Base) ChangeTypeListView(TypeListView.Base);
                else baseListView.DrawDir(OpenDir);
            }
        }


        // обработчик и BackgroundWorker для комманды Zip и ExtractZip
        private void Zip_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (typeListView == TypeListView.Base)
            {
                string path;
                List<string> files = new List<string>(), dirs = new List<string>();

                foreach (var item in listView.SelectedItems)  // перебираем все выделенные элементы списка
                {
                    path = Path.Combine(OpenDir, ((MyItem)item).Item2);  // узнаём название элемента списка
                    if (File.Exists(path)) files.Add(path);
                    else dirs.Add(path);
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
                MessageBox.Show("Невозможно выполнить архивацию!", "Message", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Worker_DoWork(object? sender, DoWorkEventArgs e)
        {
            // начало работы BackgroundWorker-а
            try
            {
                // для того чтобы можно в другом потоке изменять интерфейс окна
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // запускаем окно для отображения прогресса
                    progressWindow = new ProgressWindow(this, "Zip");
                    progressWindow.Show();
                });
                FileWork.DeleteFile(FileWork.CreateZip(e, worker));  // запускаем архиватор и если возвращаемое значение будет не пустая строка, то удаляем
            }
            catch (UnauthorizedAccessException)
            {
                var answer = MessageBox.Show("Нельзя сохранять в этой директории .zip файлы\nХотите разместить на рабочем столе?", "Error", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (answer == MessageBoxResult.Yes)
                {
                    // меняем название директории у объекта для передачи параметра
                    ((CreateZipArguments)e.Argument).NameDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    FileWork.DeleteFile(FileWork.CreateZip(e, worker));  // запускаем архиватор и если возвращаемое значение будет не пустая строка, то удаляем
                }
            }
        }

        private void Worker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            // меняем значение текстового поля и прогресс-бара
            progressWindow.textBoxValue.Text = e.ProgressPercentage.ToString() + "%";
            progressWindow.progressBar.Value = e.ProgressPercentage;
        }

        private void Worker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            // завершение работы BackgroundWorker-а
            progressWindow.Close();  // закрываем окно для отображения прогресса
            if (e.Error != null) MessageBox.Show("Что-то пошло не так! Не удалось успешно завершить архивацию!", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
            else MessageBox.Show("Архивация прошла успешно!");
            baseListView.UpdateDrawDir();  // обновляем отображение элементов в списке
        }

        public void CloseProgressWindow(string typeWorker)
        {
            if (typeWorker == "Zip") worker.CancelAsync();
            else workerExtract.CancelAsync();
        }


        private void ExtractZip_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // если открытая директория это Base (например: диск E, C, D и тд)
            if (typeListView == TypeListView.Base && listView.SelectedItems.Count == 1)
            {
                string zipPath = Path.Combine(OpenDir, ((MyItem)listView.SelectedItems[0]).Item2);  // получаем название
                if (Path.GetExtension(zipPath) == ".zip")  // если это zip файл
                {
                    if (!worker.IsBusy) workerExtract.RunWorkerAsync(zipPath);  // запуск фонового процесса с передачей аргумента
                    else MessageBox.Show("Подождите пожалуйста, происходит отмена распаковки!");
                    return;
                }
            }
            MessageBox.Show("Невозможно распаковать!", "Message", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void WorkerExtract_DoWork(object? sender, DoWorkEventArgs e)
        {
            // для того чтобы можно в другом потоке изменять интерфейс окна
            Application.Current.Dispatcher.Invoke(() =>
            {
                // запускаем окно для отображения прогресса
                progressWindowExtract = new ProgressWindow(this, "Extract");
                progressWindowExtract.Show();
            });
            FileWork.DeleteDir(FileWork.ExtractZipFile(e, workerExtract));  // запускаем распаковку и если возвращаемое значение будет не пустая строка, то удаляем
        }

        private void WorkerExtract_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            // обновляем данные в интерфейсеы
            progressWindowExtract.textBoxValue.Text = e.ProgressPercentage.ToString() + "%";
            progressWindowExtract.progressBar.Value = e.ProgressPercentage;
        }

        private void WorkerExtract_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            progressWindowExtract.Close();  // закрываем окно для отображения прогресса
            if (e.Error != null) MessageBox.Show("Что-то пошло не так! Не удалось успешно завершить распаковку!", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
            else MessageBox.Show("Распаковка архива прошла успешно!");
            baseListView.UpdateDrawDir();  // обновляем отображение элементов в списке
        }


        // обработчики для изменения названия элемента в ListView
        private void TextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (typeListView == TypeListView.Base)
            {
                RenamePath_Executed(sender, null);  // вызов обработчика для команды
            }
            else
            {
                foreach (var item in listView.Items)
                {
                    if (((MyItem)item).Item2 == ((TextBox)sender).Text)  // если название элемента списка равняется текстовому блоку(который находится на элементе спискаы)
                    {
                        listView.SelectedItem = item;  // устанавливаем выделенный элемент списка т.к. затем вызовится обработчик ListView_MouseDoubleClick()
                        return;
                    }
                }
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // при потере фокуса для элемента TextBox у элемента списка
            if (isChangeFileName)
            {
                TextBox textBox = (TextBox)sender;
                try
                {
                    if (File.Exists(Path.Combine(OpenDir, oldName)))  // если это файл
                    {
                        if (Path.GetExtension(oldName) != Path.GetExtension(textBox.Text))  // если пользователь указал другое расширение 
                        {
                            var answer = MessageBox.Show("Вы действительно хотите изменить расширение файла?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (answer == MessageBoxResult.Yes)
                                FileWork.EditFileName(Path.Combine(OpenDir, oldName), textBox.Text);  // меняем название
                        }
                        else FileWork.EditFileName(Path.Combine(OpenDir, oldName), textBox.Text);
                    }
                    else
                        FileWork.EditDirName(Path.Combine(OpenDir, oldName), textBox.Text);  // если это папка, то меняем название
                }
                catch (Exception) {}
                finally
                {
                    ((TextBox)sender).Style = (Style)(Resources["myEditBoxStyle"]);  // назначаем обратно стиль для TextBox элемента списка
                    baseListView.UpdateDrawDir();  // обновляем содержимое списка
                }
                isChangeFileName = false;
            }
        }


        // обработчик для комманды Properties
        private void Properties_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (listView.SelectedItems.Count == 1)
                FileWork.GetProperties(Path.Combine(OpenDir, ((MyItem)listView.SelectedItem).Item2));
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
                if (sender is TextBox text)  // если был двойнок клик прям по элементу TextBox
                {
                    textBox = text;
                }
                else  // если команда была вызвана с помощью контексного меню
                {
                    // находим контейнер у выделенного элемента списка и TextBox внутри этого контейнера
                    DependencyObject container = listView.ItemContainerGenerator.ContainerFromItem(listView.SelectedItem);
                    TextBox findText = FindVisualChild(container);
                    if (findText != null) textBox = findText;
                }
                textBox.Focus();  // устанавливаем фокус
                textBox.SelectAll();  // выделяем весь текст
                textBox.Style = null;  // снимаем стиль (чтобы можно было написать что-то)
                oldName = textBox.Text;  // запоминаем старое название
                isChangeFileName = true;  // меняем название
            }
            else MessageBox.Show("Нельзя изменить название!", "Message", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void SaveRenamePath_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // если это TextBox из ListView (т.к. они находятся в StackPanel)
            if (Keyboard.FocusedElement is TextBox text && text.Parent is StackPanel) listView.Focus();
        }

        private TextBox FindVisualChild(DependencyObject parent)
        {
            // находим элемент TextBox у элемента parent
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
                    string dirDefaultName = $"new_folder_{DateTime.Now.Hour}_{DateTime.Now.Minute}_{DateTime.Now.Second}";
                    Directory.CreateDirectory(Path.Combine(OpenDir, dirDefaultName));
                    baseListView.UpdateDrawDir();
                }
                catch (Exception) {}
            }
            else MessageBox.Show("Невозможно создать папку!", "Message", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // обработчик для комманды NewFile
        private void NewFile_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (typeListView == TypeListView.Base)
            {
                try
                {
                    string dirdefaultName = $"new_file_{DateTime.Now.Hour}_{DateTime.Now.Minute}_{DateTime.Now.Second}";
                    using (FileStream fs = File.Create(Path.Combine(OpenDir, dirdefaultName))) {}
                    baseListView.UpdateDrawDir();
                }
                catch (Exception) { }
            }
            else MessageBox.Show("Невозможно создать папку!", "Message", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // класс, для передачи этого типа ввиде одного аргумента (для архиватораы)
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

    // класс, для созания команд
    public class FileCommands
    {
        public static RoutedCommand Zip { get; set; }
        public static RoutedCommand ExtractZip { get; set; }
        public static RoutedCommand Properties { get; set; }
        public static RoutedCommand AllSelected { get; set; }
        public static RoutedCommand NoneSelected { get; set; }
        public static RoutedCommand RenamePath { get; set; }
        public static RoutedCommand SaveRenamePath { get; set; }
        public static RoutedCommand NewFolder { get; set; }
        public static RoutedCommand NewFile { get; set; }

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
            NewFile = new RoutedCommand("NewFile", typeof(FileCommands));
        }
    }
}
