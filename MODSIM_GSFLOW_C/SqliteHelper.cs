using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;

namespace MODSIM_GSFLOW_C
{
    class SqliteHelper:IDisposable
    {

        private string dbFile { get; }
        private SQLiteConnection _sqlconnection { get; set; }
        private SQLiteTransaction _sqltransaction { get; set; }

        public SqliteHelper(string databasePath)
        {
            dbFile = databasePath;
        }

        public DataTable GetTableFromDB( string sql, string tableName)
        {
            CheckDatabaseConnection();
            DataTable rval = new DataTable();
            try
            {
                using (SQLiteCommand cmd = new SQLiteCommand(sql, _sqlconnection))
                {
                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                    {
                        adapter.Fill(rval);
                    }
                    rval.TableName = tableName;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR [DATABASE]" + ex.Message);
            }
            finally
            {
                CommitTransaction();
            }
            return rval;
        }

        private void CheckDatabaseConnection(bool beginTransaction = true)
        {
            if (_sqlconnection == null || _sqlconnection.State != ConnectionState.Open)
            {
                string ConnectionString = GetSqLiteConnectionString();
                _sqlconnection = new SQLiteConnection(ConnectionString);
                _sqlconnection.Open();
            }

            if (_sqltransaction == null && beginTransaction)
            {
                _sqltransaction = _sqlconnection.BeginTransaction();
            }
            return;
        }

        private string GetSqLiteConnectionString()
        {
            SQLiteConnectionStringBuilder conn = new SQLiteConnectionStringBuilder
            {
                DataSource = dbFile,
                Version = 3,
                FailIfMissing = true,
            };
            conn.Add("Compress", true);

            return conn.ConnectionString;
        }

        public void CommitTransaction()
        {
            if (_sqltransaction != null && _sqltransaction.Connection != null)
            {
                _sqltransaction.Commit();
                _sqltransaction = null;
            }

            if (_sqlconnection != null && _sqlconnection.State == ConnectionState.Open)
            {
                _sqlconnection.Close();
                _sqlconnection = null;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (_sqltransaction != null)
                    {
                        _sqlconnection.Close();
                        _sqltransaction.Dispose();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }
        #endregion
    }
}
