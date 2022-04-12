using System;

namespace BirdStudioRefactor
{
    public class TASInputLine
    {
        public int frames;
        public string buttons;

        private TASInputLine(int frames, string buttons)
        {
            this.frames = frames;
            this.buttons = buttons;
        }

        public static TASInputLine from(string line)
        {
            line = line.Trim();
            if (line == "" || line.StartsWith('#') || line.StartsWith('>'))
                return null;
            int split = line.LastIndexOfAny("0123456789".ToCharArray()) + 1;
            // If no frame number is found, split will be at 0.
            string frames = line.Substring(0, split);
            string buttons = line.Substring(split).ToUpper();
            buttons = string.Join("", buttons.Split(','));
            foreach (char c in buttons)
                if (!Char.IsLetter(c))
                    return null;
            if (split == 0)
                return new TASInputLine(0, buttons);
            try
            {
                return new TASInputLine(Int32.Parse(frames), buttons);
            }
            catch { }
            return null;
        }
    }
}
