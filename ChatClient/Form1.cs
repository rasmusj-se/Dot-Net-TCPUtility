using System;
using System.Windows.Forms;

namespace TCPUtility.ChatClient
{
    public partial class Form1 : Form
    {

        TCPClient client;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try {
                client.Connect();
                button2.Enabled = true;
            }
            catch (Exception err)
            {
                MessageBox.Show("Error: " + err.Message);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //Create a client with encryption, Note: Use TCPSecurity.EncryptedAndVerified to improve security (this requires root CA to be trusted and commonname to match with hostname)
            client = new TCPClient(textBox3.Text, 1337, TCPSecurity.Encrypted);
            client.MessageReceived += Client_MessageReceived;
        }


        //This invoke is required as the message receive handler is running in another thread
        delegate void AddMessageCallback(string text);

        private void AddMessage(string text)
        {
            if (this.textBox2.InvokeRequired)
            {
                AddMessageCallback d = new AddMessageCallback(AddMessage);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.textBox2.Text += text + Environment.NewLine + Environment.NewLine;
            }
        }

        private void Client_MessageReceived(object sender, MessageReceivieEventArgs e)
        {
            //Add incoming messages to chat
            AddMessage(e.Message);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            client.Send(textBox1.Text);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Gracefully shutdown client on exit
            client.Close();
        }
    }
}
