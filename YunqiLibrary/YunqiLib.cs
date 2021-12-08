using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace YunqiLibrary
{
    public class TCP_Client {

        private Thread t;

        public delegate void CallBack<T>(T arg);

        public event CallBack<string> TCPCallBackevent;

        public TCP_Client(CallBack<string> TcpclientListeningAction)
        {
            TCPCallBackevent += TcpclientListeningAction;
        }
        struct LinkData
        {
            public string host;
            public int port;
            public string data;
        };


        public void TCPSenHex(string _host, int _port, string _data) {
            LinkData linkData = new LinkData { host = _host, port = _port, data = _data };

            t = new Thread(new ParameterizedThreadStart(sendHEX));

            t.Start(linkData);
        }

        public void TCPSend(string _host, int _port, string _data)
        {
            LinkData linkData = new LinkData { host = _host, port = _port, data = _data };

            t = new Thread(new ParameterizedThreadStart(send));

            t.Start(linkData);
        }


        private void sendHEX(object _linkData)
        {
            LinkData linkdata = (LinkData)_linkData;

            if (!Utility.checkIp(linkdata.host))
            {
                TCPCallBackevent.Invoke("ip地址不正确");

                t.Abort();
            }
            string result = string.Empty;

            IPAddress ipAddress = IPAddress.Parse(linkdata.host);

            IPEndPoint ipep = new IPEndPoint(ipAddress, linkdata.port);//IP和端口

            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            ConnectSocketDelegate connect = ConnectSocket;

            IAsyncResult asyncResult = connect.BeginInvoke(ipep, clientSocket, null, null);

            bool connectSuccess = asyncResult.AsyncWaitHandle.WaitOne(1000, false);

            if (!connectSuccess)
            {
                TCPCallBackevent.Invoke("链接失败请检查网络链接");

                t.Abort();
            }

            string exmessage = connect.EndInvoke(asyncResult);

            if (!string.IsNullOrEmpty(exmessage))
            {
                t.Abort();
            }


            byte[] b = Utility.strToToHexByte(linkdata.data);

            clientSocket.Send(b);

            Thread.Sleep(500);

            result = ReceiveLEDHex(clientSocket, 2000); //5*2 seconds timeout.
                                                        // Debug.Log("Receive：" + result)             
            Thread.Sleep(500);
            DestroySocket(clientSocket);

            if (result.Length > 0)
            {
                TCPCallBackevent.Invoke(result);
            }

            DestroySocket(clientSocket);

            t.Abort();
        }

        private void send(object _linkData)
        {
            LinkData linkdata = (LinkData)_linkData;

            if (!Utility.checkIp(linkdata.host))
            {
                TCPCallBackevent.Invoke("ip地址不正确");

                t.Abort();
            }
            string result = string.Empty;

            IPAddress ipAddress = IPAddress.Parse(linkdata.host);

            IPEndPoint ipep = new IPEndPoint(ipAddress, linkdata.port);//IP和端口

            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            ConnectSocketDelegate connect = ConnectSocket;

            IAsyncResult asyncResult = connect.BeginInvoke(ipep, clientSocket, null, null);

            bool connectSuccess = asyncResult.AsyncWaitHandle.WaitOne(1000, false);

            if (!connectSuccess)
            {
                TCPCallBackevent.Invoke("链接失败请检查网络链接");

                t.Abort();
            }

            string exmessage = connect.EndInvoke(asyncResult);

            if (!string.IsNullOrEmpty(exmessage))
            {
                t.Abort();
            }

            //if (linkdata.isHEX)
            //{
            //    byte[] b = Utility.strToToHexByte(linkdata.data);

            //    clientSocket.Send(b);


            //    Thread.Sleep(500);

            //    result = ReceiveLEDHex(clientSocket, 2000); //5*2 seconds timeout.
            //                                                                         // Debug.Log("Receive：" + result)             
            //}
            //else
            //{
            clientSocket.Send(Encoding.Default.GetBytes(linkdata.data));

            Thread.Sleep(500);

            result = Receive(clientSocket, 2000);

            Thread.Sleep(500);
            DestroySocket(clientSocket);

            if (result.Length > 0)
            {
                TCPCallBackevent.Invoke(result);
            }

            DestroySocket(clientSocket);

            t.Abort();
        }

        private static void DestroySocket(Socket socket)
        {
            if (socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            socket.Close();
        }

        private string Receive(Socket socket, int timeout)
        {
            string result = string.Empty;
            socket.ReceiveTimeout = timeout;
            List<byte> data = new List<byte>();
            byte[] buffer = new byte[1024];
            int length = 0;
            try
            {
                while ((length = socket.Receive(buffer)) > 0)
                {
                    for (int j = 0; j < length; j++)
                    {
                        data.Add(buffer[j]);
                    }
                    if (length < buffer.Length)
                    {
                        break;
                    }
                }
            }
            catch { }
            if (data.Count > 0)
            {
                result = Encoding.Default.GetString(data.ToArray(), 0, data.Count);
            }

            return result;
        }

        private string ReceiveLEDHex(Socket socket, int timeout)
        {
            string result = string.Empty;
            socket.ReceiveTimeout = timeout;
            List<byte> data = new List<byte>();
            byte[] buffer = new byte[1024];
            int length = 0;
            try
            {
                while ((length = socket.Receive(buffer)) > 0)
                {
                    for (int j = 0; j < length; j++)
                    {
                        data.Add(buffer[j]);
                    }
                    if (length < buffer.Length)
                    {
                        break;
                    }
                }
            }
            catch { }
            if (data.Count > 0)
            {            
                byte[] bytes = data.ToArray();


                if (bytes != null)
                {
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        result += bytes[i].ToString("X2");
                    }
                }
            }

            return result;
        }
        private delegate string ConnectSocketDelegate(IPEndPoint ipep, Socket sock);
        private string ConnectSocket(IPEndPoint ipep, Socket sock)
        {
            string exmessage = "";
            try
            {
                sock.Connect(ipep);
            }
            catch (System.Exception ex)
            {
                exmessage = ex.Message;
            }
            finally
            {
            }

            return exmessage;
        }
    }

    public class UDP
    {

        public IPEndPoint m_ip;//定义一个IP地址和端口号

        Socket m_newsock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);//实例化socket对象设置寻址方案为internetwork（IP版本的4存放）,设置Soket的类型，为Dgram（支持数据报形式的数据），设置协议的类型，为UDP
                                                                                                      //绑定网络地址
        Thread test;

        public int m_ReceivePort;

        private int m_recv;//定义一个接受值的变量

        private byte[] m_data = new byte[1024];//定义一个二进制的数组用来获取客户端发过来的数据包

        private string m_mydata;

        public delegate void CallBack<T>(T arg);

        public event CallBack<string> CallBackevent;

        public UDP(int _m_ReceivePort,CallBack<string> udpListeningAction)
        {
            m_ReceivePort= _m_ReceivePort;

            CallBackevent += udpListeningAction;

            InitializationUdp();

        }




        public void InitializationUdp()
        {
            //得到本机IP，设置TCP端口号        
            m_ip = new IPEndPoint(IPAddress.Any, m_ReceivePort);//设置自身的IP和端口号，在这里IPAddress.Any是自动获取本机IP
      
            m_newsock.Bind(m_ip);//绑定IP

            test = new Thread(BeginListening);//定义一个子线程

            test.Start();//子线程开始
        }

        /// <summary>
        /// 线程接受
        /// </summary>
        void BeginListening()
        {

            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);//实例化一个网络端点，设置为IPAddress.Any为自动获取跟我通讯的IP，0代表所有的地址都可以
            EndPoint Remote = (EndPoint)(sender);//实例化一个地址结束点来标识网络路径
                                                 //  Debug.Log(Encoding.ASCII.GetString(data, 0, recv));//输出二进制转换为string类型用来测试
            while (true)
            {
                m_data = new byte[1024];//实例化data
                m_recv = m_newsock.ReceiveFrom(m_data, ref Remote);//将数据包接收到的数据放入缓存点，并存储终节点
                m_mydata = Encoding.UTF8.GetString(m_data, 0, m_recv);

                if (m_mydata.Length > 0)
                {
                    CallBackevent.Invoke(m_mydata);
                }
            }
        }

        #region SENDUDP
        Socket udpserver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        public bool udp_Send(string da, string ip, int port)
        {
            try
            {
                //设置服务IP，设置端口号
                IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(ip), port);
                //发送数据
                byte[] data = new byte[1024];

                data = Encoding.UTF8.GetBytes(da);


                udpserver.SendTo(data, data.Length, SocketFlags.None, ipep);
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }

    public static class Utility
    {
        public static bool checkIp(string ipStr)
        {
            IPAddress ip;
            if (IPAddress.TryParse(ipStr, out ip))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //十六进制字符串转byte数组
        public static byte[] strToToHexByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }
    }
}
