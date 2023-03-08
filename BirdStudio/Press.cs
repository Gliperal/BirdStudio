namespace BirdStudio
{
    public struct Press
    {
        public int frame;
        public char button;
        public bool on;

        public static int compareFrames(Press x, Press y)
        {
            return x.frame - y.frame;
        }
    }
}
