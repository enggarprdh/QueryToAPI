using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;


namespace QueryToAPI
{
    class SqlManager : IDisposable
    {
        public SqlManager(Logger log)
        {
            Log = log;
        }
        public SqlManager(string ConnectionStr, Logger log)
        {
            ConnectionString = ConnectionStr;
            Log = log;
        }

        public void TruncateTable(string TableName)
        {
            using (SqlConnection sqlconn = new SqlConnection(ConnectionString))
            {
                sqlconn.Open();
                using (SqlCommand sqlcmd = new SqlCommand())
                {
                    sqlcmd.CommandTimeout = 3600;  //setting query timeout for 1 hour
                    sqlcmd.Connection = sqlconn;
                    sqlcmd.CommandText = $"truncate table {TableName}";

                    sqlcmd.ExecuteNonQuery();

                }

                sqlconn.Close();
            }
        }

        public int SqlToCsv(string QueryString, string CsvFileName, CsvConfiguration csvConfig)
        {
            int rowCount = 0;
            using (SqlConnection sqlconn = new SqlConnection(ConnectionString))
            {
                sqlconn.Open();
                using (SqlCommand sqlcmd = new SqlCommand())
                {
                    sqlcmd.CommandTimeout = 3600; //setting query timeout for 1 hour
                    sqlcmd.Connection = sqlconn;
                    sqlcmd.CommandText = QueryString;

                    using (SqlDataAdapter sqlda = new SqlDataAdapter())
                    {
                        using (DataSet ds = new DataSet())
                        {
                            sqlda.SelectCommand = sqlcmd;
                            sqlda.Fill(ds);

                            rowCount = ds.Tables[0].Rows.Count;

                            using (StreamWriter sw = new StreamWriter(CsvFileName, true))
                            {
                                using (var csv = new CsvHelper.CsvWriter(sw, csvConfig))
                                {
                                    // Write row values
                                    foreach (DataRow row in ds.Tables[0].Rows)
                                    {
                                        for (var i = 0; i < ds.Tables[0].Columns.Count; i++)
                                        {
                                            csv.WriteField(row[i]);
                                        }
                                        csv.NextRecord();
                                    }
                                }
                            }
                        }
                    }

                }

                sqlconn.Close();
            }

            return rowCount;
        }

        public int SqlToCsvHeaderOnly(string QueryString, string CsvFileName, CsvConfiguration csvConfig)
        {
            int rowCount = 0;

            bool FileIsExist;
            if (File.Exists(CsvFileName))
                FileIsExist = true;
            else FileIsExist = false;

            using (SqlConnection sqlconn = new SqlConnection(ConnectionString))
            {
                sqlconn.Open();
                using (SqlCommand sqlcmd = new SqlCommand())
                {
                    sqlcmd.CommandTimeout = 3600; //setting query timeout for 1 hour
                    sqlcmd.Connection = sqlconn;
                    sqlcmd.CommandText = QueryString;

                    using (SqlDataAdapter sqlda = new SqlDataAdapter())
                    {
                        using (DataSet ds = new DataSet())
                        {
                            sqlda.SelectCommand = sqlcmd;
                            sqlda.Fill(ds);
                            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                            {
                                rowCount = ds.Tables[0].Rows.Count;

                                using (StreamWriter sw = new StreamWriter(CsvFileName, true))
                                {
                                    using (var csv = new CsvWriter(sw, csvConfig))
                                    {
                                        if (!FileIsExist)
                                        {
                                            foreach (DataColumn column in ds.Tables[0].Columns)
                                            {
                                                csv.WriteField(column.ColumnName);
                                            }
                                            csv.NextRecord();
                                        }

                                    }
                                }
                            }

                        }
                    }

                }

                sqlconn.Close();
            }

            return rowCount;
        }

        public void SqlToTable(string QueryString, string DestinationConnectionString, string TableName, int BatchSize = 1000, int Timeout = 3600)
        {

            using (SqlConnection sqlconn = new SqlConnection(ConnectionString))
            {
                sqlconn.Open();
                using (SqlCommand sqlcmd = new SqlCommand())
                {
                    sqlcmd.CommandTimeout = 3600;  //setting query timeout for 1 hour
                    sqlcmd.Connection = sqlconn;
                    sqlcmd.CommandText = QueryString;

                    using (SqlDataReader sqldr = sqlcmd.ExecuteReader())
                    {

                        using (SqlBulkCopy bcp = new SqlBulkCopy(DestinationConnectionString))
                        {
                            bcp.BatchSize = BatchSize;
                            bcp.BulkCopyTimeout = Timeout;
                            bcp.DestinationTableName = $"{TableName}";
                            bcp.WriteToServer(sqldr);
                        }
                    }

                }

                sqlconn.Close();
            }
        }

        public DataTable SqlToDataTable(string query, string conns)
        {
            var result = new DataTable();
            using (SqlConnection sqlconn = new SqlConnection(conns))
            {
                sqlconn.Open();
                using (SqlCommand sqlcmd = new SqlCommand())
                {
                    sqlcmd.CommandTimeout = 3600; //setting query timeout for 1 hour
                    sqlcmd.Connection = sqlconn;
                    sqlcmd.CommandText = query;
                    using (SqlDataAdapter sqlda = new SqlDataAdapter())
                    {
                        using (DataSet ds = new DataSet())
                        {
                            sqlda.SelectCommand = sqlcmd;
                            sqlda.Fill(ds);
                            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                            {
                                var data = ds.Tables[0];
                                result = data;
                            }
                        }
                    }
                }

                sqlconn.Close();
            }

            return result;
        }

        public void CsvToTable(string ServerName, string DbName, string TableName)
        {

        }

        public void CreateTable(string connstr, string query)
        {
            SqlConnection conn = new SqlConnection(connstr);
            conn.Open();
            SqlCommand com = new SqlCommand(query, conn);
            com.ExecuteNonQuery();
            conn.Close();
        }

        public void CreateLog(string connStr, string responAPI, string URL, string tableName, string requestBody)
        {
            SqlConnection conn = new SqlConnection(connStr);
            conn.Open();
            var query = $"INSERT INTO {tableName} VALUES (@ID, @URL, @Request , @Response, GETDATE())";
            SqlCommand com = new SqlCommand(query, conn);
            com.Parameters.AddWithValue("@ID", Guid.NewGuid());
            com.Parameters.AddWithValue("@URL", URL);
            com.Parameters.AddWithValue("@Response", responAPI);
            com.Parameters.AddWithValue("@Request", requestBody);
            com.ExecuteNonQuery();
            conn.Close();
        }
        public string ConnectionString { get; set; }

        public Logger Log { get; }

        public void Dispose() { }

    }
}
