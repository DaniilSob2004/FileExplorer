using System;
using System.Windows.Controls;
using System.Windows.Data;
using System.IO;

namespace FileExplorer
{
    public class ListViewPC : BaseListView
    {
        private static ListViewPC listViewPC = null;

        private ListViewPC(MainWindow parent) : base(parent)
        {
            StartSettings();
        }

        public static BaseListView GetInstance(MainWindow parent)
        {
            if (listViewPC == null)
            {
                listViewPC = new ListViewPC(parent);
            }
            return listViewPC;
        }


        public override void StartSettings()
        {
            GridView gridView = new GridView() { AllowsColumnReorder = false };

            GridViewColumn column1 = new GridViewColumn() { Header = "Name", Width = 250, CellTemplate = imageTextTemplate };
            gridView.Columns.Add(column1);

            GridViewColumn column2 = new GridViewColumn() { Header = "Type", Width = 150 };
            column2.DisplayMemberBinding = new Binding("Item3");
            gridView.Columns.Add(column2);

            GridViewColumn column3 = new GridViewColumn() { Header = "Total Size", Width = 150 };
            column3.DisplayMemberBinding = new Binding("Item4");
            gridView.Columns.Add(column3);

            GridViewColumn column4 = new GridViewColumn() { Header = "Free Space", Width = 150 };
            column4.DisplayMemberBinding = new Binding("Item5");
            gridView.Columns.Add(column4);

            listView.View = gridView;
        }

        public override void DrawDir(string path)
        {
            ClearListItems();

            foreach (var d in DriveInfo.GetDrives())
            {
                if (d.IsReady)
                    CreateListViewItem(d);
            }
            base.DrawDir(path);
        }

        public override void UpdateDrawDir()
        {
            DrawDir("");
        }

        private void CreateListViewItem(DriveInfo drive)
        {
            try
            {
                items.Add(new MyItem
                {
                    Item1 = PrototypeImage.GetByTag(drive.Name).Image,
                    Item2 = drive.Name,
                    Item3 = drive.DriveType.ToString(),
                    Item4 = FileWork.FormatBytes(drive.TotalSize),
                    Item5 = FileWork.FormatBytes(drive.AvailableFreeSpace)
                });
                listView.Items.Add(items[items.Count - 1]);
            }
            catch (Exception) { }
        }
    }
}
