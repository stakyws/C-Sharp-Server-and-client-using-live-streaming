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

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Thread T1server = new Thread(Tcpclient);
            T1server.Start();
        }
        bool videoIsStreaming = false;
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        TcpClient CopyClient;
        NetworkStream stream;
        void Tcpclient()
        {
            CopyClient = new TcpClient("127.0.0.1", 5050);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
            textBox1.Text = Path.GetExtension(openFileDialog1.FileName);
            if (textBox1.Text == ".png" || textBox1.Text == ".jpg" || textBox1.Text == ".jpeg" || textBox1.Text == ".webp" || textBox1.Text == ".ico" && pictureBox1.Image != null) 
            {
                FileStream fs = new FileStream(openFileDialog1.FileName, FileMode.Open);

                pictureBox1.Image = Image.FromStream(fs);
                fs.Close();
            }
            else if (textBox1.Text == ".txt")
                richTextBox1.Text = File.ReadAllText(openFileDialog1.FileName);
            else
                MessageBox.Show("عنوان الملف غير صالح");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            byte[] Mytext = Encoding.UTF8.GetBytes(textBox1.Text);
            CopyClient.Client.Send(Mytext);

            FileStream myfile = new FileStream(openFileDialog1.FileName,FileMode.Open);
            byte[] mybytes = new byte[myfile.Length];
            myfile.Read(mybytes, 0, mybytes.Length);
            CopyClient.Client.Send(mybytes);
            myfile.Close();
            MessageBox.Show(" تم إرسال الملفات بنجاح ");
        }
        //...............................................................................//
        // استقبال اللايف
        private void button1_Click(object sender, EventArgs e)
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
                        {
                            
                            // يمكنك إضافة معالجة للخطأ هنا
                        }
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


        //...............................................................................//
        //بدأ بث اللايف
        private void button1_Click_1(object sender, EventArgs e)
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
                //عمل نسخه من إطار الصوره الحالي داخل الخيط نفسه لتجنب مشاكل الوصول المتزامن
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
                        // باستخدامها تقوم بعمل نسخه من الصوره الحاليه لضمان عدم التأثير على الصورة الأصلية : clon()
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
                                {
                                   
                                }
                            }
                            catch
                            {
                               
                            }
                        }
                    }
                }
            }
        }

        private ImageCodecInfo GetEncoderInfo(ImageFormat format)
        {
            //هذه الدالة تقوم بالبحث واسترجاع معلومات محول تشفير الصورة المناسب لتنسيق الصورة المعطاة
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

        private void button4_Click(object sender, EventArgs e)
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                pictureBox1.Image = Image.FromFile("C:\\Users\\Smart\\Desktop\\ALL_FOLDER\\IMG\\anime\\5.jpeg");
            }
        }

        //...............................................................................//



    }
}
