using System.Collections.Generic;
using System.IO;

namespace FileExplorer
{
    public class FolderNavigation
    {
        const int MaxAmountStoryNavigation = 15;
        private List<string> foldersNavigation;
        private int indNavigation;

        public FolderNavigation()
        {
            foldersNavigation = new List<string>();
            indNavigation = 0;
        }

        public string Back()
        {
            if (indNavigation > 0) indNavigation--;
            return foldersNavigation[indNavigation];
        }

        public string Forward()
        {
            if (indNavigation < foldersNavigation.Count - 1) indNavigation++;
            return foldersNavigation[indNavigation];
        }

        public string Up(string openDir)
        {
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
            if (foldersNavigation.Count >= MaxAmountStoryNavigation)
            {
                foldersNavigation.RemoveAt(0);
            }

            if (foldersNavigation.Count == 0)
            {
                indNavigation = 0;
                foldersNavigation.Add(openDir);
            }
            else if (foldersNavigation[foldersNavigation.Count - 1] != openDir)
            {
                int del = foldersNavigation.Count - 1 - indNavigation;
                for (int i = 0; i < del; i++)
                {
                    foldersNavigation.RemoveAt(foldersNavigation.Count - 1);
                }
                foldersNavigation.Add(openDir);
                indNavigation = foldersNavigation.Count - 1;
            }
        }
    }
}
