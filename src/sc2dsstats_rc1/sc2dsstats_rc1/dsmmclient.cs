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
using System.Threading;

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
                    string string1 = "Hello from [" + player + "]: Letmeplay: " + mode + ";" + num + ";" + skill + ";" + server + ";" + "\r\n";
                    byte[] preBuf = Encoding.UTF8.GetBytes(string1);

                    int bytesSent = sender.Send(preBuf);

                    // Receive the response from the remote device.    
                    int bytesRec = sender.Receive(bytes);
                    Console.WriteLine("Echoed test = {0}",
                        Encoding.UTF8.GetString(bytes, 0, bytesRec));

                    if (sender != null) PingPong(Encoding.UTF8.GetString(bytes, 0, bytesRec), player, sender, "0");


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

        public string PingPong(string pong, string player, Socket client, string mmid)
        {
            string response = "";
            string answer = "";
            //string mmid = "0";

            string pattern = @"^sc2dsmm: (.*)";
            foreach (Match m in Regex.Matches(pong, pattern))
            {
                answer = m.Groups[1].ToString();
            }
            if (CheckPong(answer))
            {
                string pt_letmeplay = @"^Letmeplay: (.*)";
                string pt_accept = @"^Accept: (.*)";
                string pt_decline = @"^Decline: (.*)";
                string pt_findgame = @"^Findgame: (.*)";
                string pt_ready = @"^Ready: (.*)";


                // Matchup ready to go
                foreach (Match m_ready in Regex.Matches(answer, pt_ready))
                {
                    string ent = m_ready.Groups[1].ToString();
                    if (ent == "0")
                    {
                        // Someone declined / timed out :(
                        MW.Dispatcher.Invoke(() =>
                        {
                            MW.gr_accept.Visibility = Visibility.Hidden;
                            MW.tb_gamefound.Visibility = Visibility.Visible;
                            MW.tb_gamefound.Text = "Someone declined / timed out :( - Searching again ...";
                        });

                        // Waiting again
                        Random rngesus = new Random();
                        int rng = rngesus.Next(0, 3000);
                        int mysleep = 5000 + rng;
                        Thread.Sleep(mysleep);
                        response = "Findgame: 1";

                    }
                    else
                    {
                        // all accepted :)
                        SetupPos(ent, player);
                        response = "fin";
                    }
                    goto Response;
                }

                // We are ready!
                foreach (Match m_accept in Regex.Matches(answer, pt_accept))

                {
                    mmid = m_accept.Groups[1].ToString();
                    response = "Ready: " + mmid;
                    goto Response;
                }
                // Searching ...
                foreach (Match m_findgame in Regex.Matches(answer, pt_findgame))
                {
                    mmid = m_findgame.Groups[1].ToString();
                    if (mmid == "0")
                    {
                        // Waiting
                        Random rngesus = new Random();
                        int rng = rngesus.Next(0, 3000);
                        int mysleep = 5000 + rng;
                        Thread.Sleep(mysleep);

                        response = "Findgame: 0";

                    }
                    else if (mmid.StartsWith("Games"))
                    {
                        MW.Dispatcher.Invoke(() =>
                        {
                            MW.tb_info.Text = mmid;
                        });
                        // Waiting
                        Random rngesus = new Random();
                        int rng = rngesus.Next(0, 3000);
                        int mysleep = 5000 + rng;
                        Thread.Sleep(mysleep);
                        response = "Findgame: 0";
                    } else  
                    {
                        // Matchup ready - waiting for Accept
                        MW.Dispatcher.Invoke(() =>
                        {
                            MW.GameFound(mmid);
                        });
                        response = "wait";
                    }
                    goto Response;
                }

                // we declined :(
                foreach (Match m_decline in Regex.Matches(answer, pt_decline))
                {
                    response = "fin";
                    MW.Dispatcher.Invoke(() =>
                    {
                        MW.bt_show.IsEnabled = true;
                        MW.tb_gamefound.Visibility = Visibility.Visible;
                        MW.tb_gamefound.Text = "Ready process declined :(";
                        try
                        {
                            MW._timer.Stop();
                        }
                        catch { }
                    });
                    goto Response;
                }
                // Find game
                foreach (Match m_letmeplay in Regex.Matches(answer, pt_letmeplay))
                {
                    response = "Findgame: 0";
                    MW.Dispatcher.Invoke(() =>
                    {
                        MW.tb_gamefound.Text = "Searching ...";
                        MW.mmcb_ample.IsChecked = true;
                        MW.mmcb_ample.Content = "Online";
                    });
                    goto Response;
                }
            }

            Console.WriteLine("ERR: " + answer);

                        

            Response:        

            Console.WriteLine(player + ": " + pong + " => " + response);

            if (client != null)
            {

                if (response == "fin")
                {
                    try
                    {
                        StopClient(client);
                        client.Dispose();
                        client = null;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Client closed: {0}", e.ToString());
                    }
                    MW.Dispatcher.Invoke(() =>
                    {
                        MW.mmcb_ample.IsChecked = false;
                        MW.mmcb_ample.Content = "Offline";
                    });
                }
                else
                {
                    if (response != "wait")
                    {
                        response = "Hello from [" + player + "]: " + response + "\r\n";
                        byte[] msgBuf = Encoding.UTF8.GetBytes(response);
                        try
                        {
                            client.Send(msgBuf);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Client closed: {0}", e.ToString());
                        }
                    }


                    byte[] bytes = new byte[1024];
                    string fin = "";
                    int bytesRec = 0;
                    try
                    {
                        bytesRec = client.Receive(bytes);
                        fin = Encoding.UTF8.GetString(bytes, 0, bytesRec);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Client closed: {0}", e.ToString());
                    }

                    if (bytesRec > 0) PingPong(Encoding.UTF8.GetString(bytes, 0, bytesRec), player, client, mmid);

                }

            } else
            {
                MW.Dispatcher.Invoke(() =>
                {
                    MW.mmcb_ample.IsChecked = false;
                    MW.mmcb_ample.Content = "Offline";
                });
            }

            return response;
        }

        public void SetupPos (string msg, string player)
        {
            int mypos = 0;
            string mmid = "0";
            string creator = "1";
            string server = "NA";

            MW.Dispatcher.Invoke(() =>
            {
                MW.doit = false;
                MW.GAME_READY = true;
                MW.gr_accept.Visibility = Visibility.Hidden;
            });

                foreach (string p in msg.Split(';'))
            {

                string pt_pos = @"^pos(\d): (.*)";
                foreach (Match m2 in Regex.Matches(p, pt_pos))
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
                    else if (pos == 7)
                    {
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
                        MW.GameReady(mypos, creator, mmid);
                        MW.tb_gamefound.Visibility = Visibility.Visible;
                    });
                }
            }
        }

    

        public bool CheckPong (string msg)
        {
            bool check = false;
            if (msg.Length < 400)
            {
                check = true;
            } else
            {
                Console.WriteLine("Response too long :(");
            }
            return check;
        }

    }
}

