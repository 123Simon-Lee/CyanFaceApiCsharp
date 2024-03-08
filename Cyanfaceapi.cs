using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

// Definition of a struct to represent the header of a message.
public struct HEADCALL
{
    public int len;
    public short reqType;
    public short errorType;
    public short v1;
}
class SocketClient
{

    private Socket clientSocket;
    private IPEndPoint serverEndPoint;
    public const int ServerPort = 8061;
    private byte[] recvBuffer = new byte[1024];
    // Array to store the processed data.
    public static float[] ExpressDat;
    // Converts a slice of the received byte array to a single precision floating point number.
    private float ReadFloat32(byte[] buffer, int offset)
        {
            float int_value;
            byte[] charList = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                if (System.BitConverter.IsLittleEndian)
                {
                    charList[i] = buffer[i + offset];
                }
                else
                {
                // Adjust byte order for big endian systems.
                charList[i] = buffer[offset + (charList.Length - 1 - i)]; 
                }
            }
            int_value = System.BitConverter.ToSingle(charList, 0);

            return int_value;
        }
    // Similar conversion methods for reading Int32 and Int16 values from the byte array.
    private int ReadInt32(byte[] buffer, int offset)
        {
            if (buffer.Length - offset < 4)
            {
                return 0;
            }
            int int_value;
            byte[] charList = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                if (System.BitConverter.IsLittleEndian)//Little Endian 小端序
                {
                    charList[i] = buffer[i + offset];
                }
                else//Big Endian 大端序
                {
                    charList[i] = buffer[offset + (charList.Length - 1 - i)]; // 调整字节顺序
                }
            }
            int_value = System.BitConverter.ToInt32(charList, 0);
            return int_value;
        }
    private short ReadInt16(byte[] buffer, int offset)
        {
            if (buffer.Length - offset < 2)
            {
                return 0;
            }
            short shortValue;
            byte[] charList = new byte[2];
            for (int i = 0; i < 2; i++)
            {
                if (BitConverter.IsLittleEndian)
                {
                    charList[i] = buffer[i + offset];
                }
                else
                {
                    charList[i] = buffer[offset + (charList.Length - 1 - i)]; // 调整字节顺序
                }
            }
            shortValue = System.BitConverter.ToInt16(charList, 0);
            return shortValue;
        }

    // Attempts to connect to the server with the specified address and port.
    public bool Connect(string serverAddress, int port)
    {

        ExpressDat = new float[53];
        serverEndPoint = new IPEndPoint(IPAddress.Parse(serverAddress), port);
        clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try
        {

            clientSocket.Connect(serverEndPoint);
            Console.WriteLine("Connected to server Succ.");
            // Send an initial message upon connection.
            HEADCALL DL_head = new HEADCALL();
            DL_head.len = 12;
            DL_head.reqType = 1;
            DL_head.errorType = 0;
            DL_head.v1 = 1;
            Send(StructureToByteArray(DL_head));
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Connection failed: {e.Message}");
            return false;
        }
    }


    // Sends the provided byte array over the socket.
    public void Send(byte[] sendData)
    {
        try
        {
            clientSocket.Send(sendData);
        }
        catch(Exception e)
        {
            Console.WriteLine("Error sending message: " + e.Message);
        }
    }
    // Receives data from the socket and processes it.
    public void Receive()
    {
        try
        {
            int receivedBytes = clientSocket.Receive(recvBuffer);
            if (receivedBytes > 0)
            {
                // Process received message as needed
                int pshift = 0;
                int len = ReadInt32(recvBuffer, pshift); pshift += 4;
                short reqType = ReadInt16(recvBuffer, pshift); pshift += 2;
                short errorType = ReadInt16(recvBuffer, pshift); pshift += 2;
                pshift += 16;
                if (len > 24)
                {
                    CopyFromexpressionStream(recvBuffer, pshift);
                    for (int i = 0; i < 53; i++)
                    {
                        Console.WriteLine($"ExpressDat[{i}] :{ExpressDat[i]}");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Close();
            Console.WriteLine(e.Message);
        }
            


    }
    // Closes the socket and releases resources.
    public void Close()
    {
            if (clientSocket != null && clientSocket.Connected)
            {
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
            }
    }
    // Converts a struct to a byte array for sending.
    private byte[] StructureToByteArray(object obj)
    {
        int length = Marshal.SizeOf(obj);
        byte[] byteArray = new byte[length];
        IntPtr ptr = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr(obj, ptr, true);
        Marshal.Copy(ptr, byteArray, 0, length);
        Marshal.FreeHGlobal(ptr);
        return byteArray;
    }
    // Processes a stream of bytes into the ExpressDat array.
    private void CopyFromexpressionStream(byte[] ReceiveData, int pshift)
    {
        if(ExpressDat.Length>0)
        {
            for(int i=0;i<53;i++)
            {
                float temp = (ReadFloat32(ReceiveData, pshift));
                pshift += 4;
                ExpressDat[i] = temp;
            }
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        SocketClient client = new SocketClient();
        bool Isconnected = client.Connect("192.168.0.12", SocketClient.ServerPort);
        while (true)
        {
            if(Isconnected)
            {
                client.Receive();
            }
           
        }
        client.Close();
    }
}



