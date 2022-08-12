using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TableDependency.SqlClient;
using TableDependency.SqlClient.Base.Enums;
using TableDependency.SqlClient.Base.EventArgs;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        SqlDataAdapter dataadapter;
        DataBase database = new DataBase();
        public SqlTableDependency<ExampleTable> example_table_dependency;

        public Form1()
        {
            InitializeComponent();        
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                stop_example_table_dependency();
            }
            catch (Exception ex) { log_file(ex.ToString()); }
        }


        private async void btnStart_Click(object sender, EventArgs e)
        {          

            database.setConnection(connStringTextBox.Text);

            SqlDependency.Start(database.getConnection().ConnectionString);

            await database.openConnection();

            refresh_table();
   
            start_example_table_dependency();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            try
            {              
                stop_example_table_dependency();
                database.closeConnection();
            }
            catch (Exception ex) { log_file(ex.ToString()); }
        }


        private void refresh_table()
        {
            string sql = "SELECT * FROM ExampleTable";

            dataadapter = new SqlDataAdapter(sql, database.getConnection());
            DataSet ds = new DataSet();
            dataadapter.Fill(ds, "ExampleTable");
            ThreadSafe(() => dataGridView1.DataSource = ds);
            ThreadSafe(() => dataGridView1.DataMember = "ExampleTable");
        }

        private bool start_example_table_dependency()
        {
            try
            {
                example_table_dependency = new SqlTableDependency<ExampleTable>(database.getConnection().ConnectionString);
                example_table_dependency.OnChanged += example_table_dependency_Changed;
                example_table_dependency.OnError += example_table_dependency_OnError;
                example_table_dependency.Start();
                return true;
            }
            catch (Exception ex)
            {
                log_file(ex.ToString());
            }
            return false;

        }

        private bool stop_example_table_dependency()
        {
            try
            {
                if (example_table_dependency != null)
                {
                    example_table_dependency.Stop();

                    return true;
                }
            }
            catch (Exception ex) { log_file(ex.ToString()); }

            return false;

        }

        private void example_table_dependency_OnError(object sender, ErrorEventArgs e)
        {
            log_file(e.Error.Message);
        }

        private async void example_table_dependency_Changed(object sender, RecordChangedEventArgs<ExampleTable> e)
        {
            try
            {
                var changedEntity = e.Entity;

                switch (e.ChangeType)
                {
                    case ChangeType.Insert:
                        {
                            log_file("Insert values:\tFlag: " + changedEntity.Flag.ToString() + " Data: " + changedEntity.Data.ToString());
                             await Task.Run(() => refresh_table()); 
                        }
                        break;

                    case ChangeType.Update:
                        {
                            log_file("Update values:\tFlag: " + changedEntity.Flag.ToString() + " Data: " + changedEntity.Data.ToString());
                            await Task.Run(() => refresh_table());
                        }
                        break;           
                };

            }
            catch (Exception ex) { log_file(ex.ToString()); }

        }

        public void log_file(string logText)
        {                 
            System.IO.File.AppendAllText(Application.StartupPath + "\\log.txt", logText);
        }

        private void ThreadSafe(MethodInvoker method)
        {
            try
            {
                if (InvokeRequired)
                    Invoke(method);
                else
                    method();
            }
            catch (ObjectDisposedException) { }
        }

    }
}
