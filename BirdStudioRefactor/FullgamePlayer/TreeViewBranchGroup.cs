using System.Collections.Generic;

namespace BirdStudioRefactor
{
    public class TreeViewBranchGroup
    {
        // TODO private
        public TreeViewBranch parent;
        public BranchGroup branchGroup;
        public List<TreeViewBranch> branches;
        public int defaultBranch;
        public int forcedBranch = -1;

        public TreeViewBranchGroup(BranchGroup branchGroup, TreeViewBranch parent)
        {
            this.parent = parent;
            this.branchGroup = branchGroup;
            defaultBranch = branchGroup.activeBranch;
            branches = new List<TreeViewBranch>();
            for (int i = 0; i < branchGroup.branches.Count; i++)
            {
                Branch b = branchGroup.branches[i];
                string hdr = (i + 1) + ". " + b.getName();
                TreeViewBranch child = new TreeViewBranch(this, b.getName(), hdr, b);
                branches.Add(child);
            }
            // TODO do better
            foreach (TreeViewBranch child in branches)
                child._updateColors();
        }

        public TreeViewBranch activeBranch()
        {
            return branches[(forcedBranch != -1) ? forcedBranch : defaultBranch];
        }

        public bool isDefaultBranch(TreeViewBranch branch)
        {
            return branch == branches[defaultBranch];
        }

        public bool isForcedBranch(TreeViewBranch branch)
        {
            return (forcedBranch != -1) && branch == branches[forcedBranch];
        }

        public void force(TreeViewBranch target, bool on)
        {
            int i = (target != null) ? branches.IndexOf(target) : forcedBranch;
            if ((forcedBranch == i) == on)
                return;
            // TODO remove oldForcedBranch; just use forcedBranch
            int oldForcedBranch = forcedBranch;
            if (oldForcedBranch == -1)
                branches[defaultBranch].setActive(false);
            else
            {
                branches[oldForcedBranch].setActive(false);
                foreach (TreeViewBranchGroup group in branches[oldForcedBranch].groups)
                    group.force(null, false);
            }
            // Update forced branch after deactivating the old branch, so that
            // updates will cascade correctly
            // TODO remove newForcedBranch; just use forcedBranch
            int newForcedBranch = on ? i : -1;
            forcedBranch = newForcedBranch;
            if (newForcedBranch == -1)
            {
                branchGroup.activeBranch = defaultBranch;
                if (parent.active)
                    branches[defaultBranch].setActive(true);
            }
            else
            {
                branchGroup.activeBranch = newForcedBranch;
                branches[newForcedBranch].setActive(true);
                if (parent.parent != null)
                    parent.parent.force(parent, true);
            }
        }
    }
}
