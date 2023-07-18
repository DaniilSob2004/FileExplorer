using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.IO;

namespace FileExplorer
{
    public class ListViewPath : BaseListView
    {
        private static ListViewPath listViewPath = null;

        private ListViewPath(MainWindow parent) : base(parent) { }

        public static BaseListView GetInstance(MainWindow parent)  // Паттерн ОДИНОЧКА
        {
            if (listViewPath == null) listViewPath = new ListViewPath(parent);
            return listViewPath;
        }


        public override void StartSettings()
        {
            GridView gridView = new GridView() { AllowsColumnReorder = false };

            // imageTextTemplate - это шаблон, для отображения контента в ListView
            GridViewColumn column1 = new GridViewColumn() { Header = "Name", Width = 350, CellTemplate = imageTextTemplate };
            gridView.Columns.Add(column1);

            GridViewColumn column2 = new GridViewColumn() { Header = "Data", Width = 200 };
            column2.DisplayMemberBinding = new Binding("Item3");  // привязка к свойству типа MyItem
            gridView.Columns.Add(column2);

            GridViewColumn column3 = new GridViewColumn() { Header = "Type", Width = 150 };
            column3.DisplayMemberBinding = new Binding("Item4");  // привязка к свойству типа MyItem
            gridView.Columns.Add(column3);

            GridViewColumn column4 = new GridViewColumn() { Header = "Size", Width = 150 };
            column4.DisplayMemberBinding = new Binding("Item5");  // привязка к свойству типа MyItem
            gridView.Columns.Add(column4);

            listView.View = gridView;
        }

        public override void DrawDir(string path)
        {
            if (Directory.Exists(path))
            {
                try
                {
                    ClearListItems();

                    string[] paths = Directory.GetDirectories(path);
                    foreach (string dir in paths)
                        CreateListViewItem(dir);

                    paths = Directory.GetFiles(path);
                    foreach (string file in paths)
                        CreateListViewItem(file);

                    base.DrawDir(path);
                }
                catch (UnauthorizedAccessException) { }
            }
            else
            {
                //MessageBox.Show("Такого пути не существует!");
            }
        }

        public override void UpdateDrawDir()
        {
            DrawDir(parent.OpenDir);
        }

        private void CreateListViewItem(string path)
        {
            try
            {
                MyImage? im = null;
                string getCurrentDir = Directory.GetCurrentDirectory();

                if (File.Exists(path))
                {
                    string extension = Path.GetExtension(path);

                    if (extension != ".exe" && extension != ".lnk")  // если расширение не .exe и .lnk
                    {
                        im = PrototypeImage.GetByTag(extension);  // получаем из хранилища картинок, картинку по расширению
                        // если нет такой, то создаём объект и добавляем в хранилище
                        if (im == null) im = PrototypeImage.AddImage(new MyImage() { Image = Icon.GetFileIcon(path), Tag = extension });
                    }
                    else im = new MyImage { Image = Icon.GetFileIcon(path) };  // создаём объект

                    FileInfo fileInfo = new FileInfo(path);  // добавляем объект в коллекцию MyItem
                    items.Add(new MyItem
                    {
                        Item1 = im.Image,
                        Item2 = Path.GetFileName(path),
                        Item3 = fileInfo.CreationTime.ToString(),
                        Item4 = FileWork.GetFileType(path),
                        Item5 = FileWork.FormatBytes(fileInfo.Length)
                    });
                }
                else
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(path);
                    im = PrototypeImage.GetByTag("dir");  // находим объект картинки
                    if (im == null) im = PrototypeImage.AddImage(new MyImage()
                    {
                        // находим картинку в директории и добавляем
                        Image = new BitmapImage(new Uri(Path.Combine(Directory.GetCurrentDirectory(), "Images", "dir.png"))),
                        Tag = "dir"
                    });
                    items.Add(new MyItem  // добавляем объект в коллекцию MyItem
                    {
                        Item1 = im.Image,
                        Item2 = Path.GetFileName(path),
                        Item3 = dirInfo.CreationTime.ToString(),
                        Item4 = "File folder"
                    });
                }
                listView.Items.Add(items[items.Count - 1]);  // добавляем объект MyItem, в коллекцию ListViewItems
            }
            catch (Exception) { }
        }
    }
}
