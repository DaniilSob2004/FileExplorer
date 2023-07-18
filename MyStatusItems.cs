namespace FileExplorer
{
    public class MyStatusItems
    {
        private static MainWindow parent;

        public static void SetParent(MainWindow parent_)
        {
            parent = parent_;
        }

        public static void UpdateItemsCount(int count)
        {
            parent.statusItems.Text = count.ToString();  // устанавливаем данные о кол-ве файлов/папок в StatusBar
        }

        public static void UpdateItemsSelected(int selected)
        {
            parent.statusItemsSelected.Text = selected.ToString();  // устанавливаем данные о кол-ве выделенных файлов/папок в StatusBar
        }

        public static void UpdateItemsSize(string size)
        {
            parent.statusItemSize.Text = size;  // устанавливаем данные о размере выделенных файлов в StatusBar
        }
    }
}
