using System;
using System.Collections.Generic;
using System.Data;
//using System.Data.SqlClient;
using System.Reflection;
using Npgsql;

namespace SqlMinimal {
    //TODO: Set SqlTransaction as Disposable to be able to use inside an "using"
    public sealed class DBContextPostgre {
        private readonly string stringConnection;
        private readonly NpgsqlConnection db;

        public DBContextPostgre(string stringConnection) {
            this.stringConnection = stringConnection;
            db = new NpgsqlConnection(stringConnection);
        }


        public DBContextPostgre(string serverAddress, string dataBase) : this(serverAddress, dataBase, null, null, null) { }
        public DBContextPostgre(string serverAddress, string dataBase, string user, string password) : this(serverAddress, dataBase, user, password, null) { }

        public DBContextPostgre(string serverAddress, string dataBase, string user, string password, string port) {
            stringConnection = $"Server={serverAddress};";

            if (!string.IsNullOrEmpty(port))
                stringConnection += $"Port={port};";

            if (!string.IsNullOrEmpty(dataBase))
                stringConnection += $"Database={dataBase};";

            if (string.IsNullOrEmpty(user) && string.IsNullOrEmpty(password))
                stringConnection += $"Trusted_Connection=True;";
            else
                stringConnection += $"User ID={user}; Password={password};";

            stringConnection += "SSLMode=Prefer";


            db = new NpgsqlConnection(stringConnection);
        }

        /// <summary>
        /// Return the number of lines affected
        /// </summary>
        /// <param name="sqlQuery">SQL Command</param>
        /// <param name="sqlParams">SQL Parameters</param>
        /// <returns></returns>
        public int ExecuteSqlCommand(string sqlQuery, params object[] sqlParams) {
            int numeroRows = 0;

            NpgsqlCommand cmd = new NpgsqlCommand(sqlQuery, db) {
                CommandTimeout = _timeOut,
                CommandType = CommandType.Text
            };

            if (sqlTransaction != null)
                cmd.Transaction = sqlTransaction;


            if (sqlParams != null)
                for (int i = 0; i < sqlParams.Length; i++)
                    cmd.Parameters.AddWithValue("p" + i.ToString(), sqlParams[i] == null ? DBNull.Value : sqlParams[i]);

            db.Open();
            numeroRows = cmd.ExecuteNonQuery();
            db.Close();

            return numeroRows;
        }

        /// <summary>
        /// Return a list with data of type T
        /// </summary>
        /// <typeparam name="T">Type of data to be returned</typeparam>
        /// <param name="sqlQuery">SQL Command</param>
        /// <param name="sqlParams">SQL Parameters</param>
        /// <returns></returns>
        public List<T> SqlQuery<T>(string sqlQuery, params object[] sqlParams) {
            List<T> lista = new List<T>();
            T obj = Activator.CreateInstance<T>();
            int i = 0;

            NpgsqlCommand cmd = new NpgsqlCommand(sqlQuery, db) {
                CommandTimeout = _timeOut,
                CommandType = CommandType.Text
            };

            if (sqlTransaction != null)
                cmd.Transaction = sqlTransaction;


            if (sqlParams != null)
                for (i = 0; i < sqlParams.Length; i++)
                    cmd.Parameters.AddWithValue("p" + i.ToString(), sqlParams[i] == null ? DBNull.Value : sqlParams[i]);

            db.Open();
            NpgsqlDataReader reader = cmd.ExecuteReader();

            if (obj.GetType().GetProperties().Length == 0) {
                while (reader.Read()) {
                    lista.Add((T)reader[0]);
                }
            } else {
                string propName = string.Empty;
                PropertyInfo prop;

                while (reader.Read()) {
                    obj = Activator.CreateInstance<T>();

                    for (i = 0; i < reader.FieldCount; i++) {
                        propName = reader.GetName(i);

                        prop = obj.GetType().GetProperty(propName);
                        if (prop != null && prop.CanWrite && !object.Equals(reader[prop.Name], DBNull.Value)) {
                            prop.SetValue(obj, reader[prop.Name].Convert(prop.PropertyType), null);
                        }
                    }
                    lista.Add(obj);
                }
            }

            db.Close();

            return lista;
        }

