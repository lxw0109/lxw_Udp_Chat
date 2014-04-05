using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

//lxw0109 Need the following.
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace lxw_Udp_Chat
{
    public partial class ChatWindow : Form
    {
        private Communicate com = new Communicate();
        //Mutex.
        private Mutex mut = new Mutex();
        //The current person(IP Adress in string) who you are chatting with.
        private string chatPerson = "";
        //FILE TRANSFER.
        //Socket socketSent = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //private UdpClient fileSent = new UdpClient(10087);

        //Whether receive Window.
        private IfRecv ifWindow;
        

        public ChatWindow()
        {
            //ForIllegalCrossThreadCalls
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
        }

        private void ChatWindow_Load(object sender, EventArgs e)
        {
            //Once the window is loaded, broadcast.
            this.com.receiveBroadcast();
            //NO Online User.
            if (com.olUser.Count == 0)
            {
                MessageBox.Show("Sorry, No online user now");
            }
            else
            {
                com.olUserArr = com.olUser.ToArray();
                this.comboBox1.Items.AddRange((object[])com.olUserArr);
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.chatPerson = com.olUserArr.GetValue(this.comboBox1.SelectedIndex).ToString().Replace("Addr:", "");
            this.label1.Text = "Chatting with " + this.chatPerson;

            //Create a new thread to listen the user's input.
            ThreadStart ts1 = new ThreadStart(listenRemote_10086);
            Thread thread1 = new Thread(ts1);
            thread1.Start();
            
            /*
            //UDP
            
            //IPEndPoint ipSent = new IPEndPoint(IPAddress.Parse(this.chatPerson), 10087);
            //socketSent.Connect(ipSent);
            //Create a new thread to listen the user's FILE TRANSFER INFOR.
            ThreadStart ts2 = new ThreadStart(listenRemote_10087);
            Thread thread2 = new Thread(ts2);
            //NOTE: Be of vital importance.
            thread2.SetApartmentState(ApartmentState.STA);
            thread2.Start();
            */

            //TCP
            this.com.tcpListen = new TcpListener(IPAddress.Parse(this.chatPerson), 10010);
            ThreadStart ts5 = new ThreadStart(listenRemote_10010);
            Thread thread5 = new Thread(ts5);
            //NOTE: Be of vital importance.
            thread5.SetApartmentState(ApartmentState.STA);
            thread5.Start();
        }

        //send Button.
        private void button1_Click(object sender, EventArgs e)
        {
            if (this.chatPerson.Equals(""))
            {
                MessageBox.Show("Choose one guy you want to chat, Please!");
                return;
            }
            if (this.richTextBox2.Text != "") //Without strip() is better.
            {
                string tempStr = this.richTextBox2.Text;
                //MUTEX
                this.mut.WaitOne();

                this.richTextBox1.Text += "Me: " + tempStr + "\r\n";
                this.richTextBox2.Text = "";

                this.mut.ReleaseMutex();

                this.com.sendContent(tempStr, this.chatPerson);                
            }
            else 
            {
                MessageBox.Show("Oh man, don't send empty content!");
            }
        }

        //When receive packets from the remote host, update the RichTextBox1.
        /*public void updateRichTextBox1(string recvContent)
        {
            this.richTextBox1.Text += recvContent + "\r\n";
        }*/

        //Listen to the user's input(Port 10086). NEVER STOP.
        //public void receiveContent()
        //Receive from the 10086 port. Parent: Master(comboBox1_SelectedIndexChanged).
        public void listenRemote_10086()
        {
            try
            {
                IPEndPoint chatPersonIP = new IPEndPoint(IPAddress.Parse(this.chatPerson), 10086);
                while (true)
                {
                    // This expression can never be put into the MUTEX REGION.
                    Byte[] receiveBytes = this.com.recvUdpClient.Receive(ref chatPersonIP);
                    string returnData = Encoding.ASCII.GetString(receiveBytes);
                    //MUTEX
                    this.mut.WaitOne();
                    
                    this.richTextBox1.Text += this.chatPerson + ": " + returnData + "\r\n";

                    this.mut.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        //UDP: Listen to FILE TRANSFER(Port 10087). After creation, NEVER STOP.
        //Receive from the 10087 port. Parent: master thread(comboBox1_SelectedIndexChanged).
        public void listenRemote_10087()
        {
            try
            {
                IPEndPoint fileIP = new IPEndPoint(IPAddress.Parse(this.chatPerson), 10087);
                while (true)
                {
                    Byte[] receiveBytes = this.com.fileUdpClient.Receive(ref fileIP);
                    string returnData = Encoding.ASCII.GetString(receiveBytes).Trim();
                    //NOTE: lxw MUTEX NOTE HERE.
                    if (returnData.StartsWith("FILE:"))
                    {
                        string fileName = returnData.Substring(5);
                        this.ifWindow = new IfRecv();

                        ParameterizedThreadStart pts4 = new ParameterizedThreadStart(ifRecvFun);
                        Thread thread4 = new Thread(pts4);                       
                        thread4.Start(fileName);

                        while (!this.ifWindow.ifClick)
                        {
                            ;   //Waiting for the click. 
                        }
                        
                        thread4.Abort();
                        if (ifWindow.ifRecv)//Receive
                        {
                            #region SAVEFILEDIALOG_NOTE
                            //CANNOT BE PLACED HERE.
                            //SaveFileDialog sfd = new SaveFileDialog();
                            //sfd.ShowDialog();/////?????????????????????????????????
                            #endregion

                            /*ThreadStart ts5 = new ThreadStart(getSavePath);
                            Thread thread5 = new Thread(ts5);
                            thread5.SetApartmentState(ApartmentState.STA);
                            thread5.Start();*/
                            SaveFileDialog sfd = new SaveFileDialog();
                            sfd.ShowDialog();
                            //fileName = sfd.FileName;  //Absolute Path. E.G. "F:\\lxw.txt"

                            byte[] sendByte = Encoding.Default.GetBytes("Yes");
                            this.com.fileUdpClient.Send(sendByte, sendByte.Length, fileIP);

                            FileStream write = new FileStream(sfd.FileName, FileMode.OpenOrCreate, FileAccess.Write);
                            //Receive the content of the file.
                            while (true)
                            {
                                try
                                {
                                    receiveBytes = this.com.fileUdpClient.Receive(ref fileIP);
                                    //whithou ".Trim()" is better.
                                    returnData = Encoding.ASCII.GetString(receiveBytes);
                                    if (returnData == "FILEEND")
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        write.Write(receiveBytes, 0, receiveBytes.Length);
                                    }
                                }
                                catch (Exception)
                                {
                                    break;
                                }
                            }
                            write.Close();
                            MessageBox.Show("File Transfer Finished!");
                        }
                        else    //Not Receive
                        {
                            //this.com.fileUdpClient.Send();
                        }
                    }
                    else 
                    {
                        MessageBox.Show("FILE TRANSFER WRONG in listenRemote_10087()");
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        //ifRecv window processing.
        public void ifRecvFun(Object arg)
        {
            string fileName = (string)arg;
            this.ifWindow.setLabel(fileName);
            this.ifWindow.ShowDialog();
        }

        /*//SaveFileDialog.ShowDialog();
        public void getSavePath()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.ShowDialog();
        }*/

        //TCP: Listen to FILE TRANSFER(Port 10010). After creation, NEVER STOP.
        //Receive from the 10010 port. Parent: master thread(comboBox1_SelectedIndexChanged).
        public void listenRemote_10010()
        {
            try
            {
                // Start listening for client requests.
                this.com.tcpListen.Start();
                Byte[] bytes = new Byte[1024];
                string returnData = "";

                //listening loop.
                while (true)
                {
                    // Perform a blocking call to accept requests.
                    TcpClient client = this.com.tcpListen.AcceptTcpClient();
                    returnData = "";
                    // A stream for reading and writing.
                    NetworkStream stream = client.GetStream();
                    int i = stream.Read(bytes, 0, bytes.Length);
                    returnData = System.Text.Encoding.ASCII.GetString(bytes, 0, i);

                    if (returnData.StartsWith("FILE:"))
                    {
                        string fileName = returnData.Substring(5);
                        this.ifWindow = new IfRecv();

                        ParameterizedThreadStart pts4 = new ParameterizedThreadStart(ifRecvFun);
                        Thread thread4 = new Thread(pts4);
                        thread4.Start(fileName);

                        while (!this.ifWindow.ifClick)
                        {
                            ;   //Waiting for the click. 
                        }
                        thread4.Abort();

                        if (ifWindow.ifRecv)//Receive
                        {
                            SaveFileDialog sfd = new SaveFileDialog();
                            sfd.ShowDialog();
                            //fileName = sfd.FileName;  //Absolute Path. E.G. "F:\\lxw.txt"

                            byte[] sendByte = Encoding.Default.GetBytes("Yes");
                            stream.Write(sendByte, 0, sendByte.Length);

                            FileStream write = new FileStream(sfd.FileName, FileMode.OpenOrCreate, FileAccess.Write);
                            
                            //int i = stream.Read(bytes, 0, bytes.Length);
                            //returnData = System.Text.Encoding.ASCII.GetString(bytes, 0, i);

                            //Receive the content of the file.
                            while (true)
                            {
                                try
                                {
                                    i = stream.Read(bytes, 0, bytes.Length);
                                    returnData = Encoding.ASCII.GetString(bytes, 0, i);

                                    //receiveBytes = this.com.fileUdpClient.Receive(ref fileIP);
                                    ////whithou ".Trim()" is better.
                                    //returnData = Encoding.ASCII.GetString(receiveBytes);
                                    if (returnData == "FILEEND")
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        //write.Write(receiveBytes, 0, receiveBytes.Length);
                                        write.Write(bytes, 0, i);
                                    }
                                }
                                catch (Exception)
                                {
                                    break;
                                }
                            }
                            write.Close();
                            MessageBox.Show("File Transfer Finished!");
                        }
                        else    //Not Receive
                        {
                            //this.com.fileUdpClient.Send();
                        }
                    }
                    else
                    {
                        MessageBox.Show("FILE TRANSFER WRONG in listenRemote_10087()");
                    }

                    stream.Close();
                    client.Close();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }
        
        // File transfer Button.
        private void button3_Click(object sender, EventArgs e)
        {
            if (this.chatPerson.Equals(""))
            {
                MessageBox.Show("Choose one guy you want to chat, Please!");
                return;
            }

            //choose the file you want to transfer.
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.ShowDialog();
            string fn = ofd.FileName.ToString();    //fn is in Absolute Path: "C:\\Users\\Administrator\\Documents\\a.txt"

            /*
            //Create a new thread in sendFile specially to send the file.
            //Since here is the MASTER THREAD, so we should create a new threads to sendFile and recvFile.
            //OR the main form may GOT STUCK.
            ParameterizedThreadStart pts = new ParameterizedThreadStart(sendFile);
            Thread thread3 = new Thread(pts);
            thread3.Start(fn);
            */

            //TCP
            ParameterizedThreadStart pts = new ParameterizedThreadStart(tcpSendFile);
            Thread thread6 = new Thread(pts);
            thread6.Start(fn);
        }

        //sendFile thread. Parent: Master(button3_Click).
        private void sendFile(Object arg)
        {
            string fileAbsoluteName = ((string)arg).Trim();
            int index = fileAbsoluteName.Length;
            string fileName = "";
            while (index-- > 0)
            {
                if (fileAbsoluteName[index] != '\\')
                {
                    fileName += fileAbsoluteName[index].ToString();
                }
                else    // '\\'
                {
                    break;
                }
            }
            char[] charArray = fileName.ToCharArray();
            Array.Reverse(charArray);
            fileName = new string(charArray);
            
            string content = "FILE:" + fileName;
            //byte[] chatPersonBytes = { };
            IPAddress chatIP = IPAddress.Parse(chatPerson);
            IPEndPoint chatIPEndPoint = new IPEndPoint(chatIP, 10087);
            byte[] sendByte = Encoding.Default.GetBytes(content);
            this.com.fileUdpClient.Send(sendByte, sendByte.Length, chatIPEndPoint);
            //Get the result whether the other want to receive.
            Byte[] receiveBytes = this.com.fileUdpClient.Receive(ref chatIPEndPoint);
            string returnData = Encoding.ASCII.GetString(receiveBytes).Trim();
            switch (returnData)
            {
                case "Yes":
                    {
                        FileStream read = new FileStream(fileAbsoluteName, FileMode.Open, FileAccess.Read);

                        byte[] buff = new byte[1024];
                        int length = 0;
                        while ((length = read.Read(buff, 0, 1024)) != 0)
                        {
                            this.com.fileUdpClient.Send(buff, length, chatIPEndPoint);
                        }
                        //Define a flag 'FILEEND' that means the end of the file.
                        buff = Encoding.Default.GetBytes("FILEEND");
                        this.com.fileUdpClient.Send(buff, 7, chatIPEndPoint);
                        read.Close();
                    }
                    break;
                case "No":
                    {
                        //Nothing to do.
                    }
                    break;
                case "FILE:":
                    {
                        //click but choose nothing.
                        //omit.
                    }
                    break;
                default:
                    {
                        MessageBox.Show("Neither Receive Nor Not Receive.");
                    }
                    break;
            }
        }

        //TCP: tcpSendFile thread. Parent: Master(button3_Click).
        private void tcpSendFile(Object arg)
        {
            try
            {
                string fileAbsoluteName = ((string)arg).Trim();
                int index = fileAbsoluteName.Length;
                string fileName = "";
                while (index-- > 0)
                {
                    if (fileAbsoluteName[index] != '\\')
                    {
                        fileName += fileAbsoluteName[index].ToString();
                    }
                    else    // '\\'
                    {
                        break;
                    }
                }
                char[] charArray = fileName.ToCharArray();
                Array.Reverse(charArray);
                fileName = new string(charArray);
                string content = "FILE:" + fileName;

                // Note: for this client to work you need to have a TcpServer 
                // that connected to the same address as specified by the (server, port) combination.
                int port = 10010;
                // remote IP adrress.
                string remoteIP = this.chatPerson;
                TcpClient client = new TcpClient(remoteIP, port); // TcpClient(string hostname, int port)
                
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
                // A client stream for reading and writing.
                NetworkStream stream = client.GetStream();
                stream.Write(data, 0, data.Length);

                //Get the result whether the other want to receive.

                Byte[] bytes = new Byte[1024];
                int i = stream.Read(bytes, 0, bytes.Length);
                string returnData = Encoding.ASCII.GetString(bytes, 0, i).Trim();

                switch (returnData)
                {
                    case "Yes":
                        {
                            FileStream read = new FileStream(fileAbsoluteName, FileMode.Open, FileAccess.Read);

                            byte[] buff = new byte[1024];
                            int length = 0;
                            while ((length = read.Read(buff, 0, 1024)) != 0)
                            {
                                //this.com.fileUdpClient.Send(buff, length, chatIPEndPoint);
                                stream.Write(buff, 0, length);
                            }
                            //Define a flag 'FILEEND' that means the end of the file.
                            buff = Encoding.Default.GetBytes("FILEEND");
                            //this.com.fileUdpClient.Send(buff, 7, chatIPEndPoint);
                            stream.Write(buff, 0, 7);
                            read.Close();
                        }
                        break;
                    case "No":
                        {
                            //Nothing to do.
                        }
                        break;
                    case "FILE:":
                        {
                            //click but choose nothing.
                            //omit.
                        }
                        break;
                    default:
                        {
                            MessageBox.Show("Neither Receive Nor Not Receive.");
                        }
                        break;
                }
                stream.Close();
                client.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        /*//recvFile thread. Parent: sendFile.
        private void recvFile()
        {
 
        }

        // File transfer.
        private void transfer(Object arg)
        {
            string fileName = (string)arg;
            string msg = "FILE:" + fileName;
            //初始化接受套接字：寻址方案，以字符流方式和Tcp通信
            Socket socketSent = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // 10086 OK? 10087 OK?
            IPEndPoint ipSent = new IPEndPoint(IPAddress.Parse(this.chatPerson), 10087);
            socketSent.Connect(ipSent);

            socketSent.Send(Encoding.Default.GetBytes(msg));

            //定义一个读文件流
            FileStream readFile = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            byte[] buff = new byte[1024];
            int len = 0;
            while ((len = readFile.Read(buff, 0, 1024)) != 0)
            {
                socketSent.Send(buff, 0, len, SocketFlags.None);
            }

            //将要发送信息的最后加上"END"标识符
            msg = "FILEEND";
            socketSent.Send(Encoding.Default.GetBytes(msg));

            readFile.Close();
            socketSent.Close();            
        }*/
    }
}
