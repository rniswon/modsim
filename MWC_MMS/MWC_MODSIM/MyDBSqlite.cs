/*
 * --------------------------------------------------------------------------------
 * Project:         WaterALLOC
 * Namespace:       RTI.WRMD.WaterALLOC.Class
 * Class:           SqliteHelper1
 * Description:     <DESCRIPTION>
 * Author:          anuragsrivastav@rti.org
 * Date:            January 01, 2018 - March 31, 2018
 * Note:            <NOTES>
 * Revision History:
 * Name:            Date:           Description:
 * 
 * 
 * --------------------------------------------------------------------------------
 */

using System;
using System.IO;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Data.SQLite;

namespace RTI.CWR.MWC_MODSIMUtils
{
    //public delegate void ProcessMessage(string msg);  // delegate

    public class MyDBSqlite : IDisposable
    {
        public string dbFile { get; private set; }
        private string ConnectionString { get; set; }
        private bool _version4Plus { get; set; }
        private SQLiteTransaction _sqltransaction { get; set; }
        private SQLiteConnection _sqlconnection { get; set; }

        public event ProcessMessage messageOut; // event


        public MyDBSqlite()
        {  
            
        }

        public MyDBSqlite(string dbfile)
        {
            //dbFile = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), dbfile);
            ConnectionString = GetSqLiteConnectionString(dbfile);
            this.dbFile = dbfile;
        }

        public bool IsColumnsExist(string tableName, string columnName)
        {
            string sql = $"SELECT * FROM [{tableName}] LIMIT 1";
            DataTable dbTbl = GetTableFromDB(sql, tableName);
            if (dbTbl.Columns.Contains(columnName))
            {
                return true;
            }
            return false;
        }

        private string GetSqLiteConnectionString(string dbFileName)
        {
            SQLiteConnectionStringBuilder conn = new SQLiteConnectionStringBuilder
            {
                DataSource = dbFileName,
                Version = 3,
                FailIfMissing = true,
            };
            conn.Add("Compress", true);

            return conn.ConnectionString;
        }

