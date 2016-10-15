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
        private MessageRepository repository;
        string connectionString = @"Data Source=INGVAR\SQLEXPRESS;Integrated security = True;Initial Catalog=chatDB";
        public ChatServer()
        {
            listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 8009);
            repository = new MessageRepository(connectionString);
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
                                return;
                            case Requests.GetMessages:
                                writer.Write((int)Requests.GetMessages);
                                var messages = repository.GetMessages();
                                writer.Write(messages.Count);
                                foreach (var msg in messages)
                                {
                                    writer.Write(msg.Message);
                                }
                                break;
                            case Requests.SendMessage:
                                repository.SendMessages(reader.ReadString());
                                break;
                            case Requests.GetUsers:
                                writer.Write((int)Requests.GetUsers);
                                var users = repository.GetUsers();
                                writer.Write(users.Count);
                                foreach (var usr in users)
                                {
                                    writer.Write(usr);
                                }
                                break;
                            case Requests.LogIn:
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
                                break;
                            case Requests.Reg:
                                string check = repository.Reg(reader.ReadString(), reader.ReadString());
                                if (check == "1")
                                    writer.Write((int)Requests.Reg);
                                else
                                    writer.Write((int)Requests.NotReg);
                                writer.Write((int)Requests.GetUsers);
                                writer.Flush();
                                break;
                            case Requests.PrivateMessage:
                                writer.Write((int)Requests.PrivateMessage);
                                writer.Write(reader.ReadString());
                                writer.Write(reader.ReadString());
                                writer.Write(reader.ReadString());
                                writer.Flush();
                                break;
                        }
                    }
                }
            }
        }
    }
}
