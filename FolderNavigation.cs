using System.Collections.Generic;
using System.IO;

namespace FileExplorer
{
    public class FolderNavigation
    {
        const int MaxAmountStoryNavigation = 15;  // максимальное кол-во путей, которое будет храниться в истории просмотров
        private List<string> foldersNavigation;  // пути, которые посетил пользователь
        private int indNavigation;  // на каком пути находится пользователь, по отношению к коллекции foldersNavigation

        public FolderNavigation()
        {
            foldersNavigation = new List<string>();
            indNavigation = 0;
        }

        public string Back()
        {
            // навигация назад
            if (indNavigation > 0) indNavigation--;
            return foldersNavigation[indNavigation];
        }

        public string Forward()
        {
            // навигация вперёд
            if (indNavigation < foldersNavigation.Count - 1) indNavigation++;
            return foldersNavigation[indNavigation];
        }

        public string Up(string openDir)
        {
            // навигация вверхнюю директорию
            DirectoryInfo? info = Directory.GetParent(openDir);
            if (openDir != "This PC" && openDir != "Recycle" && info != null)
            {
                AddPathNavigation(info.FullName);
                return info.FullName;
            }
            else if (openDir != "This PC")
            {
                AddPathNavigation("This PC");
                return "This PC";
            }
            return "";
        }

        public void AddPathNavigation(string openDir)
        {
            // добавление пути в коллекцию навигации

            if (foldersNavigation.Count >= MaxAmountStoryNavigation)
            {
                foldersNavigation.RemoveAt(0);
            }

            if (foldersNavigation.Count == 0)
            {
                indNavigation = 0;
                foldersNavigation.Add(openDir);
            }
            else if (foldersNavigation[foldersNavigation.Count - 1] != openDir)  // если это путь, который не последний в коллекции
            {
                int del = foldersNavigation.Count - 1 - indNavigation;  // находим разницу от индекса, до конца коллекции
                for (int i = 0; i < del; i++)
                {
                    foldersNavigation.RemoveAt(foldersNavigation.Count - 1);  // и удаляем те пути, которые были после индекса
                }
                foldersNavigation.Add(openDir);  // добавляем путь в конец коллекции
                indNavigation = foldersNavigation.Count - 1;  // переставляем индекс
            }
        }
    }
}
