using System;
using System.Collections.Generic;

namespace BirdStudioRefactor
{
    public class TASInputLine
    {
        public int frames;
        public string buttons;

        public TASInputLine(int frames, string buttons)
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
            catch
            {
                return null;
            }
        }

        public bool Equals(TASInputLine that)
        {
            if (frames != that.frames)
                return false;
            foreach (char button in buttons)
                if (!that.buttons.Contains(button))
                    return false;
            foreach (char button in that.buttons)
                if (!buttons.Contains(button))
                    return false;
            return true;
        }

        public string toText()
        {
            if (buttons.Length == 0)
                return String.Format("{0,4}", frames);
            const string order = TASInputs.BUTTONS;
            List<char> orderedButtons = new List<char>();
            foreach (char c in order)
                if (buttons.Contains(c))
                    orderedButtons.Add(c);
            string buttonsStr = string.Join(",", orderedButtons);
            if (buttonsStr == "")
                return String.Format("{0,4}", frames);
            else
                return String.Format("{0,4},{1}", frames, buttonsStr);
        }

        public static bool isInputLine(string line)
        {
            return from(line) != null;
        }
    }
}
