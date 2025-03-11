using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Drawing;
using AForge.Video;
using AForge.Video.DirectShow;

namespace Server1
{
    public partial class Form1 : Form
    {
        bool videoIsStreaming = false;
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        NetworkStream stream;

        public Form1()
        {
            InitializeComponent();

            Thread t1 = new Thread(TcpServer);
            t1.Start();
        }
        TcpClient CopyClient;
        TcpListener Myserver;
        void TcpServer()
        {
            Myserver = new TcpListener(5050);
            Myserver.Start();
            CopyClient = Myserver.AcceptTcpClient();
        }
        private void button3_Click(object sender, EventArgs e)
        {
            byte[] Mytext = new byte[4];
            CopyClient.Client.Receive(Mytext);
            textBox1.Text = Encoding.UTF8.GetString(Mytext);

            byte[] mybytes = new byte[999999];
            CopyClient.Client.Receive(mybytes);
            if (textBox1.Text == ".png" || textBox1.Text == ".jpg")
            {
                MemoryStream ms = new MemoryStream(mybytes);
                pictureBox1.Image = Image.FromStream(ms);
            }
            else
                richTextBox1.Text = Encoding.UTF8.GetString(mybytes);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //if (textBox1.Text == ".png" || textBox1.Text == ".jpg")
            //    pictureBox1.Image.Save(textBox1.Text);
            //else
            //    //richTextBox1.
        }

        //...............................................................................//
        //بدأ بث اللايف
        private void button1_Click(object sender, EventArgs e)
        {
            pictureBox2.Visible = false;
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (videoDevices.Count > 0)
            {
                videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
                videoSource.NewFrame += new NewFrameEventHandler(video_NewFrame);
                videoSource.Start();

                // يجب بدء تنفيذ البث للعميل على Thread منفصل لتجنب تجميد الواجهة الرسومية
                Thread clientStreamingThread = new Thread(HandleClientConnection);
                clientStreamingThread.Start();
            }
            else
            {
                MessageBox.Show("No connected camera found.");
            }
        }

        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (pictureBox1.InvokeRequired)
            {
                pictureBox1.Invoke(new MethodInvoker(delegate { pictureBox1.Image = (Bitmap)eventArgs.Frame.Clone(); }));
            }
            else
            {
                pictureBox1.Image = (Bitmap)eventArgs.Frame.Clone();
            }
        }

        private void HandleClientConnection()
        {
            while (true)
            {
                Image currentFrame = null;

                if (pictureBox1.Image == null)
                {
                    Thread.Sleep(100);
                    continue;
                }

                if (pictureBox1.Image != null)
                {
                    if (pictureBox1.InvokeRequired)
                    {
                        pictureBox1.Invoke(new MethodInvoker(delegate
                        {
                            currentFrame = (Image)pictureBox1.Image.Clone();
                        }));
                    }
                    else
                    {
                        currentFrame = (Image)pictureBox1.Image.Clone();
                    }

                    if (currentFrame != null)
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            try
                            {
                                currentFrame.Save(ms, ImageFormat.Jpeg); // حفظ الإطار الحالي كصورة JPEG دون تحديد جودة
                                byte[] imageData = ms.ToArray();

                                try
                                {
                                    // إرسال البيانات إلى العميل
                                    CopyClient.Client.Send(imageData);
                                }
                                catch
                                {}
                            }
                            catch
                            {}
                        }
                    }
                }
            }
        }

        private ImageCodecInfo GetEncoderInfo(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        private void UpdatePictureBoxImage(Image image)
        {
            if (pictureBox1.InvokeRequired)
            {
                pictureBox1.Invoke((MethodInvoker)delegate
                {
                    pictureBox1.Image = image;
                });
            }
            else
            {
                pictureBox1.Image = image;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                pictureBox1.Image = Image.FromFile("C:\\Users\\Smart\\Desktop\\ALL_FOLDER\\IMG\\anime\\5.jpeg");
            }
        }
        //...............................................................................//
        // بدأ كود إستقبال اللايف
        private void button5_Click(object sender, EventArgs e)
        {
            pictureBox2.Visible = true;
            stream = CopyClient.GetStream();

            // يجب بدء تنفيذ الاستقبال على Thread منفصل لتجنب تجميد واجهة المستخدم
            Thread receiveThread = new Thread(ReceiveData);
            receiveThread.Start();
        }

        private void ReceiveData()
        {
            while (true)
            {
                byte[] imageData = new byte[1000000]; // حجم الصورة المتوقع
                //Invoke(new Action(() =>
                //{
                int bytesRead = stream.Read(imageData, 0, imageData.Length);

                if (bytesRead > 0)
                {
                    using (MemoryStream ms = new MemoryStream(imageData, 0, bytesRead))
                    {
                        try
                        {
                            Image receivedImage = Image.FromStream(ms);
                            UpdatePictureBoxImage1(receivedImage);
                        }
                        catch
                        {}
                    }
                }
                //}));
            }
        }

        private void UpdatePictureBoxImage1(Image image)
        {
            if (pictureBox2.InvokeRequired)
            {
                pictureBox2.Invoke((MethodInvoker)delegate
                {
                    pictureBox2.Image = image;
                });
            }
            else
            {
                pictureBox2.Image = image;
            }
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                pictureBox2.Visible = false;
            }
        }
        // إنتهاء كود إستقبال بث اللايف

        //...............................................................................//
    }
}
