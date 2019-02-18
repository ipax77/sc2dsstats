using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace sc2dsstats_rc1
{
    class sc2hex
    {
        public string NAME { get; set; }
        public sc2hex() {
        }

        public sc2hex(string name)
        {
            NAME = name;
        }

        public string UTF8Convert()
        {
            string utf8_name = "";

            //my $player = "\xd0\x94\xd0\xb0\xd0\xbc\xd0\xb8\xd1\x80";
            // K\xc4\xb1l\xc4\xb1\xc3\xa7arslan
            // xxW\\xc3\\x82Rxx

            string hex = Regex.Replace(NAME, @"\\", @"/", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            //string pattern = @"([^\\]+)?\\\\x(..)([^\\]+)?";
            string pattern = @"([^/]+)?/x(..)([^/]+)?";

            Regex regex = new Regex(pattern);
            //Match m = regex.Match(NAME);

            List<byte> encl = new List<byte>();
            UTF8Encoding utf8 = new UTF8Encoding();

            foreach (Match m in Regex.Matches(hex, pattern))
            {
                if (m.Groups[1].Length > 0)
                {
                    utf8_name += m.Groups[1].ToString();
                }

                if (m.Groups[2].Length > 0)
                {
                    encl.Add(Convert.ToByte(m.Groups[2].ToString().ToUpper(), 16));
                }

                if (m.Groups[3].Length > 0)
                {
                    utf8_name += utf8.GetString(encl.ToArray());
                    encl.Clear();
                    utf8_name += m.Groups[3];
                }
                


                //m = m.NextMatch();
            }

            if (encl.Count > 0)
            {
                utf8_name += utf8.GetString(encl.ToArray());
                encl.Clear();
            }

            return utf8_name;
        }

        public string UTF8Convert(string name)
        {
            string utf8_name = "";
            NAME = name;
            utf8_name = UTF8Convert();
            return utf8_name;
        }

    }
}
