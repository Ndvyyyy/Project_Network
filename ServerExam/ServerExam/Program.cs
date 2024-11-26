using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerExam
{
    internal class Program
    {
        private const int receivePort = 9050;
        private const int sendPort = 9060;
        private const string directoryPath = @"C:\DinhVy-ThanhDong\ImageServer";
        private static ConcurrentQueue<string> receivedFilePaths = new ConcurrentQueue<string>();

        static void Main(string[] args)
        {
            // Kiểm tra và tạo thư mục nếu nó chưa tồn tại
            if (!Directory.Exists(directoryPath))
            {
                try
                {
                    Directory.CreateDirectory(directoryPath);
                    Console.WriteLine($"Created directory: {directoryPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating directory: {ex.Message}");
                    return; // Thoát khỏi phương thức nếu có lỗi
                }
            }

            // Start the server to receive images from clients
            Task.Run(() => StartReceiveServer(receivePort));
            // Start the server to send images to clients
            Task.Run(() => StartSendServer(sendPort));

            Console.WriteLine("Server started. Listening for connections...");
            Console.ReadLine(); // Keep the server running until Enter is pressed
        }

        static void StartReceiveServer(int port)
        {
            TcpListener listener = null;
            try
            {
                IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);
                listener = new TcpListener(localEndPoint);
                listener.Start();
                Console.WriteLine($"Receive server listening for connections on port {port}...");

                // Listen for requests from clients
                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    Console.WriteLine($"Client connected from {((IPEndPoint)client.Client.RemoteEndPoint).Address}:{((IPEndPoint)client.Client.RemoteEndPoint).Port}");
                    Task.Run(() => HandleClientReceiveRequest(client));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Receive server error: {ex.Message}");
            }
            finally
            {
                listener?.Stop();
            }
        }

        static void HandleClientReceiveRequest(TcpClient client)
        {
            NetworkStream networkStream = client.GetStream();
            try
            {
                // Read the file name size from the client
                byte[] fileNameSizeBytes = new byte[4];
                networkStream.Read(fileNameSizeBytes, 0, 4);
                int fileNameSize = BitConverter.ToInt32(fileNameSizeBytes, 0);

                // Read the file name from the client
                byte[] fileNameBytes = new byte[fileNameSize];
                networkStream.Read(fileNameBytes, 0, fileNameSize);
                string fileName = Encoding.UTF8.GetString(fileNameBytes);

                // Read the file size from the client
                byte[] fileSizeBytes = new byte[4];
                networkStream.Read(fileSizeBytes, 0, 4);
                int fileSize = BitConverter.ToInt32(fileSizeBytes, 0);

                // Read the file data from the client
                byte[] fileData = new byte[fileSize];
                int bytesRead = 0;
                while (bytesRead < fileSize)
                {
                    int read = networkStream.Read(fileData, bytesRead, fileSize - bytesRead);
                    if (read == 0)
                    {
                        throw new Exception("Client disconnected unexpectedly.");
                    }
                    bytesRead += read;
                }

                // Save the received file to disk
                string filePath = Path.Combine(directoryPath, fileName);
                File.WriteAllBytes(filePath, fileData);
                receivedFilePaths.Enqueue(filePath);

                // Check if fileName contains computer name
                string namePC = GetComputerNameFromFileName(fileName);

                // Create directory path for the computer if it doesn't exist
                string computerDirectoryPath = Path.Combine(directoryPath, namePC);
                if (!Directory.Exists(computerDirectoryPath))
                {
                    Directory.CreateDirectory(computerDirectoryPath);
                    Console.WriteLine($"Created directory for {namePC}: {computerDirectoryPath}");
                }

                // Combine directory path with file name
                filePath = Path.Combine(computerDirectoryPath, fileName);

                // Write file data to the filePath
                File.WriteAllBytes(filePath, fileData);
                receivedFilePaths.Enqueue(filePath);

                Console.WriteLine($"Received '{fileName}' successfully at port {receivePort}, SAVE at {filePath}");
            
                // Close the connection
                networkStream.Close();
                client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to receive file: {ex.Message}");
            }
        }

        // Function to extract computer name from file name
        static string GetComputerNameFromFileName(string fileName)
        {
            int index = fileName.IndexOf("--");
            if (index != -1)
            {
                return fileName.Substring(0, index);
            }
            else
            {
                // If '--' is not found, return the full fileName (or handle error)
                return fileName;
            }
        }

        // send image to parent client
        static void StartSendServer(int port)
        {
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

            using (Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                try
                {
                    listener.Bind(localEndPoint);
                    listener.Listen(10);
                    Console.WriteLine($"Send server listening for connections on port {port}...");

                    while (true)
                    {
                        Socket client = listener.Accept();
                        Task.Run(() => HandleSendClientRequest(client));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        static void HandleSendClientRequest(Socket client)
        {
            try
            {
                byte[] data = new byte[1024];
                int recv = client.Receive(data); // Số byte nhận

                if (recv == 0)
                {
                    client.Close();
                    return;
                }

                string s = Encoding.ASCII.GetString(data, 0, recv);
                Console.WriteLine($"Received request for files from: {s}");

                // Lọc danh sách các file có tên bắt đầu với targetComputerName
                var filteredFilePaths = FilterFilesByComputerName(s);

                if (filteredFilePaths.Count != 0)
                {
                    // Không có file nào để gửi
                    string input = "YES";
                    data = Encoding.ASCII.GetBytes(input);
                    client.Send(data, data.Length, SocketFlags.None);
                    foreach (var filePath in filteredFilePaths)
                    {

                        NetworkStream networkStream = new NetworkStream(client);
                        // Read the file data
                        byte[] fileData = File.ReadAllBytes(filePath);
                        Console.WriteLine($"Read file '{Path.GetFileName(filePath)}' successfully.");

                        // Get the file name
                        string fileName = Path.GetFileName(filePath);

                        // Send the file name size to the client
                        byte[] fileNameSize = BitConverter.GetBytes(fileName.Length);
                        networkStream.Write(fileNameSize, 0, 4);
                        Console.WriteLine($"Sent file name size: {BitConverter.ToInt32(fileNameSize, 0)}");

                        // Send the file name to the client
                        byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
                        networkStream.Write(fileNameBytes, 0, fileNameBytes.Length);
                        Console.WriteLine($"Sent file name: {fileName}");
                        // Send the file size to the client
                        byte[] fileSize = BitConverter.GetBytes(fileData.Length);
                        networkStream.Write(fileSize, 0, fileSize.Length);
                        Console.WriteLine($"Sent file size: {BitConverter.ToInt32(fileSize, 0)} bytes");

                        // Send the file data to the client
                        networkStream.Write(fileData, 0, fileData.Length);

                        Console.WriteLine($"Sent '{fileName}' to client on port {sendPort}");
                        Console.WriteLine($"--------------------------------------------------------------------------------");
                        networkStream.Close();
                    }

                }
                else
                {
                    string input = "NULL";
                    data = Encoding.ASCII.GetBytes(input);
                    client.Send(data, data.Length, SocketFlags.None);
                    client.Close();
                }              
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending file: {ex.Message}");
            }
          
        }

        static List<string> FilterFilesByComputerName(string targetComputerName)
        {
            var filteredFilePaths = new List<string>();

            foreach (var filePath in receivedFilePaths)
            {
                string fileName = Path.GetFileName(filePath);
                string computerName = fileName.Split(new[] { "--" }, StringSplitOptions.None)[0];

                if (computerName.Equals(targetComputerName, StringComparison.OrdinalIgnoreCase))
                {
                    filteredFilePaths.Add(filePath);
                }
            }

            return filteredFilePaths.Count > 0 ? filteredFilePaths : new List<string>();
        }
    }

}
