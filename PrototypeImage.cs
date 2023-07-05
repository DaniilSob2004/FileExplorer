using System.Collections.Generic;

namespace FileExplorer
{
    public static class PrototypeImage
    {
        private static List<MyImage> items = new List<MyImage>();

        public static MyImage AddImage(MyImage image)
        {
            if (image != null) items.Add(image);
            return items[items.Count - 1];
        }

        public static MyImage? GetByTag(string tag)
        {
            foreach (MyImage image in items)
            {
                if (image.Tag == tag) return image;
            }
            return null;
        }
    }
}
