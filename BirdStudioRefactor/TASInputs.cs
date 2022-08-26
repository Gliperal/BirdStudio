using System.Collections.Generic;

namespace BirdStudioRefactor
{
    class TASInputs
    {
        public const string BUTTONS = "RLUDJXGCQMN";

        private List<TASInputLine> inputLines;
        private int frameCount;
        private List<int> startingFrames; // 1 larger in size than inputLines

        public TASInputs(string text)
        {
            inputLines = new List<TASInputLine>();
            string[] lines = text.Split('\n');
            foreach (string line in lines)
                inputLines.Add(TASInputLine.from(line));
            countFrames();
        }

        public TASInputs(List<Press> presses)
        {
            inputLines = new List<TASInputLine>();
            presses.Sort(Press.compareFrames);
            HashSet<char> state = new HashSet<char>();
            int frame = 0;
            foreach (Press press in presses)
            {
                if (press.frame > frame)
                {
                    inputLines.Add(new TASInputLine(press.frame - frame, string.Join("", state)));
                    frame = press.frame;
                }
                if (press.on)
                    state.Add(press.button);
                else
                    state.Remove(press.button);
            }
            inputLines.Add(new TASInputLine(1, string.Join("", state)));
        }

        private void countFrames()
        {
            startingFrames = new List<int>();
            int frame = 0;
            for (int i = 0; i < inputLines.Count; i++)
            {
                startingFrames.Add(frame);
                if (inputLines[i] != null)
                    frame += inputLines[i].frames;
            }
            startingFrames.Add(frame);
            frameCount = frame;
        }

        public List<Press> toPresses()
        {
            List<Press> presses = new List<Press>();
            HashSet<char> state = new HashSet<char>();
            int frame = 0;
            foreach (TASInputLine inputLine in inputLines)
            {
                if (inputLine == null)
                    continue;
                foreach (char button in BUTTONS)
                {
                    bool isOn = inputLine.buttons.Contains(button);
                    bool wasOn = state.Contains(button);
                    if (isOn != wasOn)
                    {
                        presses.Add(new Press
                        {
                            frame = frame,
                            button = button,
                            on = isOn
                        });
                        if (isOn)
                            state.Add(button);
                        else
                            state.Remove(button);
                    }
                }
                frame += inputLine.frames;
            }
            return presses;
        }

        public List<TASInputLine> getInputLines()
        {
            return inputLines;
        }

        public string toText(string stage = null, int rerecords = 0)
        {
            string text = "";
            foreach (TASInputLine inputLine in inputLines)
                text += inputLine.toText() + '\n';
            if (stage != null)
                text = ">stage " + stage + "\n>rerecords " + rerecords + "\n\n" + text;
            return text;
        }

        public int startingFrameForLine(int lineNumber)
        {
            if (lineNumber < 0 || lineNumber >= inputLines.Count)
                return -1;
            return startingFrames[lineNumber];
        }

        public int endingFrameForLine(int lineNumber)
        {
            if (lineNumber < 0 || lineNumber >= inputLines.Count)
                return -1;
            return startingFrames[lineNumber + 1];
        }

        public int[] locateFrame(int frame)
        {
            for (int i = 0; i < inputLines.Count; i++)
            {
                int lineStartFrame = startingFrames[i];
                int lineEndFrame = startingFrames[i + 1];
                if (frame <= lineEndFrame)
                    return new int[] { i, frame - lineStartFrame };
            }
            return new int[] { inputLines.Count - 1, frame - startingFrames[inputLines.Count - 1] };
        }

        public int totalFrames()
        {
            return frameCount;
        }
    }
}
