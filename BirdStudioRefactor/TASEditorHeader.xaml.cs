using System;
using System.Security;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;

namespace BirdStudioRefactor
{
    /// <summary>
    /// Interaction logic for TASEditorHeader.xaml
    /// </summary>
    public partial class TASEditorHeader : UserControl
    {
        private string _getAttribute(XmlAttributeCollection attributes, string name, string ifNotExists)
        {
            XmlNode attribute = attributes.GetNamedItem(name);
            if (attribute == null)
                return ifNotExists;
            return attribute.InnerText;
        }

        public TASEditorHeader(XmlAttributeCollection attributes)
        {
            InitializeComponent();
            stageInput.Text = _getAttribute(attributes, "stage", "");
            spawnInput.Text = _getAttribute(attributes, "spawn", "");
            rerecordsInput.Text = _getAttribute(attributes, "rerecords", "0");
            rerecordsInput.PreviewTextInput += Rerecords_PreviewTextInput;
        }

        public string toXml(string innerXml)
        {
            string xml = "<tas stage=\"" + SecurityElement.Escape(stage()) + "\"";
            if (rerecords() != 0)
                xml += " rerecords=\"" + rerecords() + "\"";
            if (spawn() != null)
                xml += " spawn=\"" + spawnInput.Text + "\"";
            return xml + ">" + innerXml + "</tas>";
        }

        public string stage()
        {
            return stageInput.Text;
        }

        public float[] spawn()
        {
            try
            {
                string[] coords = spawnInput.Text.Split(',');
                if (coords.Length != 2)
                    return null;
                float x = float.Parse(coords[0].Trim());
                float y = float.Parse(coords[0].Trim());
                return new float[] { x, y };
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public int rerecords()
        {
            try
            {
                return Int32.Parse(rerecordsInput.Text);
            }
            catch (Exception ex)
            {
                if (ex is FormatException || ex is OverflowException)
                    return 0;
                throw;
            }
        }

        public void incrementRerecords()
        {
            int value = Int32.Parse(rerecordsInput.Text);
            rerecordsInput.Text = (value + 1).ToString();
        }

        private void Rerecords_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                Int32.Parse(e.Text);
            }
            catch (Exception ex)
            {
                if (ex is FormatException || ex is OverflowException)
                    e.Handled = true;
                else
                    throw;
            }
        }
    }
}
