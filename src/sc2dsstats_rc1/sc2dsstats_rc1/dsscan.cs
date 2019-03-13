using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace sc2dsstats_rc1
{
    class dsscan
    {
        public string REPLAY_PATH { get; set; }
        public List<string> REPLAY_LIST { get; set; }
        public string STATS_FILE { get; set; }
        public MainWindow MW { get; set; }
        public int NEWREP { get; set; }
        public double FS { get; set; }
        public string FREESPACE { get; set; }
        public string ESTTIME { get; set; }
        public string ESTSPACE { get; set; }
        public int TOTAL { get; set; }

        public dsscan()
        {

        }

        public dsscan(string replay_path, string stats_file, MainWindow mw)
        {
            REPLAY_PATH = replay_path;
            REPLAY_LIST = new List<string>();
            REPLAY_LIST.Add(REPLAY_PATH);
            STATS_FILE = stats_file;
            MW = mw;
        }
        public dsscan(List<string> replay_list, string stats_file, MainWindow mw)
        {
            REPLAY_LIST = new List<string>(replay_list);
            STATS_FILE = stats_file;
            MW = mw;
        }

        public void Scan()
        {

            string dir = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            string drive = "C:\\";
            var appSettings = ConfigurationManager.AppSettings;
            Hashtable dsreplays = new Hashtable();
            Hashtable dsskip = new Hashtable();

            Regex rx = new Regex(@"(\w:\\)");

            MatchCollection matches = rx.Matches(dir);
            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                    drive = match.Value.ToString();
            }

            /// MessageBox.Show(drive);

            string Laufwerksbuchstabe = drive;
            DriveInfo[] Drives = DriveInfo.GetDrives();
            DriveInfo Drive = Drives.Where(x => x.Name == Laufwerksbuchstabe).SingleOrDefault();

            /// MessageBox.Show(Drive.TotalFreeSpace.ToString());
            FS = Drive.TotalFreeSpace;

            int i = 0;
            int newrep = 0;

            if (File.Exists(STATS_FILE))
            {

                string line;
                string pattern = @"^(\d+); ([^;]+);";
                ///string pattern = @"^(\d+);";

                try
                {
                    System.IO.StreamReader file = new System.IO.StreamReader(STATS_FILE);
                    while ((line = file.ReadLine()) != null)
                    {
                        foreach (Match m in Regex.Matches(line, pattern))
                        {
                            string value1 = m.Groups[2].ToString();
                            if (dsreplays.ContainsKey(value1))
                            {


                            }
                            else
                            {
                                dsreplays.Add(value1, "1");

                            }
                        }
                    }

                    file.Close();
                }
                catch (System.IO.IOException)
                {
                }
            }

            if (File.Exists(appSettings["SKIP_FILE"]))
            {
                string line;

                try
                {
                    System.IO.StreamReader file = new System.IO.StreamReader(appSettings["SKIP_FILE"]);
                    while ((line = file.ReadLine()) != null)
                    {

                        if (line == "") continue;
                        //dsskip.Add(line + ".SC2Replay", 1);
                        dsskip.Add(line, 1);
                    }

                    file.Close();
                }
                catch (System.IO.IOException)
                {
                }
            }
            int rep_count = 0;
            foreach (string rep_path in REPLAY_LIST)
            {
                //if (Directory.Exists(REPLAY_PATH))
                if (Directory.Exists(rep_path))
                {
                    string[] replays = Directory.GetFiles(rep_path);
                    foreach (string fileName in replays)
                    {
                        ///string rx_id = @"(Direct Strike.*)\.SC2Replay$|(Desert Strike.*)\.SC2Replay$";
                        string rx_id = @"(Direct Strike.*)\.SC2Replay|(Desert Strike.*)\.SC2Replay";
                        //string rx_id2 = @"(Desert Strike.*)\.SC2Replay";

                        //Match m = Regex.Match(fileName, rx_id, RegexOptions.IgnoreCase);
                        //Match m2 = Regex.Match(fileName, rx_id2, RegexOptions.IgnoreCase);

                        foreach (Match m in Regex.Matches(fileName, rx_id))
                        {

                            i++;
                            string id = m.Groups[1].ToString();
                            if (rep_count > 0) id += "_" + rep_count.ToString();

                            if (!dsreplays.ContainsKey(id))
                            {
                                if (!dsskip.ContainsKey(id))
                                    newrep++;
                            }




                            /**
                            if (m2.Success)
                            {
                                i++;
                                string id = m2.Value;
                                if (rep_count > 0) id += "_" + rep_count.ToString();

                                if (!dsreplays.ContainsKey(id))
                                    if (!dsskip.ContainsKey(id))
                                        newrep++;
                            }
                            **/
                        }
                    }
                }
                rep_count++;
            }
            TOTAL = i;
            NEWREP = newrep;

        }

        public double GetInfo()
        {
            double ds = 0;
            Scan();

            double scalc = 6472659;
            double nsize = NEWREP * scalc;
            double time = NEWREP * 7.2;

            nsize /= 1024;
            nsize /= 1024;
            nsize /= 1024;

            FS /= 1024;
            FS /= 1024;
            FS /= 1024;

            time /= 60;
            time /= 60;

            string st_size = string.Format("{0:0.00}", nsize);
            string st_fs = string.Format("{0:0.00}", FS);
            string st_time = string.Format("{0:0.00}", time);

            ESTTIME = st_time;
            ESTSPACE = st_size;
            FREESPACE = st_fs;

            return ds;
        }


    }
}


