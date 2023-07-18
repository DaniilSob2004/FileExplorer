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
        public static Brush brush = new SolidColorBrush(FromArgb(0xFF, 0x1B, 0x1B, 0x1B));  // кисть по умолчанию

        // массив путей к основным папкам (Desktop, Documents, Downloads, Music, Pictures, Videos)
        private static string[] basePaths = { GetFolderPath(SpecialFolder.Desktop),
                                              GetFolderPath(SpecialFolder.MyDocuments),
                                              Path.Combine(GetFolderPath(SpecialFolder.UserProfile), "Downloads"),
                                              GetFolderPath(SpecialFolder.MyMusic),
                                              GetFolderPath(SpecialFolder.MyPictures),
                                              GetFolderPath(SpecialFolder.MyVideos)};

        private static string[] baseNames = { "Desktop", "Documents", "Downloads", "Music", "Pictures", "Videos" };  // основные названия папок
        private static List<TreeViewItem> expandedItems = new List<TreeViewItem>();  // коллекция раскрытых в дереве элементов TreeViewItem

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
            DrawBasePaths();  // отображаем дерево основных папок
            DrawDisks();  // отображаем дерево дисков на ПК
        }


        private void DrawBasePaths()
        {
            // создаём верхнюю иерархию дерева: Quick Access, Recycle, и This PC
            string getCurrentDir = Directory.GetCurrentDirectory();
            CreateTreeViewItem(null, "Quick Access", "", Path.Combine(getCurrentDir, "Images", "quick_access.png"), 15, false);
            CreateTreeViewItem(null, "Recycle", "", Path.Combine(getCurrentDir, "Images", "recycle.png"), 15, false);
            CreateTreeViewItem(null, "This PC", "", Path.Combine(getCurrentDir, "Images", "pc.png"), 15, false);

            // создаём поддерево с основными папками в дереве This PC
            for (int i = 0; i < basePaths.Length; i++)
                CreateTreeViewItem((TreeViewItem)treeView.Items[2], baseNames[i], basePaths[i], basePaths[i]);
        }

        private void DrawDisks()
        {
            // создаём поддерево с дисками в дереве This PC
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
                    item.Items.Clear();  // очищаем элементы дерева

                    foreach (string dir in Directory.GetDirectories(path))
                        CreateTreeViewItem(item, Path.GetFileName(dir), dir, Path.Combine(Directory.GetCurrentDirectory(), "Images", "dir.png"), 2, false);
                }
                catch (UnauthorizedAccessException) { }
            }
        }

        private void CreateTreeViewItem(TreeViewItem item, string title, string tag, string imagePath, int margin = 2, bool isFindIcon = true)
        {
            StackPanel stackPanel = new StackPanel() { Orientation = Orientation.Horizontal, Height = 23 };  // создаём контейнер для элемента дерева

            MyImage? im;
            if (isFindIcon)  // если нужно найти картинку
            {
                im = PrototypeImage.GetByTag(Path.GetExtension(title));
                if (im == null)  // если картинки в хранилище нет, то создаём и добавляем
                    im = PrototypeImage.AddImage(new MyImage() { Image = Icon.GetFileIcon(imagePath), Tag = title });    
            }
            else
            {
                string imTag = (tag == "") ? title : "dir";  // если тэг не пустой, значит это директория(обычная папка)
                im = PrototypeImage.GetByTag(imTag);
                if (im == null)  // если картинки в хранилище нет, то создаём и добавляем
                    im = PrototypeImage.AddImage(new MyImage() { Image = new BitmapImage(new Uri(imagePath)), Tag = imTag });
            }
            Image image = new Image() { Width = 20, Height = 20, Source = im.Image };  // создаём объект Image, в Source передаём ссылку из объекта MyImages

            // создаём текстовый блок, для вывода названия в элементе дерева
            TextBlock textBlock = new TextBlock()
            {
                Text = title,
                Margin = new Thickness(5, 4, 0, 0),
                Foreground = Brushes.White,
                FontSize = 12,
            };

            // добавляем в контейнер картинку и текст
            stackPanel.Children.Add(image);
            stackPanel.Children.Add(textBlock);

            // создаём элемент дерева, в Header передаём контейнер
            TreeViewItem newItem = new TreeViewItem()
            { 
                Header = stackPanel,
                Tag = tag,
                Margin = new Thickness(0, margin, 0, 0)
            };

            newItem.MouseUp += parent.TreeViewItem_MouseUp;
            newItem.MouseEnter += parent.NewItem_MouseEnter;
            newItem.MouseLeave += parent.NewItem_MouseLeave;
            newItem.Expanded += parent.NewItem_Expanded;
            newItem.Collapsed += parent.NewItem_Collapsed;

            // если элемент дерева в который добавляем элемент равняется null, значит это корень дерева
            if (item == null) treeView.Items.Add(newItem);
            else
            {
                AddPassItem(newItem);  // добавляем заглушку в элемент дерева
                item.Items.Add(newItem);  // добавление элемента в дерево
            }
        }

        public void UpdateDrawDir()
        {
            GetExpandedTreeViewItems((TreeViewItem)treeView.Items[2]);  // получение всех раскрытых элементов дерева

            foreach (var item in expandedItems)
            {
                item.Items.Clear();  // очищаем
                DrawDir(item, GetTag(item));  // отображаем снова
            }

            ClearExpandedItems();  // очищаем коллекцию раскрытых элементов
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
                if (treeViewItem != null && treeViewItem.IsExpanded)  // если элемент есть и он раскрыт, то добавляем в коллекцию
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
            item.Items.Add(new TreeViewItem() { Header = "pass" });  // добавляение заглушки(один элемент, чтобы было видно что элементы внутри элемента есть)
        }
    }
}
