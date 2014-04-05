using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//lxw0109 Need the following.
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;


namespace lxw_Udp_Chat
{
    class Communicate
    {
        //Use the UdpClient socket.
        public UdpClient recvUdpClient;
        //Online Users List.
        public List<string> olUser = new List<string>();
        public Array olUserArr;
        //FILE TRANSFER. UDP
        public UdpClient fileUdpClient;
        //FILE TRANSFER. TCP
        public TcpListener tcpListen;

        public Communicate()
        {
            //Bind the UdpClient to the specified port.
            this.recvUdpClient = new UdpClient(10086);
            this.fileUdpClient = new UdpClient(10087);
            //this.tcpListen = new TcpListener(IPAddress.Parse("127.0.0.1"), 10010);
        }

        //send/receive Broadcast.
        public void receiveBroadcast()
        {
            //Server
            // The master thread is to receive as a server. And the child thread is to send as a client.
            try
            {
                //Multiple Threads.
                ThreadStart ts = new ThreadStart(sendBroadcast);
                Thread thread = new Thread(ts);
                thread.Start();

                //NOTE: Is 'while(true)' needed here? I think so.

                //IPAddress.Any: Any IP address that is available in local.
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                //IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0);  // OK                
                //The peer which receive at first is Server.
                this.recvUdpClient.Receive(ref RemoteIpEndPoint);
                //Console.WriteLine("RemoteIpEndPoint：{0}", RemoteIpEndPoint.ToString());
                //Console.WriteLine("RemoteIpEndPoint.Address is {0}, RemoteIpEndPoint.Port is {1}", RemoteIpEndPoint.Address.ToString(), RemoteIpEndPoint.Port.ToString());
                this.olUser.Add("Addr:" + RemoteIpEndPoint.Address.ToString());

                #region code_not_use
                /*
                byte[] sdata = Encoding.ASCII.GetBytes("Server:\tHello!");
                IPAddress ipa = IPAddress.Parse("127.0.0.1");
                while (true)
                {
                    //lxw0109
                    //recvUdpClient.Send(sdata, sdata.Length, new IPEndPoint(RemoteIpEndPoint.Address, RemoteIpEndPoint.Port));
                    //Send 方法将数据报发送到指定的终结点，并返回成功发送的字节数。
                    //在调用此重载之前，首先必须使用要将数据报发送到的远程主机的 IP 地址和端口号来创建一个 IPEndPoint。
                    //通过将 IPEndPoint 的 Address 属性指定为 SocketOptionName.Broadcast，可将数据报发送到默认广播地址：255.255.255.255。
                    recvUdpClient.Send(sdata, sdata.Length, RemoteIpEndPoint);
                    //System.Threading.Thread.Sleep(3000);
                    //Console.Write('.');
                    //Get the input of the user.
                    sdata = Encoding.ASCII.GetBytes(Console.ReadLine());
                }*/
                #endregion
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        //Client. Child Thread.
        public void sendBroadcast()
        {
            //Client
            try
            {
                // Not send these strings, but send the broadcast packet.
                IPEndPoint broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, 10086);
                this.recvUdpClient.Send(new byte[] { 0x31 }, 1, broadcastEndPoint);

                #region code_not_use
                /*
                byte[] data = new byte[100];
                //将数据报发送到在 Connect 方法中建立的远程主机，并返回发送的字节数。
                //如果在调用此重载之前未调用 Connect，则 Send 方法将引发 SocketException。
                //udpClient.Send(new byte[] { 0x31, 0x32 }, 2);   // 1 2 

                // Not send these strings, but send the broadcast packet.
                udpClient.Send(new byte[] { 0x31 }, 1);   // 发什么无所谓

                
                while (true)
                {
                    //返回由远程主机发送的 UDP 数据报的内容
                    data = udpClient.Receive(ref ep);
                    string dataByte = Encoding.ASCII.GetString(data);
                    Console.WriteLine(dataByte);
                }*/
                #endregion
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        //send/receive Content.
        public void sendContent(string chatContent, string chatPerson)
        {
            try
            {
                //byte[] chatPersonBytes = { };
                IPAddress chatIP = IPAddress.Parse(chatPerson);// (Encoding.Default.GetBytes(chatPerson));   //not OK in 'byte[]' way.
                IPEndPoint chatIPEndPoint = new IPEndPoint(chatIP, 10086);
                byte[] sendByte = Encoding.Default.GetBytes(chatContent);
                this.recvUdpClient.Send(sendByte, sendByte.Length, chatIPEndPoint);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }        
    }
}
