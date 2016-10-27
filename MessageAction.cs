using ChatClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ServerAutonomicLayer
{
    class MessageAction
    {
        private string connectionString;

        public MessageAction(string connectionString)
        {
            this.connectionString = connectionString;
        }


        public List<ChatMessage> GetMessages()
        {
            var adapter = new SqlDataAdapter("SELECT * FROM CHATMESSAGES", connectionString);
            var data = new DataSet();
            adapter.Fill(data);
            List<ChatMessage> result = new List<ChatMessage>();
            foreach (DataRow item in data.Tables[0].Rows)
            {
                result.Add(new ChatMessage()
                {
                    Message = (string)item["Message"]
                });
            }
            return result;
        }

        public List<string> GetUsers()
        {
            var adapter = new SqlDataAdapter("SELECT IP FROM CHATUsers where login=1", connectionString);
            var data = new DataSet();
            adapter.Fill(data);
            List<string> result = new List<string>();

            foreach (DataRow item in data.Tables[0].Rows)
            {
                result.Add((string)item["IP"]);
            }
            return result;
        }

        public void SendMessages(string messageUser)
        {

            DateTime time;
            time = DateTime.Now;

            using (SqlConnection Dbconn = new SqlConnection(connectionString))
            {

                SqlCommand command = new SqlCommand(
                        "INSERT INTO CHATMESSAGES (MESSAGE, TIME)" +
                        "VALUES (@messageUser, @time)", Dbconn);
                command.Parameters.AddWithValue("@messageUser", messageUser);
                command.Parameters.AddWithValue("@time", time);
                Dbconn.Open();
                command.ExecuteScalar();
            }
        }

        public string LogInCheck(string User, string password)
        {
            string result;
            using (SqlConnection Dbconn = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(
                       "SELECT IP FROM CHATUSERS WHERE IP = @user and PASSWORD =@password", Dbconn);
                command.Parameters.AddWithValue("@user", User);
                command.Parameters.AddWithValue("@password", password);
                Dbconn.Open();

                if (command.ExecuteScalar() != null)
                    result = command.ExecuteScalar().ToString();
                else
                    result = "";
            }
            return result;
        }

        public string Reg(string User, string password)
        {
            string check;
            using (SqlConnection Dbconn = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(
                       "SELECT IP FROM CHATUSERS WHERE IP = @user", Dbconn);
                command.Parameters.AddWithValue("@user", User);
                Dbconn.Open();
                if (command.ExecuteScalar() != null)
                {
                    check = "0";
                }
                else
                {
                    using (SqlConnection Dbconn2 = new SqlConnection(connectionString))
                    {
                        SqlCommand command2 = new SqlCommand(
                               "INSERT INTO CHATUSERS(IP, PASSWORD) VALUES (@user,@password)", Dbconn);
                        command2.Parameters.AddWithValue("@user", User);
                        command2.Parameters.AddWithValue("@password", password);
                        Dbconn2.Open();
                        command2.ExecuteNonQuery();
                    }
                    check = "1";
                }
            }

            return check;
        }
    }
}
