namespace BirdStudio
{
    public interface IBranchSection : IEditable
    {
        public string getText();
        public IBranchSection clone();
    }
}
