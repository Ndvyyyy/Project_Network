using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

class Parent_Client
{
    private const string serverAddress = "127.0.0.1";
    private const int port = 9060;
    private const string directoryPath = @"C:\DinhVy-ThanhDong\ImageParentClient";

    static void Main()
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
        while (true)
        {
            try
            {
                using (TcpClient client = new TcpClient(serverAddress, port))
                {
                    Console.WriteLine("Connected to server.");

                    // Nhập tên máy tính và chuyển thành chữ in hoa
                    Console.Write("Enter computer name: ");
                    string input = Console.ReadLine().ToUpper();
                    byte[] data = Encoding.ASCII.GetBytes(input);
                    client.GetStream().Write(data, 0, data.Length);

                    // Nhận phản hồi từ máy chủ
                    byte[] buffer = new byte[1024];
                    int bytesRead = client.GetStream().Read(buffer, 0, buffer.Length);
                    string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    Console.WriteLine(response);

                    if (response == "YES")
                    {
                        using (NetworkStream networkStream = client.GetStream())
                            try
                            {
                                while (true)
                                { // Read the file name size from the server
                                    byte[] fileNameSizeBytes = new byte[4];
                                    networkStream.Read(fileNameSizeBytes, 0, 4);
                                    int fileNameSize = BitConverter.ToInt32(fileNameSizeBytes, 0);
                                    Console.WriteLine($"Received file name size: {fileNameSize}");

                                    // Read the file name from the server
                                    byte[] fileNameBytes = new byte[fileNameSize];
                                    networkStream.Read(fileNameBytes, 0, fileNameSize);
                                    string fileName = Encoding.UTF8.GetString(fileNameBytes);
                                    Console.WriteLine($"Received file name: {fileName}");

                                    // Read the file size from the server
                                    byte[] fileSizeBytes = new byte[4];
                                    networkStream.Read(fileSizeBytes, 0, 4);
                                    int fileSize = BitConverter.ToInt32(fileSizeBytes, 0);
                                    Console.WriteLine($"Received file size: {fileSize} bytes");

                                    // Save the received file to disk
                                    byte[] fileData = new byte[fileSize];
                                    int totalBytesRead = 0;
                                    while (totalBytesRead < fileSize)
                                    {
                                        bytesRead = networkStream.Read(fileData, totalBytesRead, fileSize - totalBytesRead);
                                        if (bytesRead == 0)
                                        {
                                            break; // Exit the loop if the server has closed the connection
                                        }
                                        totalBytesRead += bytesRead;
                                    }

                                    // Save the received file to disk
                                    string savePath = Path.Combine(directoryPath, fileName);
                                    File.WriteAllBytes(savePath, fileData);

                                    Console.WriteLine($"Received '{fileName}' successfully. SAVE at {savePath}");
                                    Console.WriteLine($"--------------------------------------------------------------------------------");
                                }
                            }
                            catch (IOException ex)
                            {
                                // Handle exceptions (e.g., connection closed)
                                Console.WriteLine($"Error: {ex.Message}");
                            }
                    }
                    else
                    {
                            Console.WriteLine("Server response is NULL. Please input again.");
                        }               
                    } 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending file: {ex.Message}");
            }
        }
        
    }
}
