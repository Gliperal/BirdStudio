using System;
using System.Collections.Generic;

namespace BirdStudio
{
    public class Replay
    {
        public struct ReplayInput
        {
            public int frame;
            public int buttonID;
        }

        private Dictionary<string, List<ReplayInput>> inputsByType;

        /// <exception cref="FormatException"></exception>
        private Replay(string[] lines)
        {
            inputsByType = new Dictionary<string, List<ReplayInput>>();
            if (lines.Length < 17) throw new FormatException("Too few lines.");
            if (lines[0] != "0:") throw new FormatException("Expected 0: on line 0.");
            _loadInputs(" J", lines[1]);
            if (lines[2] != "1:") throw new FormatException("Expected 1: on line 2.");
            _loadInputs(" X", lines[3]);
            if (lines[4] != "2:") throw new FormatException("Expected 2: on line 4.");
            _loadInputs(" G", lines[5]);
            if (lines[6] != "3:") throw new FormatException("Expected 3: on line 6.");
            _loadInputs(" C", lines[7]);
            if (lines[8] != "4:") throw new FormatException("Expected 4: on line 8.");
            _loadInputs(" Q", lines[9]);
            if (lines[10] != "") throw new FormatException("Expected line 10 to be empty.");
            if (lines[11] != "0:") throw new FormatException("Expected 0: on line 11.");
            _loadInputs(" RL", lines[12]);
            if (lines[13] != "1:") throw new FormatException("Expected 1: on line 13.");
            _loadInputs(" UD", lines[14]);
            if (lines[15] != "2:") throw new FormatException("Expected 2: on line 15.");
            _loadInputs(" MN", lines[16]);
        }

        public Replay(string file) : this(System.IO.File.ReadAllLines(file)) {}

        public Replay(string buffer, bool _) : this(buffer.Split('\n')) {}

        private void _loadInputs(string type, string inputsString)
        {
            try
            {
                List<ReplayInput> inputs = new List<ReplayInput>();
                foreach (string input in inputsString.Split('|'))
                {
                    if (input == "")
                        continue;
                    string[] s = input.Split(',');
                    inputs.Add(new ReplayInput
                    {
                        frame = Int32.Parse(s[0]),
                        buttonID = Int32.Parse(s[1])
                    });
                }
                inputsByType[type] = inputs;
            }
            catch
            {
                throw new FormatException("Unable to parse inputs: " + inputsString);
            }
        }

        private string[] TYPES = {" J", " X", " G", " C", " Q", " RL", " UD", " MN"};
        public Replay(List<Press> presses)
        {
            inputsByType = new Dictionary<string, List<ReplayInput>>();
            foreach (string type in TYPES)
            {
                List<ReplayInput> inputs = new List<ReplayInput>();
                foreach (Press press in presses)
                {
                    string buttons = type;
                    int buttonID = buttons.IndexOf(press.button);
                    if (buttonID >= 0)
                        inputs.Add(new ReplayInput
                        {
                            frame = press.frame,
                            buttonID = press.on ? buttonID : 0
                        });
                    // merge two inputs on the same frame (e.g. L off, R on)
                    if (inputs.Count < 2)
                        continue;
                    ReplayInput prev = inputs[inputs.Count - 2];
                    ReplayInput current = inputs[inputs.Count - 1];
                    if (prev.frame == current.frame)
                    {
                        if (prev.buttonID == 0)
                            inputs.RemoveAt(inputs.Count - 2);
                        else if (current.buttonID == 0)
                            inputs.RemoveAt(inputs.Count - 1);
                    }
                }
                inputsByType[type] = inputs;
            }
        }

        // TODO handle IndexOutOfRangeException
        public List<Press> toPresses()
        {
            List<Press> presses = new List<Press>();
            foreach (string type in inputsByType.Keys)
            {
                string buttons = type;
                foreach (ReplayInput input in inputsByType[type])
                    foreach (char button in buttons.Substring(1))
                    {
                        presses.Add(new Press
                        {
                            frame = input.frame,
                            button = button,
                            on = button == buttons[input.buttonID]
                        });
                    }
            }
            return presses;
        }

        private string _writeInputs(string type)
        {
            string result = "";
            foreach (ReplayInput input in inputsByType[type])
                result += input.frame + "," + input.buttonID + "|";
            if (!result.StartsWith("0,"))
                result = "0,0|" + result;
            return result;
        }

        public string writeString()
        {
            List<string> lines = new List<string>();
            lines.Add("0:");
            lines.Add(_writeInputs(" J"));
            lines.Add("1:");
            lines.Add(_writeInputs(" X"));
            lines.Add("2:");
            lines.Add(_writeInputs(" G"));
            lines.Add("3:");
            lines.Add(_writeInputs(" C"));
            lines.Add("4:");
            lines.Add(_writeInputs(" Q"));
            lines.Add("");
            lines.Add("0:");
            lines.Add(_writeInputs(" RL"));
            lines.Add("1:");
            lines.Add(_writeInputs(" UD"));
            lines.Add("2:");
            lines.Add(_writeInputs(" MN"));
            lines.Add("");
            return string.Join('\n', lines);
        }

        public void writeFile(string file)
        {
            System.IO.File.WriteAllText(file, writeString());
        }
    }
}
