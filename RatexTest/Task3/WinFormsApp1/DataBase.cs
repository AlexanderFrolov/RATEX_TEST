﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;

namespace WinFormsApp1
{
    internal class DataBase
    {
        //SqlConnection conn = new SqlConnection(@"Data Source=DESKTOP-MELE315\SQLEXPRESS01;Initial Catalog=db;Integrated Security=True;MultipleActiveResultSets=True");
        SqlConnection conn;

        public void setConnection(string connectionString)
        {
            conn = new SqlConnection(connectionString);
        }


        public void openConnection()
        {
            if(conn.State == ConnectionState.Closed)
            {
                conn.Open();               
            }
        }

        public void closeConnection()
        {
            if (conn.State == ConnectionState.Open)
            {
                conn.Close();
            }
        }

        public SqlConnection getConnection()
        {
            return conn;
        }


        public SqlTransaction BeginTransaction()
        {
            return conn.BeginTransaction();
        }

        public SqlCommand CreateCommand()
        {
            return conn.CreateCommand();
        }

    }
}
