using System.Collections.Generic;

// Паттерн ПРОТОТИП
namespace FileExplorer
{
    public static class PrototypeImage
    {
        private static List<MyImage> items = new List<MyImage>();  // коллекция(хранилище) для объектов MyImage(картинок)

        public static MyImage AddImage(MyImage image)
        {
            // если картинку которую добавляем, нет в хранилище, то добавляем и возвращаем
            if (image != null) items.Add(image);
            return items[items.Count - 1];
        }

        public static MyImage? GetByTag(string tag)
        {
            // возвращаем объект картинки по тэгу, иначе null
            foreach (MyImage image in items)
            {
                if (image.Tag == tag) return image;
            }
            return null;
        }
    }
}
