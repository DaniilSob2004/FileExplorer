using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static System.Windows.Media.Color;
using System.IO;

namespace FileExplorer
{
    public class MyItem
    {
        public ImageSource Item1 { get; set; }
        public string Item2 { get; set; }
        public string Item3 { get; set; }
        public dynamic Item4 { get; set; }
        public string Item5 { get; set; }
        public string Item6 { get; set; }
        public string Item7 { get; set; }
    }

    public class BaseListView
    {
        public static Brush brush = new SolidColorBrush(FromArgb(0xFF, 0x25, 0x25, 0x25));

        protected DataTemplate imageTextTemplate;
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
            MyStatusItems.UpdateItemsSelected(listView.SelectedItems.Count);

            long size = 0;
            string path;
            foreach (var item in listView.SelectedItems)
            {
                path = System.IO.Path.Combine(parent.OpenDir, ((MyItem)item).Item2);
                if (File.Exists(path))
                    size += FileWork.GetFileSize(path);
            }

            MyStatusItems.UpdateItemsSize(FileWork.FormatBytes(size));
        }

        protected void ClearListItems()
        {
            listView.Items.Clear();
            items.Clear();
        }


        public virtual void StartSettings() { }

        public virtual void DrawDir(string path) 
        {
            MyStatusItems.UpdateItemsCount(items.Count);
        }

        public virtual void UpdateDrawDir() { }
    }
}
