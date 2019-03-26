using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace sc2dsstats_rc1
{
    class dsupload
    {


        public dsupload()
        {

        }

        public static List<string> GenExport(string csv)
        {
            List<string> anonymous = new List<string>();
            var appSettings = ConfigurationManager.AppSettings;
            string player_name = Properties.Settings.Default.PLAYER;
            List<string> player_list = new List<string>();
            if (player_name.Contains(";"))
            {
                player_name = string.Concat(player_name.Where(c => !char.IsWhiteSpace(c)));
                if (player_name.EndsWith(";")) player_name = player_name.Remove(player_name.Length - 1);
                player_list = player_name.Split(';').ToList();
            }
            else
            {
                player_list.Add(player_name);
            }


            if (File.Exists(csv))
            {

                string line;
                string pattern = @"^(\d+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+);$";
                ///string pattern = @"^(\d+);";

                try
                {
                    System.IO.StreamReader file = new System.IO.StreamReader(csv);
                    while ((line = file.ReadLine()) != null)
                    {
                        foreach (Match m in Regex.Matches(line, pattern))
                        {
                            //string value1 = m.Groups[2].ToString() + ".SC2Replay";
                            string player = "player";
                            if (m.Groups[11].ToString() == "1") player = "player1";
                            else if (m.Groups[11].ToString() == "2") player = "player2";
                            else if (m.Groups[11].ToString() == "3") player = "player3";
                            else if (m.Groups[11].ToString() == "4") player = "player4";
                            else if (m.Groups[11].ToString() == "5") player = "player5";
                            else if (m.Groups[11].ToString() == "6") player = "player6";

                            //if (m.Groups[3].ToString() == Properties.Settings.Default.PLAYER) player = "player";
                            if (player_list.Contains(m.Groups[3].ToString())) player = "player";

                            string newline = "";
                            for (int i = 1; i < m.Groups.Count; i++)
                            {
                                if (i == 3) newline += player + "; ";
                                else newline += m.Groups[i].ToString() + "; ";
                            }
                            if (newline.Length > 0) newline = newline.Remove(newline.Length - 1);
                            anonymous.Add(newline);

                        }
                    }

                    file.Close();
                }
                catch (System.IO.IOException)
                {
                }
            }

            return anonymous;
        }
    }

    class dsclient
    {
        private const int port = 7890;

        public static void StartClient(string id, string anonymous)
        {
            byte[] bytes = new byte[1024];

            //string filename = "C:\\temp\\sommer.csv";
            //string zipPath = "C:\\temp\\sommer.gz";

            string filename = anonymous;
            string zipPath = anonymous + ".gz";

            UnicodeEncoding uniEncode = new UnicodeEncoding();

            FileStream fsInFile = null;
            FileStream fsOutFile = null;
            GZipStream Myrar = null;
            byte[] filebuffer;
            int count = 0;

            try
            {
                fsOutFile = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
                Myrar = new GZipStream(fsOutFile, CompressionMode.Compress, true);
                fsInFile = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                filebuffer = new byte[fsInFile.Length];
                count = fsInFile.Read(filebuffer, 0, filebuffer.Length);
                fsInFile.Close();
                fsInFile = null;
                Myrar.Write(filebuffer, 0, filebuffer.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                if (Myrar != null)
                {
                    Myrar.Close();
                    Myrar = null;
                }
                if (fsOutFile != null)
                {
                    fsOutFile.Close();
                    fsOutFile = null;
                }
                if (fsInFile != null)
                {
                    fsInFile.Close();
                    fsInFile = null;
                }
            }



            try
            {
                // Connect to a Remote server  
                // Get Host IP Address that is used to establish a connection  
                // In this case, we get one IP address of localhost that is IP : 127.0.0.1  
                // If a host has multiple addresses, you will get a list of addresses  
                IPHostEntry ipHostInfo = Dns.GetHostEntry("pax77.org");
                IPAddress ipAddress = IPAddress.Parse("144.76.58.9");
                //IPHostEntry ipHostInfo = Dns.GetHostEntry("userver4");
                //IPAddress ipAddress = IPAddress.Parse("192.168.178.28");

                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                // Create a TCP/IP  socket.    
                Socket sender = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.    
                try
                {
                    // Connect to Remote EndPoint  
                    sender.Connect(remoteEP);

                    Console.WriteLine("Socket connected to {0}",
                        sender.RemoteEndPoint.ToString());

                    // Encode the data string into a byte array.    
                    byte[] msg = Encoding.ASCII.GetBytes("This is a test" + "\r\n");

                    // Send the data through the socket.    
                    //int bytesSent = sender.Send(msg);

                    // Create the preBuffer data.
                    string string1 = "Hello from [" + id + "]" + "\r\n";
                    byte[] preBuf = Encoding.ASCII.GetBytes(string1);

                    // Create the postBuffer data.
                    string string2 = "Have fun." + "\r\n";
                    byte[] postBuf = Encoding.ASCII.GetBytes(string2);

                    //Send file fileName with buffers and default flags to the remote device.
                    Console.WriteLine("Sending {0} with buffers to the host.{1}", filename, Environment.NewLine);
                    sender.SendFile(zipPath, preBuf, postBuf, TransmitFileOptions.UseDefaultWorkerThread);

                    int bytesSent = sender.Send(msg);

                    // Receive the response from the remote device.    
                    int bytesRec = sender.Receive(bytes);
                    Console.WriteLine("Echoed test = {0}",
                        Encoding.ASCII.GetString(bytes, 0, bytesRec));



                    // Release the socket.    
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();

                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
