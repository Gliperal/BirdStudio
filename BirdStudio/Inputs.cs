﻿using System.Collections.Generic;

namespace BirdStudio
{
    public class Inputs
    {
        public const string BUTTONS = "RLUDJXGCQMN";

        private List<InputsLine> inputLines;
        private int frameCount;
        private List<int> startingFrames; // 1 larger in size than inputLines

        public Inputs(string text)
        {
            inputLines = new List<InputsLine>();
            string[] lines = text.Split('\n');
            foreach (string line in lines)
                inputLines.Add(InputsLine.from(line));
            countFrames();
        }

        public Inputs(List<Press> presses)
        {
            inputLines = new List<InputsLine>();
            presses.Sort(Press.compareFrames);
            HashSet<char> state = new HashSet<char>();
            int frame = 0;
            foreach (Press press in presses)
            {
                if (press.frame > frame)
                {
                    inputLines.Add(new InputsLine(press.frame - frame, string.Join("", state)));
                    frame = press.frame;
                }
                if (press.on)
                    state.Add(press.button);
                else
                    state.Remove(press.button);
            }
            inputLines.Add(new InputsLine(1, string.Join("", state)));
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
            for (int i = 0; i < inputLines.Count; i++)
            {
                InputsLine inputLine = inputLines[i];
                if (inputLine == null)
                    continue;
                // ignore 0,... lines that aren't at the end
                if (
                    inputLine.frames == 0 &&
                    inputLines.FindIndex(i + 1, inputLines.Count - i - 1, x => x != null) != -1
                )
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

        public List<InputsLine> getInputLines()
        {
            return inputLines;
        }

        public string toText(string stage = null, int rerecords = 0)
        {
            string text = "";
            foreach (InputsLine inputLine in inputLines)
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
            // if frame is off the end, go one beyond last input line
            int x = inputLines.FindLastIndex(line => line != null);
            if (x < inputLines.Count - 1)
                x++;
            return new int[] { x, frame - startingFrames[x] };
        }

        public int totalFrames()
        {
            return frameCount;
        }
    }
}
