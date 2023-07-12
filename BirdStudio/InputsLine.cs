using System;
using System.Collections.Generic;

namespace BirdStudio
{
    public class InputsLine
    {
        public int frames;
        public string buttons;

        public InputsLine(int frames, string buttons)
        {
            this.frames = frames;
            this.buttons = buttons;
        }

        public static InputsLine from(string line)
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
                return new InputsLine(0, buttons);
            try
            {
                return new InputsLine(Int32.Parse(frames), buttons);
            }
            catch
            {
                return null;
            }
        }

        public InputsLine reformat(string favoredButtons)
        {
            HashSet<char> newButtons = new HashSet<char>();
            foreach (char c in buttons)
            {
                if (newButtons.Contains(c))
                    newButtons.Remove(c);
                else
                    newButtons.Add(c);
            }
            favoredButtons = favoredButtons.ToUpper();
            if (newButtons.Contains('U') && newButtons.Contains('D'))
            {
                if (favoredButtons.Contains('D'))
                    newButtons.Remove('U');
                else
                    newButtons.Remove('D');
            }
            if (newButtons.Contains('L') && newButtons.Contains('R'))
            {
                if (favoredButtons.Contains('R'))
                    newButtons.Remove('L');
                else
                    newButtons.Remove('R');
            }
            return new InputsLine(frames, string.Join("", newButtons));
        }

        public bool Equals(InputsLine that)
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

        public bool Contains(InputsLine that)
        {
            if (frames < that.frames)
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
            const string order = Inputs.BUTTONS;
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
