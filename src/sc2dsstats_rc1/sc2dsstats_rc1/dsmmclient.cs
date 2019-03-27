using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace sc2dsstats_rc1
{
    class dsmmclient
    {
        private const int port = 7891;
        Win_mm MW { get; set; }
        List<KeyValuePair<int, string>> PLPOS { get; set; }
        public string INFO { get; set; }

        public dsmmclient()
        {
            PLPOS = new List<KeyValuePair<int, string>>();
        }

        public dsmmclient(Win_mm mw) : this()
        {
            MW = mw;
        }


        public void StopClient(Socket sender)
        {
            try
            {
                sender.Shutdown(SocketShutdown.Both);
                sender.Close();
            }
            catch { }
        }

        public Socket StartClient(string player, string result)
        {
            Socket sender = null;
            byte[] bytes = new byte[1024];

            try
            {
                // Connect to a Remote server  
                // Get Host IP Address that is used to establish a connection  
                // In this case, we get one IP address of localhost that is IP : 127.0.0.1  
                // If a host has multiple addresses, you will get a list of addresses  

                //IPHostEntry ipHostInfo = Dns.GetHostEntry("pax77.org");
                //IPAddress ipAddress = IPAddress.Parse("144.76.58.9");

                IPHostEntry ipHostInfo = Dns.GetHostEntry("pax77.org");
                IPAddress ipAddress = IPAddress.Parse("144.76.58.9");
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                // Create a TCP/IP  socket.    
                sender = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.    
                try
                {
                    // Connect to Remote EndPoint  
                    sender.Connect(remoteEP);

                    Console.WriteLine("Socket connected to {0}",
                        sender.RemoteEndPoint.ToString());

                    // Encode the data string into a byte array.    
                    byte[] msg = Encoding.UTF8.GetBytes("This is a test" + "\r\n");

                    // Send the data through the socket.    
                    //int bytesSent = sender.Send(msg);

                    // Create the preBuffer data.
                    string string1 = "Hello from [" + player + "]: " + result + ";" + "\r\n";
                    byte[] preBuf = Encoding.UTF8.GetBytes(string1);

                    // Create the postBuffer data.
                    string string2 = "Have fun." + "\r\n";
                    byte[] postBuf = Encoding.UTF8.GetBytes(string2);

                    int bytesSent = sender.Send(preBuf);

                    // Receive the response from the remote device.    
                    int bytesRec = sender.Receive(bytes);
                    Console.WriteLine("Echoed test = {0}",
                        Encoding.UTF8.GetString(bytes, 0, bytesRec));

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
            return sender;
        }

        public Socket StartClient(Win_mm mw, string player, string mode, string num, string skill, string server)
        {
            Socket sender = null;
            byte[] bytes = new byte[1024];

            try
            {
                // Connect to a Remote server  
                // Get Host IP Address that is used to establish a connection  
                // In this case, we get one IP address of localhost that is IP : 127.0.0.1  
                // If a host has multiple addresses, you will get a list of addresses  

                //IPHostEntry ipHostInfo = Dns.GetHostEntry("pax77.org");
                //IPAddress ipAddress = IPAddress.Parse("144.76.58.9");

                IPHostEntry ipHostInfo = Dns.GetHostEntry("pax77.org");
                IPAddress ipAddress = IPAddress.Parse("144.76.58.9");
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                // Create a TCP/IP  socket.    
                sender = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                mw.Dispatcher.Invoke(() =>
                {
                    mw.CLIENT = sender;
                });

                // Connect the socket to the remote endpoint. Catch any errors.    
                try
                {
                    // Connect to Remote EndPoint  
                    sender.Connect(remoteEP);

                    Console.WriteLine("Socket connected to {0}",
                        sender.RemoteEndPoint.ToString());

                    // Encode the data string into a byte array.    
                    byte[] msg = Encoding.UTF8.GetBytes("This is a test" + "\r\n");

                    // Send the data through the socket.    
                    //int bytesSent = sender.Send(msg);

                    // Create the preBuffer data.
                    string string1 = "Hello from [" + player + "]: " + mode + ";" + num + ";" + skill + ";" + server + ";" + "\r\n";
                    byte[] preBuf = Encoding.UTF8.GetBytes(string1);

                    // Create the postBuffer data.
                    string string2 = "Have fun." + "\r\n";
                    
                    byte[] postBuf = Encoding.UTF8.GetBytes(string2);

                    int bytesSent = sender.Send(preBuf);

                    // Receive the response from the remote device.    
                    int bytesRec = sender.Receive(bytes);
                    Console.WriteLine("Echoed test = {0}",
                        Encoding.UTF8.GetString(bytes, 0, bytesRec));

                    if (sender != null) HandleResponse(Encoding.UTF8.GetString(bytes, 0, bytesRec), player, sender);


                    // Release the socket.    
                    //sender.Shutdown(SocketShutdown.Both);
                    //sender.Close();



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
            return sender;
        }

        private static bool RemoveEmpty(String s)
        {
            return s == "";
        }

        public void HandleResponse(string resp, string player, Socket client)
        {
            string msg = player + ": Thank you." + "\r\n";
            string mmid = "0";
            string creator = "0";
            int mypos = 0;
            string server = "0";

            MW.Dispatcher.Invoke(() =>
            {
                MW.tb_info.Text = resp;
            });

            string pattern = @"^sc2dsmm: (.*)";
            string pattern1 = @"^connected: (.*)";
            string pattern2 = @"^pos(\d): (.*)";
            string pattern4 = @"^sum: (.*)";

            if (resp.EndsWith(";")) resp = resp.Remove(resp.Length - 1);
            List<string> lresp = resp.Split(';').ToList();
            lresp.RemoveAll(RemoveEmpty);

            MW.Dispatcher.Invoke(() =>
            {
                //MW.tb_info.Text = "";
            });

            foreach (string myresp in lresp)
            {
                foreach (Match m in Regex.Matches(myresp, pattern))
                {
                    string ent = m.Groups[1].ToString();
                    foreach (Match m1 in Regex.Matches(ent, pattern1))
                    {
                        string teammate = m1.Groups[1].ToString();
                        MW.Dispatcher.Invoke(() =>
                        {
                            //MW.tb_connected.Text += teammate + " connected." + Environment.NewLine;
                        });
                    }

                    foreach (Match m3 in Regex.Matches(ent, pattern4))
                    {
                        string sum = m3.Groups[1].ToString();
                        MW.Dispatcher.Invoke(() =>
                        {
                            //MW.tb_info.Text += sum;
                        });
                    }
                    
                    foreach (Match m2 in Regex.Matches(ent, pattern2))
                    {
                        int pos = Int32.Parse(m2.Groups[1].ToString());
                        string teammate = m2.Groups[2].ToString();
                        
                        if (teammate == player)
                        {
                            mypos = pos;
                        }

                        Label lb = null;
                        if (pos == 0)
                        {
                            mmid = m2.Groups[2].ToString();
                        }
                        else if (pos == 1)
                        {
                            lb = MW.lb_pl1;
                        }
                        else if (pos == 2)
                        {
                            lb = MW.lb_pl2;
                        }
                        else if (pos == 3)
                        {
                            lb = MW.lb_pl3;
                        }
                        else if (pos == 4)
                        {
                            lb = MW.lb_pl4;
                        }
                        else if (pos == 5)
                        {
                            lb = MW.lb_pl5;
                        }
                        else if (pos == 6)
                        {
                            lb = MW.lb_pl6;
                        }
                        else if (pos == 7) {
                            creator = teammate;
                        }
                        else if (pos == 8)
                        {
                            server = teammate;
                        }
                        else
                        {
                            lb = MW.lb_info;
                        }
                        MW.Dispatcher.Invoke(() =>
                        {
                            if (mmid != "0") MW.tb_mmid.Text = mmid;
                            if (lb != null) lb.Content = teammate;
                            if (server != "0") MW.tb_server.Text = server;
                        });
                    }
                }



                if (myresp == "")
                {

                }
            }

            if (mmid != "0")
            {
                MW.Dispatcher.Invoke(() =>
                {
                    MW.GameFound(mypos, creator, mmid);
                });
                StopClient(client);
            }
            else if (client != null)
            {

                //Console.WriteLine("Sending stuff ..");
                byte[] msgBuf = Encoding.UTF8.GetBytes(msg);
                client.Send(msgBuf);

                byte[] bytes = new byte[1024];
                int bytesRec = client.Receive(bytes);
                string fin = Encoding.UTF8.GetString(bytes, 0, bytesRec);
                if (fin == "sc2dsmm: finished.")
                {
                    StopClient(client);
                }
                else
                {
                    //Console.WriteLine("Listening again ..");
                    HandleResponse(Encoding.UTF8.GetString(bytes, 0, bytesRec), player, client);
                }
            }

            string pattern3 = @"^Xpax\d";
            foreach (Match m in Regex.Matches(player, pattern3))
            {
                StopClient(client);
            }
        }
    }
}

