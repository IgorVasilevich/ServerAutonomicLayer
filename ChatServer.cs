using ChatClient;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerAutonomicLayer
{
    class ChatServer
    {
        private TcpListener listener;
        private MessageAction repository;
        string connectionString = @"Data Source=INGVAR\SQLEXPRESS;Integrated security = True;Initial Catalog=chatDB";
        public ChatServer()
        {
            listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 8009);
            repository = new MessageAction(connectionString);
        }

        public void Start()
        {
            Thread thread = new Thread(ServerProc);
            thread.Start();
        }

        private void ServerProc()
        {
            listener.Start();
            while (true)
            {
                var client = listener.AcceptTcpClient();
                Thread clientThread = new Thread(ClietProc);
                clientThread.Start(client);
            }
        }

        private void ClietProc(object state)
        {
            var client = (TcpClient)state;
            var stream = client.GetStream();
            var ep = (IPEndPoint)client.Client.LocalEndPoint;
            using (BinaryReader reader = new BinaryReader(stream))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    while (true)
                    {
                        var request = (Requests)reader.ReadInt32();
                        switch (request)
                        {
                            case Requests.Join:
                                Console.WriteLine($"{ep.Address} joined to chat!");
                                writer.Write((int)Requests.GetUsers);
                                break;
                            case Requests.Leave:
                                string userLeave = reader.ReadString();
                                UserLeave(userLeave, writer, reader, connectionString, ep);
                                return;
                            case Requests.GetMessages:
                                GetMessages(writer);
                                break;
                            case Requests.SendMessage:
                                repository.SendMessages(reader.ReadString());
                                break;
                            case Requests.GetUsers:
                                GetUsers(writer);
                                break;
                            case Requests.LogIn:
                                LogIn(writer, reader);
                                break;
                            case Requests.PrivateMessage:
                                PrivateMessage(writer, reader);
                                break;
                            case Requests.Reg:
                                Reg(writer, reader);
                                break;
                        }
                    }
                }
            }
        }

        private void Reg(BinaryWriter writer, BinaryReader reader)
        {
            string check = repository.Reg(reader.ReadString(), reader.ReadString());
            if (check == "1")
                writer.Write((int)Requests.Reg);
            else
                writer.Write((int)Requests.NotReg);
            writer.Write((int)Requests.GetUsers);
            writer.Flush();
        }

        private void PrivateMessage(BinaryWriter writer, BinaryReader reader)
        {
            writer.Write((int)Requests.PrivateMessage);
            writer.Write(reader.ReadString());
            writer.Write(reader.ReadString());
            writer.Write(reader.ReadString());
            writer.Flush();
        }

        private void LogIn(BinaryWriter writer, BinaryReader reader)
        {
            string user = reader.ReadString();
            string password = reader.ReadString();
            string result = repository.LogInCheck(user, password);
            if (result != "")
            {
                writer.Write((int)Requests.LogIn);
                using (SqlConnection Dbconn = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(
                           "update CHATUSERS set login =1 Where IP=@user", Dbconn);
                    command.Parameters.AddWithValue("@user", user);
                    Dbconn.Open();
                    command.ExecuteNonQuery();
                }
            }
            else
                writer.Write((int)Requests.NotLogIn);
            writer.Write((int)Requests.GetUsers);
            writer.Flush();
        }

        private void GetUsers(BinaryWriter writer)
        {
            writer.Write((int)Requests.GetUsers);
            var users = repository.GetUsers();
            writer.Write(users.Count);
            foreach (var usr in users)
            {
                writer.Write(usr);
            }
        }

        private void GetMessages(BinaryWriter writer)
        {
            writer.Write((int)Requests.GetMessages);
            var messages = repository.GetMessages();
            writer.Write(messages.Count);
            foreach (var msg in messages)
            {
                writer.Write(msg.Message);
            }
        }

        public static void UserLeave(string userLeave, BinaryWriter writer, BinaryReader reader, string connectionString, IPEndPoint ep)
        {
            using (SqlConnection Dbconn = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(
                       "update CHATUSERS set login =0 Where IP=@user", Dbconn);
                command.Parameters.AddWithValue("@user", userLeave);
                Dbconn.Open();
                command.ExecuteNonQuery();
            }
            Console.WriteLine($"{ep.Address} left chat!");
            writer.Write((int)Requests.GetUsers);
            writer.Flush();
        }
    }
}
