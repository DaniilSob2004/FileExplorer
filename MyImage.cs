using System.Windows.Media;

namespace FileExplorer
{
    public class MyImage
    {
        public ImageSource Image { get; set; }  // объект для хранения картинки
        public string Tag { get; set; }  // для получения доступа к картинки через тэг
    }
}
