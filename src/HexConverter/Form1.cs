using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HexConverter
{
    public partial class Form1 : Form
    {
        List<string> types = new List<string>() { "原始文本", "十六进制", "转义文本" };
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox3.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (EncodingInfo info in Encoding.GetEncodings())
            {
                comboBox1.Items.Add(info.Name);
            }

            foreach (string type in types)
            {
                comboBox2.Items.Add(type);
            }
        }

        private void ComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox3.Text = "";
            comboBox3.Items.Clear();
            foreach (string type in types)
            {
                if (type != comboBox2.Text)
                {
                    comboBox3.Items.Add(type);
                }
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            Encoding encoding = Encoding.Default;
            foreach (EncodingInfo info in Encoding.GetEncodings())
            {
                if (string.Equals(comboBox1.Text, info.Name, StringComparison.OrdinalIgnoreCase))
                {
                    encoding = info.GetEncoding();
                    break;
                }
            }

            string text = textBox1.Text;
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("需要进行转换的文本不能为空！");
                return;
            }

            string textType1 = comboBox2.Text;
            string textType2 = comboBox3.Text;
            if (!types.Contains(textType1) || !types.Contains(textType2))
            {
                MessageBox.Show("请选择原始内容格式与目标文本格式！");
                return;
            }

            try
            {
                //将文本转换为“原始文本”
                if (textType1 == "十六进制")
                {
                    text = text.ReHex(encoding);
                }
                else if (textType1 == "转义文本")
                {
                    text = text.ReExplain(encoding);
                }

                //将文本转换为“指定格式”
                if (textType2 == "原始文本")
                {
                    textBox2.Text = text;
                }
                else if (textType2 == "十六进制")
                {
                    textBox2.Text = text.ToHex(encoding);
                }
                else
                {
                    textBox2.Text = text.ToExplain(encoding);
                }
            }
            catch (Exception exc)
            {
                textBox2.Text = $"转换内容失败：{exc.Message}\r\n{exc.StackTrace}";
            }

        }

        private void Button2_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "文本文件(*.txt)|*.txt|所有文件|*.*",
                DefaultExt = "txt",
                AddExtension = true
            };
            DialogResult result = sfd.ShowDialog();
            if (result == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(sfd.FileName, textBox2.Text);
                }
                catch
                {
                    MessageBox.Show("保存失败！");
                }
            }
        }
    }

    public static class StringExtension
    {
        public static string ToHex(this string text, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;
            return BitConverter.ToString(encoding.GetBytes(text)).Replace('-', ' ');
        }

        public static string ReHex(this string hex, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;
            hex = Regex.Replace(hex, @"[^0-9A-F]", "");
            byte[] buffer = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length / 2; i++)
            {
                buffer[i] = byte.Parse(hex.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
            }
            return encoding.GetString(buffer);
        }

        private static readonly List<char> _controlCharacters = new List<char>()
        {
            '\u0000',
            '\u0001',
            '\u0002',
            '\u0003',
            '\u0004',
            '\u0005',
            '\u0006',
            '\u0007',
            '\u0008',
            '\u0009',
            '\u000A',
            '\u000B',
            '\u000C',
            '\u000D',
            '\u000E',
            '\u000F',
            '\u0010',
            '\u0011',
            '\u0012',
            '\u0013',
            '\u0014',
            '\u0015',
            '\u0016',
            '\u0017',
            '\u0018',
            '\u0019',
            '\u001A',
            '\u001B',
            '\u001C',
            '\u001D',
            '\u001E',
            '\u001F',
            '\u007F'
        };

        public static string ToExplain(this string text, Encoding encoding = null, List<char> include = null, string format = "#{0}'")
        {
            encoding = encoding ?? Encoding.UTF8;
            include = include ?? _controlCharacters;
            List<char> chars = text.ToCharArray().ToList();
            for (int i = chars.Count - 1; i >= 0; i--)
            {
                if (include.Contains(chars[i]))
                {
                    string temp = string.Format(format, new string(chars[i], 1).ToHex(encoding));
                    chars.RemoveAt(i);
                    chars.InsertRange(i, temp);
                }
            }
            return new string(chars.ToArray());
        }

        public static string ReExplain(this string explain, Encoding encoding = null, string regexStr = "#([0-9A-F]{2})'")
        {
            encoding = encoding ?? Encoding.UTF8;
            Dictionary<string, string> dicRep = new Dictionary<string, string>();
            Regex regex = new Regex(regexStr);
            MatchCollection mc = regex.Matches(explain);
            for (int i = 0; i < mc.Count; i++)
            {
                if (!dicRep.ContainsKey(mc[i].Value))
                {
                    dicRep.Add(mc[i].Value, mc[i].Groups[1].Value.ReHex(encoding));
                }
            }
            foreach (KeyValuePair<string, string> item in dicRep)
            {
                explain = explain.Replace(item.Key, item.Value);
            }

            return explain;
        }
    }
}
