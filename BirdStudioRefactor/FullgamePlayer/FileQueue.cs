using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;

namespace BirdStudioRefactor
{
    public class FileQueue : TreeView
    {
        List<TreeViewBranch> children = new List<TreeViewBranch>();

        public void open()
        {
            // TODO
            // List<string> lines = text.Split('\n').ToList();
            // lines = lines.FindAll(line => line.Trim() != "");
        }

        public void addFile()
        {
            /*
            this.AddChild(new TreeViewBranch("Home.tas"));
            this.AddChild(new TreeViewBranch("KeystoneIsles.tas"));
            this.AddChild(new TreeViewBranch("nonexistantfile.tas"));
            this.AddChild(new TreeViewBranch("Briar.tas"));
            */
            children.Add(TreeViewBranch.from("Home.tas"));
            children.Add(TreeViewBranch.from("KeystoneIsles.tas"));
            children.Add(TreeViewBranch.from("nonexistantfile.tas"));
            children.Add(TreeViewBranch.from("Briar.tas"));
            foreach (TreeViewBranch child in children)
                AddChild(child);
        }

        public void removeFile()
        {
            string text = "";
            foreach (TreeViewBranch child in children)
                text += child.toText();
            File.WriteAllText(TreeViewBranch.TAS_FILES_LOCATION + "fullgame.txt", text);
        }

        public void force()
        {
            object selected = this.SelectedItem;
            if (selected is TreeViewBranch)
                ((TreeViewBranch)selected).toggleForced();
        }
    }
}

/*
file01.tas
 2. unnamed branch
file02.tas
 default
  1. this branch
file03.tas
*/
