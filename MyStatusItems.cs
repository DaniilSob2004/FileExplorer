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
            parent.statusItems.Text = count.ToString();
        }

        public static void UpdateItemsSelected(int selected)
        {
            parent.statusItemsSelected.Text = selected.ToString();
        }

        public static void UpdateItemsSize(string size)
        {
            parent.statusItemSize.Text = size;
        }
    }
}
