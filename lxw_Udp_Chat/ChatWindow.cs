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

namespace lxw_Udp_Chat
{
    public partial class ChatWindow : Form
    {
        private Communicate com = new Communicate();
        // Mutex.
        private Mutex mut = new Mutex();


        private string chatPerson = "";     // The current person(IP Adress in string) who you are chatting with.

        public ChatWindow()
        {
            //线程间操作无效
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
            ThreadStart ts1 = new ThreadStart(listenRemote);
            Thread thread1 = new Thread(ts1);
            thread1.Start();
        }

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
        public void updateRichTextBox1(string recvContent)
        {
            this.richTextBox1.Text += recvContent + "\r\n";
        }

        //Listen to the user's input. NEVER STOP.
        //public void receiveContent()
        public void listenRemote()
        {
            try
            {
                IPEndPoint chatPersonIP = new IPEndPoint(IPAddress.Parse(this.chatPerson), 11000);
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

            //MUTEX   Notify both of the peer that file will be transfered.
            this.mut.WaitOne();

            this.richTextBox1.Text += fn + ": file is being transfered. You can continue your chatting.\r\n";

            this.mut.ReleaseMutex();
            
            //Create a new thread specially to transfer the file.
            ThreadStart ts2 = new ThreadStart(transfer);
            Thread thread2 = new Thread(ts2);
            thread2.Start();
            
        }

        // File transfer.
        private void transfer()
        {
 
        }
    }
}
