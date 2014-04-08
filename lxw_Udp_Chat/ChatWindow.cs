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
using System.Security.Cryptography; //FOR ENCRYPT & DECRYPT.
using System.Timers;

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
        Rijndael fileRijndael = Rijndael.Create();
        #region NOT_USE "encrypt and decrypt"
        // 8 Bytes.
        private byte[] DESKey = {0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF};
        // Base64
        private string base64EncodeChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
        private int[] base64DecodeChars = new int[] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 62, -1, -1, -1, 63, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, -1, -1, -1, -1, -1, -1, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, -1, -1, -1, -1, -1, -1, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, -1, -1, -1, -1, -1 };//对应ASICC字符的位置
        #endregion

        //own encrypt
        //The last char in the IP address as the KEYWORD.
        private byte keyword = 0x30;
        //filesize.
        private float fileSize = 0.0f;
        //ack package.
        private byte[] ack = new byte[2];
        //whether in the OnTimerEvent Method.
        private bool inTimeEvent = false;
        //whether receive the ack package.
        private bool recvFlag = false;
        //Update the user-online list.
        private System.Timers.Timer updateUser = new System.Timers.Timer();

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Application.Exit();
        }

        public ChatWindow()
        {
            //ForIllegalCrossThreadCalls
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();

            this.updateUser.Elapsed += new ElapsedEventHandler(updateUserList);
            this.updateUser.Interval = 10000;
            this.updateUser.Enabled = true;
        }

        private void updateUserList(object source, ElapsedEventArgs e)
        {
            //NO Online User.
            if (com.olUser.Count == 0)
            {
                MessageBox.Show("Sorry, No online user now");
            }
            else
            {
                com.olUserArr = com.olUser.ToArray();
                this.comboBox1.Items.Clear();
                this.comboBox1.Items.AddRange((object[])com.olUserArr);
            }
        }

        private void ChatWindow_Load(object sender, EventArgs e)
        {
            //Once the window is loaded, broadcast.
            //this.com.receiveBroadcast();

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
            this.chatPerson = this.com.olUserArr.GetValue(this.comboBox1.SelectedIndex).ToString().Replace("Addr:", "");
            this.label1.Text = "Chatting with " + this.chatPerson;

            //Key is the last char of the IP who RECEIVE file.
            this.keyword = (byte)this.chatPerson[this.chatPerson.Length - 1];

            //Create a new thread to listen the user's input.
            ThreadStart ts1 = new ThreadStart(listenRemote_10086);
            Thread thread1 = new Thread(ts1);
            thread1.Start();
            
            
            //UDP            
            //IPEndPoint ipSent = new IPEndPoint(IPAddress.Parse(this.chatPerson), 10087);
            //socketSent.Connect(ipSent);
            //Create a new thread to listen the user's FILE TRANSFER INFOR.
            ThreadStart ts2 = new ThreadStart(listenRemote_10087);
            Thread thread2 = new Thread(ts2);
            //NOTE: Be of vital importance.
            thread2.SetApartmentState(ApartmentState.STA);
            thread2.Start();
            
            /*
            //TCP
            this.com.tcpListen = new TcpListener(IPAddress.Parse(this.chatPerson), 10010);
            ThreadStart ts5 = new ThreadStart(listenRemote_10010);
            Thread thread5 = new Thread(ts5);
            //NOTE: Be of vital importance.
            thread5.SetApartmentState(ApartmentState.STA);
            thread5.Start();
            */
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
                            string lastData = "";
                            while (true)
                            {
                                try
                                {
                                    receiveBytes = this.com.fileUdpClient.Receive(ref fileIP);

                                    //Decrypt the packet received.
                                    receiveBytes = decrypt(receiveBytes);
                                    returnData = Encoding.ASCII.GetString(receiveBytes);
                                                                        
                                    if (returnData == "FILEEND")
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        //send ACK.
                                        this.com.fileUdpClient.Send(Encoding.Default.GetBytes("OK"), 2, fileIP);

                                        if (lastData == returnData)
                                        {
                                            continue;
                                        }
                                        else
                                        {
                                            write.Write(receiveBytes, 0, receiveBytes.Length);
                                        }                                        
                                    }
                                    lastData = returnData;
                                }
                                catch (Exception e)
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
                    returnData = Encoding.ASCII.GetString(bytes, 0, i);

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
                            //bytes = new Byte[2048];

                            while (true)
                            {
                                try
                                {
                                    i = stream.Read(bytes, 0, bytes.Length);

                                    if (i == 0)
                                    {
                                        break;
                                    }

                                    /*byte[] copy = new byte[i];
                                    for (int j = 0; j < i; ++j)
                                    {
                                        copy[j] = bytes[j];
                                    }
                                    returnData = DecryptStringFromBytes(copy, this.fileRijndael.Key, this.fileRijndael.IV);
                                    */

                                    /*//DES
                                    returnData = Encoding.ASCII.GetString(bytes, 0, i);
                                    returnData = DecryptDES(returnData, Encoding.ASCII.GetString(this.DESKey));
                                    */
                                    
                                    /*
                                    //Base64
                                    returnData = Encoding.ASCII.GetString(bytes, 0, i);
                                    returnData = base64decode(returnData);*/

                                    //Other(except OWN) needs the following line.
                                    //byte[] contentBytes = Encoding.Default.GetBytes(returnData);

                                    //Own decrypt.
                                    bytes = decrypt(bytes);
                                    returnData = Encoding.ASCII.GetString(bytes, 0, i);

                                    if (returnData == "FILEEND")
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        write.Write(bytes, 0, i);
                                        //write.Write(contentBytes, 0, contentBytes.Length);
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
            
            // Show how much did the file send/receive.
            //FileInfo file = new FileInfo(fn);
            //this.fileSize = float.Parse(file.Length);     // file.Length: BYTE.
            
            //Create a new thread in sendFile specially to send the file.
            //Since here is the MASTER THREAD, so we should create a new threads to sendFile and recvFile.
            //OR the main form may GOT STUCK.
            ParameterizedThreadStart pts = new ParameterizedThreadStart(sendFile);
            Thread thread3 = new Thread(pts);
            thread3.Start(fn);

            /*
            //TCP
            ParameterizedThreadStart pts = new ParameterizedThreadStart(tcpSendFile);
            Thread thread6 = new Thread(pts);
            thread6.Start(fn);
            */
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
                        try
                        {
                            FileStream read = new FileStream(fileAbsoluteName, FileMode.Open, FileAccess.Read);

                            byte[] buff = new byte[4096];
                            int length = 0;

                            System.Timers.Timer timer = new System.Timers.Timer();
                            timer.Elapsed += new ElapsedEventHandler(OnTimeEvent);
                            timer.Interval = 200;

                            while ((length = read.Read(buff, 0, 4096)) != 0)
                            {
                                //Encrypt the packet to be sent.
                                buff = encrypt(buff);
                                while (true)
                                { 
                                    this.com.fileUdpClient.Send(buff, length, chatIPEndPoint);

                                    timer.Enabled = true;
                                    //Receive the ack packet.
                                    //this.ack = this.com.fileUdpClient.Receive(ref chatIPEndPoint);

                                    //wait until into the timer.
                                    while (!this.inTimeEvent)
                                    {
                                    }
                                    this.inTimeEvent = false;
                                    timer.Enabled = false;

                                    if (this.recvFlag)  //GET
                                    {
                                        break;  //next packet.
                                    }
                                    else    //NOT GET
                                    {
                                        continue;   //REPEAT this packet.
                                    }
                                }
                            }
                            //Define a flag 'FILEEND' that means the end of the file.
                            buff = Encoding.Default.GetBytes("FILEEND");
                            //Encrypt the packet to be sent.
                            buff = encrypt(buff);
                            this.com.fileUdpClient.Send(buff, 7, chatIPEndPoint);
                            read.Close();
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.ToString());
                        }
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

        //Timer receive.
        //Still into timerEvent Method, Even though the last run doesn't terminate.
        public void OnTimeEvent(object source, ElapsedEventArgs e)
        {
            //Receive the ack packet.
            IPAddress chatIP = IPAddress.Parse(chatPerson);
            IPEndPoint chatIPEndPoint = new IPEndPoint(chatIP, 10087);
            this.inTimeEvent = true;
            this.ack = this.com.fileUdpClient.Receive(ref chatIPEndPoint);
            this.recvFlag = true;
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

                Byte[] data = Encoding.ASCII.GetBytes(content);
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

                            float blockSize = 1024.0f;
                            int blockNum = 0;
                            float transRatio = 0.0f;

                            byte[] buff = new byte[1024];                           
                            int length = 0;
                            while ((length = read.Read(buff, 0, 1024)) != 0)
                            {
                                //stream.Write(buff, 0, length);
                                //own encrypt.
                                buff = encrypt(buff);
                                stream.Write(buff, 0, length);

                                /*//Rijndael
                                string str = Encoding.ASCII.GetString(buff, 0, length);
                                byte[] encrypted = EncryptStringToBytes(str, this.fileRijndael.Key, this.fileRijndael.IV);
                                //NOTE: After encrypt the LENGTH has changed.
                                //stream.Write(encrypted, 0, length);
                                stream.Write(encrypted, 0, encrypted.Length);
                                */

                                /*
                                //DES
                                string str = Encoding.ASCII.GetString(buff, 0, length);
                                str = EncryptDES(str, Encoding.ASCII.GetString(this.DESKey));
                                byte[] encrypted = Encoding.Default.GetBytes(str);
                                stream.Write(encrypted, 0, encrypted.Length);
                                */
                                

                                /*//Base64
                                string str = Encoding.ASCII.GetString(buff, 0, length);
                                str = base64encode(str);
                                byte[] encrypted = Encoding.Default.GetBytes(str);
                                stream.Write(encrypted, 0, encrypted.Length);*/
                            }
                            //Define a flag 'FILEEND' that means the end of the file.
                            
                            //buff = Encoding.Default.GetBytes("FILEEND");
                            //stream.Write(buff, 0, 7);

                            //own encrypt.
                            buff = Encoding.Default.GetBytes("FILEEND");
                            buff = encrypt(buff);
                            stream.Write(buff, 0, 7);

                            /*//Dijndael
                            buff = EncryptStringToBytes("FILEEND", this.fileRijndael.Key, this.fileRijndael.IV);
                            stream.Write(buff, 0, buff.Length);
                            */

                            /*//DES
                            buff = Encoding.Default.GetBytes(EncryptDES("FILEEND", Encoding.ASCII.GetString(this.DESKey)));
                            stream.Write(buff, 0, 0);*/                            
 
                            /*
                            //Base64
                            buff = Encoding.Default.GetBytes(base64encode("FILEEND"));
                            //For Base64 length will be changed into 12.
                            stream.Write(buff, 0, 12);//stream.Write(buff, 0, 7);
                            */

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

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void rToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        //Own encrypt.
        public byte[] encrypt(byte[] contentBytes)
        {
            int length = contentBytes.Length;
            for (int i = 0; i < length; ++i)
            {
                contentBytes[i] += this.keyword;
            }
            return contentBytes;
        }


        //Own decrypt.
        public byte[] decrypt(byte[] contentBytes)
        {
            int length = contentBytes.Length;
            for (int i = 0; i < length; ++i)
            {
                contentBytes[i] -= this.keyword;
            }
            return contentBytes;
        }

        //Base64
        public string base64encode(string str)
        { //加密
            string Out = "";
            int i = 0, len = str.Length;
            char c1, c2, c3;
            while (i < len)
            {
                c1 = Convert.ToChar(str[i++] & 0xff);
                if (i == len)
                {
                    Out += base64EncodeChars[c1 >> 2];
                    Out += base64EncodeChars[(c1 & 0x3) << 4];
                    Out += "==";
                    break;
                }
                c2 = str[i++];
                if (i == len)
                {
                    Out += base64EncodeChars[c1 >> 2];
                    Out += base64EncodeChars[((c1 & 0x3) << 4) | ((c2 & 0xF0) >> 4)];
                    Out += base64EncodeChars[(c2 & 0xF) << 2];
                    Out += "=";
                    break;
                }
                c3 = str[i++];
                Out += base64EncodeChars[c1 >> 2];
                Out += base64EncodeChars[((c1 & 0x3) << 4) | ((c2 & 0xF0) >> 4)];
                Out += base64EncodeChars[((c2 & 0xF) << 2) | ((c3 & 0xC0) >> 6)];
                Out += base64EncodeChars[c3 & 0x3F];
            }
            return Out;
        }
        public string utf16to8(string str)
        {
            string Out = "";
            int i, len;
            char c;//char为16位Unicode字符,范围0~0xffff,感谢vczh提醒
            len = str.Length;
            for (i = 0; i < len; i++)
            {//根据字符的不同范围分别转化
                c = str[i];
                if ((c >= 0x0001) && (c <= 0x007F))
                {
                    Out += str[i];
                }
                else if (c > 0x07FF)
                {
                    Out += (char)(0xE0 | ((c >> 12) & 0x0F));
                    Out += (char)(0x80 | ((c >> 6) & 0x3F));
                    Out += (char)(0x80 | ((c >> 0) & 0x3F));
                }
                else
                {
                    Out += (char)(0xC0 | ((c >> 6) & 0x1F));
                    Out += (char)(0x80 | ((c >> 0) & 0x3F));
                }
            }
            return Out;
        }

        public string base64decode(string str)
        {//解密
            int c1, c2, c3, c4;
            int i, len;
            string Out;
            len = str.Length;
            i = 0; Out = "";
            while (i < len)
            {
                do
                {
                    c1 = base64DecodeChars[str[i++] & 0xff];
                } while (i < len && c1 == -1);
                if (c1 == -1) break;
                do
                {
                    c2 = base64DecodeChars[str[i++] & 0xff];
                } while (i < len && c2 == -1);
                if (c2 == -1) break;
                Out += (char)((c1 << 2) | ((c2 & 0x30) >> 4));
                do
                {
                    c3 = str[i++] & 0xff;
                    if (c3 == 61) return Out;
                    c3 = base64DecodeChars[c3];
                } while (i < len && c3 == -1);
                if (c3 == -1) break;
                Out += (char)(((c2 & 0XF) << 4) | ((c3 & 0x3C) >> 2));
                do
                {
                    c4 = str[i++] & 0xff;
                    if (c4 == 61) return Out;
                    c4 = base64DecodeChars[c4];
                } while (i < len && c4 == -1);
                if (c4 == -1) break;
                Out += (char)(((c3 & 0x03) << 6) | c4);
            }
            return Out;
        }

        public string utf8to16(string str)
        {
            string Out = "";
            int i, len;
            char c, char2, char3;
            len = str.Length;
            i = 0; while (i < len)
            {
                c = str[i++];
                switch (c >> 4)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    case 7: Out += str[i - 1]; break;
                    case 12:
                    case 13: char2 = str[i++];
                        Out += (char)(((c & 0x1F) << 6) | (char2 & 0x3F)); break;
                    case 14: char2 = str[i++];
                        char3 = str[i++];
                        Out += (char)(((c & 0x0F) << 12) | ((char2 & 0x3F) << 6) | ((char3 & 0x3F) << 0)); break;
                }
            }
            return Out;
        }
        
        // DES Algorithm
        // DES Encrypt.
        // <param name="encryptString">待加密的字符串</param>
        // <param name="encryptKey">加密密钥,要求为8位</param>
        // <returns>加密成功返回加密后的字符串，失败返回源串</returns>
        public string EncryptDES(string encryptString, string encryptKey)
        {
            try
            {
                byte[] rgbKey = Encoding.UTF8.GetBytes(encryptKey.Substring(0, 8));
                byte[] rgbIV = this.DESKey;//Keys;
                byte[] inputByteArray = Encoding.UTF8.GetBytes(encryptString);
                DESCryptoServiceProvider dCSP = new DESCryptoServiceProvider();
                MemoryStream mStream = new MemoryStream();
                CryptoStream cStream = new CryptoStream(mStream, dCSP.CreateEncryptor(rgbKey, rgbIV), CryptoStreamMode.Write);
                cStream.Write(inputByteArray, 0, inputByteArray.Length);
                //cStream.FlushFinalBlock();
                return Convert.ToBase64String(mStream.ToArray());
            }
            catch
            {
                return encryptString;
            }
        }

        // DES Decrypt.
        /// <param name="decryptString">待解密的字符串</param>
        /// <param name="decryptKey">解密密钥,要求为8位,和加密密钥相同</param>
        /// <returns>解密成功返回解密后的字符串，失败返源串</returns>
        public string DecryptDES(string decryptString, string decryptKey)
        {
            try
            {
                byte[] rgbKey = Encoding.UTF8.GetBytes(decryptKey);
                byte[] rgbIV = this.DESKey;//Keys;
                byte[] inputByteArray = Convert.FromBase64String(decryptString);
                DESCryptoServiceProvider DCSP = new DESCryptoServiceProvider();
                MemoryStream mStream = new MemoryStream();
                CryptoStream cStream = new CryptoStream(mStream, DCSP.CreateDecryptor(rgbKey, rgbIV), CryptoStreamMode.Write);
                cStream.Write(inputByteArray, 0, inputByteArray.Length);
                //cStream.FlushFinalBlock();
                return Encoding.UTF8.GetString(mStream.ToArray());
            }
            catch
            {
                return decryptString;
            }
        }


        // Rijndael Algorithm
        //Encrpt
        public byte[] EncryptStringToBytes(string plainText, byte[] Key, byte[] IV)
        {
            int length = plainText.Length;
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");

            byte[] encrypted = Encoding.Default.GetBytes(plainText);
            try
            {                
                // Create an Rijndael object with the specified key and IV.
                using (Rijndael rijAlg = Rijndael.Create())
                {
                    rijAlg.Key = Key;
                    rijAlg.IV = IV;

                    // Create a decrytor to perform the stream transform.
                    ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

                    // Create the streams used for encryption.
                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                //Write all data to the stream.
                                swEncrypt.Write(plainText);
                            }
                            encrypted = msEncrypt.ToArray();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            // Return the encrypted bytes from the memory stream.
            return encrypted;
        }

        //Decrypt
        public string DecryptStringFromBytes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");

            // Declare the string used to hold the decrypted text.
            string plaintext = null;

            try
            {
                // Create an Rijndael object with the specified key and IV.
                using (Rijndael rijAlg = Rijndael.Create())
                {
                    rijAlg.Key = Key;
                    rijAlg.IV = IV;

                    // Create a decrytor to perform the stream transform.
                    ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                    // Create the streams used for decryption.
                    using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                // Read the decrypted bytes from the decrypting stream and place them in a string.
                                plaintext = srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            return plaintext;
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