       public bool IsTableExist(string tablename)
        {
            bool isexist = false;
            using (SQLiteConnection c = new SQLiteConnection(ConnectionString))
            {
                try
                {
                    c.Open();
                    string sql = "SELECT name FROM sqlite_master WHERE type = 'table'";
                    using (SQLiteCommand cmd = new SQLiteCommand(sql, c))
                    {
                        SQLiteDataReader r = cmd.ExecuteReader();
                        while (r.Read())
                        {
                            if (r[0].ToString() == tablename)
                            {
                                isexist = true;
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                   messageOut("ERROR [DATABASE]: " + ex.Message);
                }
                finally
                {
                    c.Close();
                }
            }
            return isexist;
        }
        
        /// <summary>
        /// Execute an SQL query and return the new row PK_ID
        /// </summary>
        /// <param name="sql">SQL statement</param>
        /// <returns></returns>
        public int ExecuteQuery(string sql)
        {
            ////using (SQLiteConnection c = new SQLiteConnection(ConnectionString))
            ////{
                int rowid=0;
                try
                {
                    CheckDatabaseConnection();
                    using (SQLiteCommand cmd = new SQLiteCommand(sql, _sqlconnection))
                    {
                        cmd.Transaction = _sqltransaction;
                        rowid = cmd.ExecuteNonQuery();
                        rowid = (int)_sqlconnection.LastInsertRowId;
                    }
                }
                catch (Exception ex)
                {
                    messageOut(ex.Message);
                }
                finally
                {
                    CommitTransaction();
                }
                return rowid;
            //}
        }

        public void ExecuteNonQuery(string sql, int retryLocked = 0)
        {
            //using (SQLiteConnection c = new SQLiteConnection(ConnectionString))
            //{
            int tries = 0;
        retryQuery:
            try
            {
                //c.Open();                    
                CheckDatabaseConnection();
                using (SQLiteCommand cmd = new SQLiteCommand(sql, _sqlconnection, _sqltransaction))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                if (tries < retryLocked)
                {
                    if (ex.Message.Contains("database table is locked"))
                    {
                        System.Threading.Thread.Sleep((tries + 1) * 2000);
                        tries += 1;
                        goto retryQuery;
                    }
                }
               messageOut("[ERROR]" + ex.Message);
            }
            finally
            {
                CommitTransaction();
            }
        }

        public object ExecuteScalar(string sql)
        {
            using (SQLiteConnection c = new SQLiteConnection(ConnectionString))
            {
                object m_value = null;
                try
                {
                    c.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(sql, c))
                    {
                        SQLiteDataReader r = cmd.ExecuteReader();
                        while (r.Read())
                        {
                            m_value = r[0];
                        }
                    }
                }
                catch (Exception ex)
                {
                    messageOut(ex.Message + "\n" + ex.StackTrace.ToString());
                }
                finally
                {
                    c.Close();
                }
                return m_value;
            }
        }
      
        private void CheckDatabaseConnection()
        {
            try
            {
                if (_sqlconnection == null || _sqlconnection.State != ConnectionState.Open)
                {
                    _sqlconnection = new SQLiteConnection(ConnectionString);
                    _sqlconnection.Open();
                    //custom functions definition
                    _sqlconnection.BindFunction(new SQLiteFunctionAttribute("Power", 1, FunctionType.Scalar),
                        (Func<object[], object>)((object[] args) => PowerFunction((object[])args[1])),
                        null);
                }

                if (_sqltransaction == null)
                {
                    _sqltransaction = _sqlconnection.BeginTransaction();
                }
            }
            catch (Exception ex)
            {
                if(messageOut!=null)
                    messageOut(ex.Message);
            }
      
            return;
        }

        object PowerFunction(object[] args)
        {
            var arg1 = (double)args[0];
            var arg2 = (double)args[1];
            return Math.Pow(arg1, arg2);
        }


        public DataTable GetTableFromDB(string sql, string tableName)
        {
            CheckDatabaseConnection();
            DataTable rval = new DataTable();
            try
            {
                using (SQLiteCommand cmd = new SQLiteCommand(sql, _sqlconnection, _sqltransaction))
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
                messageOut("ERROR [DATABASE]" + ex.Message);
            }
            finally
            {
                CommitTransaction();
            }
            return rval;


            //DataTable settingstable = new DataTable(tableName);

            //try
            //{
            //    CheckDatabaseConnection();
            //    using (SQLiteCommand cmd = new SQLiteCommand(sql, _sqlconnection))
            //    {
            //        SQLiteDataReader r = cmd.ExecuteReader();
            //        if (settingstable.Columns.Count == 0)
            //        {
            //            for (int i = 0; i < r.FieldCount; i++)
            //            {
            //                settingstable.Columns.Add(r.GetName(i), r.GetFieldType(i));
            //            }
            //        }
            //        while (r.Read())
            //        {
                        
            //            object[] rowValues = new object[r.FieldCount];
            //            r.GetValues(rowValues);
            //            settingstable.Rows.Add(rowValues);
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    messageOut("ERROR [DATABASE]: " + ex.Message);
            //    settingstable = null;
            //}
            //finally
            //{
            //    CommitTransaction();
            //}

            //return settingstable;
        }

        public bool UpdateTableFromDB(DataTable m_Table)
        {
            try
            {
                CheckDatabaseConnection();
                // store ts pattern values
                foreach (DataRow m_row in m_Table.Rows)
                {
                    string sql = "INSERT OR REPLACE INTO " + m_Table.TableName + " (";
                    for (int i = 0; i < m_Table.Columns.Count; i++)
                    {
                        if (i > 0) sql += " ,";
                        sql += m_Table.Columns[i].ColumnName;
                    }
                    sql += ") VALUES (";
                    for (int i = 0; i < m_Table.Columns.Count; i++)
                    {
                        if (i > 0) sql += " ,";
                        if (IsNumeric(m_Table.Columns[i]))
                        {
                            if (DBNull.Value.Equals(m_row[i]))
                            {
                                sql += "NULL";
                            }
                            else
                            {
                                sql += m_row[i].ToString();
                            }
                        }
                        else if (m_Table.Columns[i].DataType.ToString().Contains("Date"))
                        {
                            DateTime m_date = (DateTime)m_row[i];
                            sql += "'" + m_date.ToString("yyyy-MM-dd HH:MM:ss") + "'";
                        }
                        else
                        {
                            sql += "'" + m_row[i].ToString() + "'";
                        }
                    }
                    sql += ");";
                    using (SQLiteCommand cmd = new SQLiteCommand(sql, _sqlconnection))
                    {
                        cmd.Transaction = _sqltransaction;
                        cmd.ExecuteNonQuery();                        
                    }
                }
            }
            catch (Exception ex)
            {
                messageOut("ERROR [DATABASE]: " + ex.Message);
                return false;
            }
            finally
            {
                CommitTransaction();
            }
            return true;
        }

        public bool IsNumeric(DataColumn col)
        {
            if (col == null)
                return false;
            // Make this const
            var numericTypes = new[] { typeof(Byte), typeof(Decimal), typeof(Double),
        typeof(Int16), typeof(Int32), typeof(Int64), typeof(SByte),
        typeof(Single), typeof(UInt16), typeof(UInt32), typeof(UInt64)};
            return numericTypes.Contains(col.DataType);
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

        public bool SetupDatabase(string dbfile)
        {
            try
            {
                if (!File.Exists(dbfile))
                {
                    SQLiteConnection.CreateFile(dbfile);
                    ConnectionString = GetSqLiteConnectionString(dbfile);
                }
                //else
                //{
                //    if (!IsTableExist("MMS_Preferences"))
                //    {
                //        messageOut("WARNING [DATABASE]: database is missing MMS tables. ");
                //        messageOut("WARNING [DATABASE]: start preparing the database... ");
                //    }
                //}
                ConnectionString = GetSqLiteConnectionString(dbfile);
                

                this.dbFile = dbfile;

                //create tables
                if (!IsTableExist("MMS_Preferences"))
                {
                    string sql = @"CREATE TABLE MMS_Preferences (
                                    Parameter TEXT,
	                                Value TEXT,
	                                PRIMARY KEY(Parameter)
                                    ); ";
                    ExecuteNonQuery(sql);
                }
                if (!IsTableExist("Features"))
                {
                    string sql = @"CREATE TABLE Features (
	                                    FeatureID	INTEGER NOT NULL,
	                                    MOD_Name	TEXT,
	                                    MOD_Type	TEXT,
	                                    MOD_Number	TEXT,
	                                    Description	TEXT,
	                                    GroupingKey	TEXT,
	                                    uid	TEXT,
	                                    X_Coord	TEXT,
	                                    Y_Coord	TEXT,
	                                    From_ID	TEXT,
	                                    To_ID	TEXT,
	                                    Cost	TEXT,
	                                    PRIMARY KEY(FeatureID AUTOINCREMENT)
                                    )";
                    ExecuteNonQuery(sql);
                }

                if (!IsTableExist("DatasetsInfo"))
                {
                    string sql = @"CREATE TABLE DatasetsInfo (
	                                ID	INTEGER NOT NULL,
	                                DSName	TEXT,
	                                Description	TEXT,
	                                PRIMARY KEY(ID AUTOINCREMENT)
                                )";
                    ExecuteNonQuery(sql);
                }

                if (!IsTableExist("DatasetsTSSet"))
                {
                    string sql = @"CREATE TABLE DatasetsTSSet (
                                        Dataset   INTEGER,
	                                    [Order] INTEGER,
	                                    TSType    INTEGER,
	                                    Notes TEXT,
	                                    PRIMARY KEY(Dataset,TSType,[Order])
                                    );";
                    ExecuteNonQuery(sql);
                }

                if (!IsTableExist("MMS_RunsInfo"))
                {
                    string sql = @"CREATE TABLE MMS_RunsInfo (
	                                runID	INTEGER NOT NULL,
	                                ScnName	TEXT,
	                                SimulationStatus	INTEGER,
	                                Keyword	TEXT,
	                                LastAccess	TEXT,
	                                Notes	TEXT,
	                                Options	TEXT,
	                                BasePath	TEXT,
                                    OutputDBScenario	INTEGER, 
                                    RunType	TEXT,
                                    RiparianON INTEGER,
                                    ModsimFile	TEXT,
                                    ProcessID INTEGER,
	                                PRIMARY KEY(runID AUTOINCREMENT)
                                )";
                    ExecuteNonQuery(sql);
                }

