using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Shell32;

namespace FileExplorer
{
    public class ListViewRecycle : BaseListView
    {
        private static ListViewRecycle listViewRecycle = null;

        private ListViewRecycle(MainWindow parent) : base(parent) { }

        public static BaseListView GetInstance(MainWindow parent)
        {
            if (listViewRecycle == null)
            {
                listViewRecycle = new ListViewRecycle(parent);
            }
            return listViewRecycle;
        }


        public override void StartSettings()
        {
            GridView gridView = new GridView() { AllowsColumnReorder = false };

            GridViewColumn column1 = new GridViewColumn() { Header = "Name", Width = 300, CellTemplate = imageTextTemplate };
            gridView.Columns.Add(column1);

            GridViewColumn column2 = new GridViewColumn() { Header = "Original Location", Width = 250 };
            column2.DisplayMemberBinding = new Binding("Item3");
            gridView.Columns.Add(column2);

            GridViewColumn column3 = new GridViewColumn() { Header = "Data Deleted", Width = 150 };
            column3.DisplayMemberBinding = new Binding("Item4");
            gridView.Columns.Add(column3);

            GridViewColumn column4 = new GridViewColumn() { Header = "Size", Width = 120 };
            column4.DisplayMemberBinding = new Binding("Item5");
            gridView.Columns.Add(column4);

            GridViewColumn column5 = new GridViewColumn() { Header = "Item type", Width = 150 };
            column5.DisplayMemberBinding = new Binding("Item6");
            gridView.Columns.Add(column5);

            GridViewColumn column6 = new GridViewColumn() { Header = "Data modified", Width = 150 };
            column6.DisplayMemberBinding = new Binding("Item7");
            gridView.Columns.Add(column6);

            listView.View = gridView;
        }

        public override void DrawDir(string path)
        {
            ClearListItems();

            Shell shell = new Shell();
            Folder recycleBin = shell.NameSpace(10);
            foreach (FolderItem2 f in recycleBin.Items())
            {
                CreateListViewItem(f, recycleBin.GetDetailsOf(f, 2), recycleBin.GetDetailsOf(f, 3));
            }

            Marshal.FinalReleaseComObject(shell);
            base.DrawDir(path);
        }

        public override void UpdateDrawDir()
        {
            DrawDir("");
        }

        private void CreateListViewItem(FolderItem2 f, string delDate, string size)
        {
            try
            {
                MyImage? im;

                if (f.IsFolder)
                {
                    string imagePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Images", "dir.png");
                    im = PrototypeImage.GetByTag("dir");
                    if (im == null) im = PrototypeImage.AddImage(new MyImage() { Image = new BitmapImage(new Uri(imagePath)), Tag = "dir" });
                }
                else
                {
                    string extension = System.IO.Path.GetExtension(f.Name);
                    if (extension != ".exe" && extension != ".lnk")
                    {
                        im = PrototypeImage.GetByTag(extension);
                        if (im == null) im = PrototypeImage.AddImage(new MyImage() { Image = Icon.GetFileIcon(System.IO.Path.Combine(f.Path, f.Name)), Tag = extension });
                    }
                    else im = new MyImage { Image = Icon.GetFileIcon(System.IO.Path.Combine(f.Path, f.Name)) };
                }

                items.Add(new MyItem
                {
                    Item1 = im.Image,
                    Item2 = f.Name,
                    Item3 = f.ExtendedProperty("{9B174B33-40FF-11D2-A27E-00C04FC30871}2"),
                    Item4 = delDate,
                    Item5 = size,
                    Item6 = f.Type.ToString(),
                    Item7 = f.ModifyDate.ToString()
                });
                listView.Items.Add(items[items.Count - 1]);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        // Некоторые флаги для API-функции SHEmptyRecycleBin:
        const int SHERB_NOCONFIRMATION = 0x00000001;  // не отображать диалог с уведомлением об удалении объектов
        const int SHERB_NOPROGRESSUI = 0x00000002;  // не отображать диалог с индикатором прогресса
        const int SHERB_NOSOUND = 0x00000004;  // когда операция закончится, не проигрывать звук

        // API-функция очистки корзины
        [DllImport("shell32.dll")]
        static extern int SHEmptyRecycleBin(IntPtr hWnd, string pszRootPath, uint dwFlags);

        // Перечисление и API-функция для открытия файлов
        public enum ShowCommands : int
        {
            SW_HIDE = 0,
            SW_SHOWNORMAL = 1,
            SW_NORMAL = 1,
            SW_SHOWMINIMIZED = 2,
            SW_SHOWMAXIMIZED = 3,
            SW_MAXIMIZE = 3,
            SW_SHOWNOACTIVATE = 4,
            SW_SHOW = 5,
            SW_MINIMIZE = 6,
            SW_SHOWMINNOACTIVE = 7,
            SW_SHOWNA = 8,
            SW_RESTORE = 9,
            SW_SHOWDEFAULT = 10,
            SW_FORCEMINIMIZE = 11,
        }
        [DllImport("shell32.dll")]
        static extern IntPtr ShellExecute(IntPtr hwnd, string lpOperation, string lpFile, string lpParameters, string lpDirectory, ShowCommands nShowCmd);
        Shell shell;
    }
}