        /// <summary>
        /// Streams data to the Context Result, 
        /// The size of the buffer can be defined on DBContext.bufferSize
        /// </summary>
        /// <param name="context">Page context that will receive the data.</param>
        /// <param name="offset">Start offset of the stream, used of big streams like videos.</param>
        /// <param name="sqlQuery">SQL Command (Must have a PathName and Size)</param>
        /// <param name="sqlParams">SQL Parameters</param>
        //public void FileStream(HttpContext context, long offset, string sqlQuery, params object[] sqlParams) {
        //    int i = 0;

        //    SqlCommand cmd = new SqlCommand(sqlQuery, db) {
        //        CommandTimeout = _timeOut,
        //        CommandType = CommandType.Text
        //    };

        //    if (sqlParams != null)
        //        for (i = 0; i < sqlParams.Length; i++)
        //            cmd.Parameters.AddWithValue("p" + i.ToString(), sqlParams[i] == null ? DBNull.Value : sqlParams[i]);

        //    db.Open();
        //    SqlDataReader reader = cmd.ExecuteReader();

        //    //Get FilePath
        //    string filePath = string.Empty;
        //    long size = 0;
        //    if (reader.Read()) {
        //        filePath = reader["PathName"].ToString();
        //        size = Convert.ToInt64(reader["Size"]);
        //    }

        //    reader.Close();

        //    //Obtain Transaction for Blob
        //    SqlTransaction trans = db.BeginTransaction("mainTransaction");
        //    cmd.Transaction = trans;
        //    cmd.CommandText = "SELECT GET_FILESTREAM_TRANSACTION_CONTEXT()";
        //    byte[] txtContent = (byte[])cmd.ExecuteScalar();

        //    //Obtain Handle
        //    SqlFileStream sqlFileStream = new SqlFileStream(filePath, txtContent, System.IO.FileAccess.Read);

        //    //Read data
        //    sqlFileStream.Seek(offset, System.IO.SeekOrigin.Begin);

        //    try {
        //        int bytesRead;
        //        byte[] buffer = new byte[_bufferSize];
        //        do {
        //            bytesRead = sqlFileStream.Read(buffer, 0, buffer.Length);
        //            context.Response.OutputStream.Write(buffer, 0, bytesRead);
        //            context.Response.Flush();
        //        } while (bytesRead == buffer.Length);
        //    } catch (Exception) { }

        //    sqlFileStream.Close();
        //    cmd.Transaction.Commit();
        //    db.Close();
        //}

        //  • Not Working yet
        //#region Transaction
        //public void StartTransaction() {
        //    if (sqlTransaction != null) {
        //        throw new SqlMinimalException("Transaction already exists.");
        //    }

        //    sqlTransaction = db.BeginTransaction("mainTransaction");
        //}

        //public void Commit(bool dispose = true) {
        //    if (sqlTransaction == null) {
        //        throw new SqlMinimalException("Transaction doesn't exists.");
        //    }

        //    sqlTransaction.Commit();

        //    if (dispose) {
        //        sqlTransaction.Dispose();
        //        sqlTransaction = null;
        //    }
        //}

        //public void Rollback(bool dispose = true) {
        //    if (sqlTransaction == null) {
        //        throw new SqlMinimalException("Transaction doesn't exists.");
        //    }

        //    sqlTransaction.Rollback();

        //    if (dispose) {
        //        sqlTransaction.Dispose();
        //        sqlTransaction = null;
        //    }
        //}
        //#endregion

        #region Parameters
        private int _timeOut = 120;
        public int timeOut { get { return _timeOut; } set { timeOut = value > 5 ? value : 5; } }

        private int _bufferSize = 65536;
        public int bufferSize { get { return _bufferSize; } set { _bufferSize = value > 255 ? value : 255; } }

        private NpgsqlTransaction sqlTransaction = null;
        #endregion
    }
}
