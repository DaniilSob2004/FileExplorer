using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using static System.Environment;
using static System.Windows.Media.Color;

namespace FileExplorer
{
    public class TreeViewPath
    {
        public static Brush brush = new SolidColorBrush(FromArgb(0xFF, 0x1B, 0x1B, 0x1B));
        private static string[] basePaths = { GetFolderPath(SpecialFolder.Desktop), GetFolderPath(SpecialFolder.MyDocuments),
                                              Path.Combine(GetFolderPath(SpecialFolder.UserProfile), "Downloads"),
                                              GetFolderPath(SpecialFolder.MyMusic), GetFolderPath(SpecialFolder.MyPictures),
                                              GetFolderPath(SpecialFolder.MyVideos)};
        private static string[] baseNames = { "Desktop", "Documents", "Downloads", "Music", "Pictures", "Videos" };
        private static List<TreeViewItem> expandedItems = new List<TreeViewItem>();

        private TreeView treeView;
        private MainWindow parent;


        public TreeViewPath(MainWindow parent)
        {
            treeView = (TreeView)parent.FindName("treeView");
            this.parent = parent;
            StartSettings();
        }

        public TreeView TreeView => treeView;


        private void StartSettings()
        {
            DrawBasePaths();
            DrawDisks();
        }


        private void DrawBasePaths()
        {
            string getCurrentDir = Directory.GetCurrentDirectory();
            CreateTreeViewItem(null, "Quick Access", "", Path.Combine(getCurrentDir, "Images", "quick_access.png"), 15, false);
            CreateTreeViewItem(null, "Recycle", "", Path.Combine(getCurrentDir, "Images", "recycle.png"), 15, false);
            CreateTreeViewItem(null, "This PC", "", Path.Combine(getCurrentDir, "Images", "pc.png"), 15, false);

            for (int i = 0; i < basePaths.Length; i++)
                CreateTreeViewItem((TreeViewItem)treeView.Items[2], baseNames[i], basePaths[i], basePaths[i]);
        }

        private void DrawDisks()
        {
            DriveInfo[] drives = DriveInfo.GetDrives();
            for (int i = 0; i < drives.Length; i++)
            {
                if (drives[i].IsReady)
                    CreateTreeViewItem((TreeViewItem)treeView.Items[2], drives[i].Name, drives[i].Name, drives[i].Name);
            }
        }

        public void DrawDir(TreeViewItem item, string path)
        {
            if (Directory.Exists(path))
            {
                try
                {
                    item.Items.Clear();

                    string getCurrentDir = Directory.GetCurrentDirectory();
                    string[] dirs = Directory.GetDirectories(path);
                    foreach (string dir in dirs)
                    {
                        CreateTreeViewItem(item, Path.GetFileName(dir), dir, Path.Combine(getCurrentDir, "Images", "dir.png"), 2, false);
                    }
                }
                catch (UnauthorizedAccessException) { }
            }
            else
            {
                //MessageBox.Show("Такого пути не существует!");
            }
        }

        private void CreateTreeViewItem(TreeViewItem item, string title, string tag, string imagePath, int margin = 2, bool isFindIcon = true)
        {
            TreeViewItem newItem = new TreeViewItem() { Margin = new Thickness(0, margin, 0, 0) };
            StackPanel stackPanel = new StackPanel() { Orientation = Orientation.Horizontal, Height = 23 };

            MyImage? im;
            if (isFindIcon)
            {
                im = PrototypeImage.GetByTag(Path.GetExtension(title));
                if (im == null)
                    im = PrototypeImage.AddImage(new MyImage() { Image = Icon.GetFileIcon(imagePath), Tag = title });    
            }
            else
            {
                string imTag = (tag == "") ? title : "dir";
                im = PrototypeImage.GetByTag(imTag);
                if (im == null)
                    im = PrototypeImage.AddImage(new MyImage() { Image = new BitmapImage(new Uri(imagePath)), Tag = imTag });
            }
            Image image = new Image() { Width = 20, Height = 20, Source = im.Image };

            TextBlock textBlock = new TextBlock()
            {
                Text = title,
                Margin = new Thickness(5, 4, 0, 0),
                Foreground = Brushes.White,
                FontSize = 12,
            };

            stackPanel.Children.Add(image);
            stackPanel.Children.Add(textBlock);

            newItem.Header = stackPanel;
            newItem.Tag = tag;

            newItem.MouseUp += parent.TreeViewItem_MouseUp;
            newItem.MouseEnter += parent.NewItem_MouseEnter;
            newItem.MouseLeave += parent.NewItem_MouseLeave;
            newItem.Expanded += parent.NewItem_Expanded;
            newItem.Collapsed += parent.NewItem_Collapsed;

            if (item == null) treeView.Items.Add(newItem);
            else
            {
                AddPassItem(newItem);
                item.Items.Add(newItem);
            }
        }

        public void UpdateDrawDir()
        {
            GetExpandedTreeViewItems((TreeViewItem)treeView.Items[2]);

            foreach (var item in expandedItems)
            {
                item.Items.Clear();
                DrawDir(item, GetTag(item));
            }

            ClearExpandedItems();
        }


        public void ClearExpandedItems()
        {
            expandedItems.Clear();
        }

        public void GetExpandedTreeViewItems(TreeViewItem parentItem)
        {
            foreach (object item in parentItem.Items)
            {
                TreeViewItem treeViewItem = (TreeViewItem)item;
                if (treeViewItem != null && treeViewItem.IsExpanded)
                {
                    expandedItems.Add(treeViewItem);
                    GetExpandedTreeViewItems(treeViewItem);  // рекурсия для дочерних элементов
                }
            }
        }


        public string GetHeader(TreeViewItem item)
        {
            try
            {
                StackPanel panel = (StackPanel)item.Header;
                return ((TextBlock)panel.Children[1]).Text;
            }
            catch (Exception)
            {
                MessageBox.Show("Не удалось получить header TreeViewItem");
                return "";
            }
        }

        public string GetTag(TreeViewItem item)
        {
            try
            {
                return item.Tag.ToString();
            }
            catch (Exception)
            {
                MessageBox.Show("Не удалось получить tag TreeViewItem");
                return "";
            }
        }

        public void AddPassItem(TreeViewItem item)
        {
            item.Items.Add(new TreeViewItem() { Header = "pass" });
        }
    }
}
