using System;
using System.Windows;

namespace FileExplorer
{
    public partial class ProgressWindow : Window
    {
        private MainWindow mainWindow;
        private string typeWorker;

        public ProgressWindow(MainWindow mainWindow, string typeWorker)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            this.typeWorker = typeWorker;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            mainWindow.CloseProgressWindow(typeWorker);
            Close();
        }
    }
}