                if (!IsTableExist("MMS_RunsParameters"))
                {
                    string sql = @"CREATE TABLE MMS_RunsParameters (
                                        runID INTEGER NOT NULL,
	                                    KeyParameter  TEXT,
	                                    [Value] TEXT,
	                                    PRIMARY KEY(runID,KeyParameter)
                                    )";
                    ExecuteNonQuery(sql);
                }

                if (!IsTableExist("UnitsInfo"))
                {
                    string sql = @"CREATE TABLE UnitsInfo (
	                                UnitsID	INTEGER NOT NULL,
	                                Units	TEXT,
	                                System	TEXT,
	                                PRIMARY KEY(UnitsID AUTOINCREMENT)
                                )";
                    ExecuteNonQuery(sql);

                    sql = @"INSERT INTO UnitsInfo ([UnitsID],[Units],[System]) 
                                VALUES (1,'acre-ft','English'),
                                 (2,'acre-ft/day','English'),
                                 (3,'cfs','English'),
                                 (4,'Dimensionless','N/A'),
                                 (5,'Default','N/A'),
                                 (6,'m³/day','Metric'),
                                 (7,'ft³/day','English'),
                                 (8,'acre-ft/month','English');";
                    ExecuteNonQuery(sql);
                }

                if (!IsTableExist("TSTypes"))
                {
                    string sql = @"CREATE TABLE TSTypes (
	                                TSTypeID	INTEGER NOT NULL,
	                                TSName	TEXT,
	                                UnitsID	INTEGER,
	                                Source	TEXT,
	                                SourceFile	TEXT,
	                                IsRegular	INTEGER,
	                                TSInterval	TEXT,
	                                Notes	TEXT,
	                                MODSIMTSType	TEXT,
	                                IsPattern	INTEGER DEFAULT 0,
	                                Created	TEXT,
	                                FOREIGN KEY(UnitsID) REFERENCES UnitsInfo(UnitsID),
	                                PRIMARY KEY(TSTypeID AUTOINCREMENT)
                                )";
                    ExecuteNonQuery(sql);
                }
          
                if (!IsTableExist("TSPatterns"))
                {
                    string sql = @"CREATE TABLE TSPatterns (
	                                TSTypeID	INTEGER NOT NULL,
	                                FeatureID	INTEGER NOT NULL,
	                                [Index]	INTEGER NOT NULL,
	                                TSValue	NUMERIC,
	                                FOREIGN KEY(TSTypeID) REFERENCES TSTypes(TSTypeID),
	                                FOREIGN KEY(FeatureID) REFERENCES Features(FeatureID),
	                                PRIMARY KEY(TSTypeID,[Index],FeatureID)
                                )";
                    ExecuteNonQuery(sql);
                }

                if (!IsTableExist("Timeseries"))
                {
                    string sql = @"CREATE TABLE Timeseries (
	                                TSDate	TEXT NOT NULL,
	                                FeatureID	INTEGER NOT NULL,
	                                TSTypeID	INTEGER NOT NULL,
	                                TSValue	REAL,
	                                FOREIGN KEY(FeatureID) REFERENCES Features(FeatureID),
	                                FOREIGN KEY(TSTypeID) REFERENCES TSTypes(TSTypeID),
	                                PRIMARY KEY(TSDate,FeatureID,TSTypeID)
                                )";
                    ExecuteNonQuery(sql);
                }

                if (!IsTableExist("WaterRights"))
                {
                    string sql = @"CREATE TABLE WaterRights (
                                    Application_ID STRING, 
                                    PriorityDate STRING,
                                    WR_Type STRING, 
                                    FaceValue DOUBLE, 
                                    POU_ID STRING,
                                    StorageAmount_AF DOUBLE)";
                    ExecuteNonQuery(sql);
                }
            }
            catch (Exception ex)
            {
                messageOut("ERROR [database_setup]: error in creating database. " + ex.Message + ex.StackTrace);
            }

            return true;
        }

        public void UpdateRunsInfoTable(int runid, Dictionary<string, object> valuePairs)
        {
            try
            {
                string sql = "SELECT * FROM MMS_RunsInfo WHERE (RunID = " + runid + ")";

                DataTable runInfoDT = GetTableFromDB(sql, "MMS_RunsInfo");

                if (runInfoDT.Rows.Count > 0)
                {
                    foreach (string key in valuePairs.Keys)
                    {
                        if(key!= "runID")
                            runInfoDT.Rows[0][key] = valuePairs[key];
                    }
                    UpdateTableFromDB(runInfoDT);
                }
            }
            catch (Exception ex)
            {
                messageOut(String.Concat("ERROR: ", ex.Message));
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

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

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~SqliteHelper1() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
}
