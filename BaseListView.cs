using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static System.Windows.Media.Color;
using System.IO;

namespace FileExplorer
{
    // тип данных для отображения контента в элементе списка ListView
    public class MyItem
    {
        public ImageSource Item1 { get; set; }  // картинка
        public string Item2 { get; set; }  // название
        public string Item3 { get; set; }
        public dynamic Item4 { get; set; }
        public string Item5 { get; set; }
        public string Item6 { get; set; }
        public string Item7 { get; set; }
    }

    // абстрактный класс всех типов для отображения контента в ListView
    public class BaseListView
    {
        public static Brush brush = new SolidColorBrush(FromArgb(0xFF, 0x25, 0x25, 0x25));

        protected DataTemplate imageTextTemplate;  // шаблон для отображения первого столбика (картинки и текстового поля)
        protected ListView listView;
        protected MainWindow parent;
        protected List<MyItem> items;

        public BaseListView(MainWindow parent)
        {
            listView = (ListView)parent.FindName("listView");
            this.parent = parent;
            imageTextTemplate = (DataTemplate)listView.Resources["imageTextTemplate"];
            items = new List<MyItem>();
        }

        public void SelectedItems()
        {
            MyStatusItems.UpdateItemsSelected(listView.SelectedItems.Count);  // обновляем данные(кол-во файлов/папок) в Statusbar

            long size = 0;
            string path;
            foreach (var item in listView.SelectedItems)
            {
                path = Path.Combine(parent.OpenDir, ((MyItem)item).Item2);
                if (File.Exists(path))  // если это файл
                    size += FileWork.GetFileSize(path);  // суммируем размер файла
            }

            MyStatusItems.UpdateItemsSize(FileWork.FormatBytes(size));  // обновляем данные(размер выделенных файлов) в Statusbar
        }

        protected void ClearListItems()
        {
            listView.Items.Clear();  // очищаем коллекцию и ListView
            items.Clear();
        }


        public virtual void StartSettings() { }

        public virtual void DrawDir(string path) 
        {
            MyStatusItems.UpdateItemsCount(items.Count);  // обновляем данные в Statusbar
        }

        public virtual void UpdateDrawDir() { }
    }
}
