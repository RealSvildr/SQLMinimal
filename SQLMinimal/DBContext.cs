/*! SQLMinimal v1.2.5 | (c) Svildr 2017 ~ 2019 | MIT License */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Reflection;
using System.Web;

namespace SqlMinimal {
    public sealed class DBContext {
        private readonly string stringConnection = "Data Source={0}; Initial Catalog={1};{2}";
        private SqlConnection db;

        public DBContext(string stringConnection) {
            this.stringConnection = stringConnection;
            db = new SqlConnection(stringConnection);
        }
        public DBContext(string serverAddress, string dataBase) : this(serverAddress, dataBase, null, null) { }
        public DBContext(string serverAddress, string dataBase, string user, string password) {
            if (string.IsNullOrEmpty(user) && string.IsNullOrEmpty(password))
                stringConnection = string.Format(stringConnection, serverAddress, dataBase, "Trusted_Connection=True");
            else
                stringConnection = string.Format(stringConnection, serverAddress, dataBase, string.Format("User ID={0}; Password={1}", user, password));

            db = new SqlConnection(stringConnection);
        }

        /// <summary>
        /// Return the number of lines affected
        /// </summary>
        /// <param name="sqlQuery">SQL Command</param>
        /// <param name="sqlParams">SQL Parameters</param>
        /// <returns></returns>
        public int ExecuteSqlCommand(string sqlQuery, params object[] sqlParams) {
            int numeroRows = 0;

            SqlCommand cmd = new SqlCommand(sqlQuery, db) {
                CommandTimeout = _timeOut,
                CommandType = CommandType.Text
            };

            if (_objectTransaction != null)
                cmd.Transaction = _objectTransaction.Transaction;


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

            SqlCommand cmd = new SqlCommand(sqlQuery, db) {
                CommandTimeout = _timeOut,
                CommandType = CommandType.Text
            };

            if (_objectTransaction != null)
                cmd.Transaction = _objectTransaction.Transaction;


            if (sqlParams != null)
                for (i = 0; i < sqlParams.Length; i++)
                    cmd.Parameters.AddWithValue("p" + i.ToString(), sqlParams[i] == null ? DBNull.Value : sqlParams[i]);

            if(db.State != ConnectionState.Open)
                db.Open();
                
            SqlDataReader reader = cmd.ExecuteReader();

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

                        prop = obj.GetType().GetProperty(propName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
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
        /// <param name="sqlQuery">SQL Command</param>
        /// <param name="offset">Start offset of the stream, used of big streams like videos.</param>
        /// <param name="sqlParams">SQL Parameters</param>
        public void FileStream(HttpContext context, string sqlQuery, long offset, params object[] sqlParams) {
            int i = 0;

            SqlCommand cmd = new SqlCommand(sqlQuery, db) {
                CommandTimeout = _timeOut,
                CommandType = CommandType.Text
            };

            if (sqlParams != null)
                for (i = 0; i < sqlParams.Length; i++)
                    cmd.Parameters.AddWithValue("p" + i.ToString(), sqlParams[i] == null ? DBNull.Value : sqlParams[i]);

            db.Open();
            SqlDataReader reader = cmd.ExecuteReader();

            //Get FilePath
            string filePath = string.Empty;
            long size = 0;
            if (reader.Read()) {
                filePath = reader["PathName"].ToString();
                size = Convert.ToInt64(reader["Size"]);
            }

            reader.Close();

            //Obtain Transaction for Blob
            SqlTransaction sqlTransaction = db.BeginTransaction("fileStreamTransaction");
            cmd.Transaction = sqlTransaction;
            cmd.CommandText = "SELECT GET_FILESTREAM_TRANSACTION_CONTEXT()";
            byte[] txtContent = (byte[])cmd.ExecuteScalar();

            //Obtain Handle
            SqlFileStream sqlFileStream = new SqlFileStream(filePath, txtContent, System.IO.FileAccess.Read);

            //Read data
            sqlFileStream.Seek(offset, System.IO.SeekOrigin.Begin);

            try {
                int bytesRead;
                byte[] buffer = new byte[_bufferSize];
                do {
                    bytesRead = sqlFileStream.Read(buffer, 0, buffer.Length);
                    context.Response.OutputStream.Write(buffer, 0, bytesRead);
                } while (bytesRead == buffer.Length);
            } catch (Exception) { }

            sqlFileStream.Close();
            cmd.Transaction.Commit();
            db.Close();
        }
        
        #region Transaction
        public void StartTransaction(string transactionName = "main") {
            if (_transactionList.Exists(o => o.Name == transactionName)) {
                throw new SqlMinimalException("Transaction already exists.");
            }

            _objectTransaction = new TransactionObject() {
                Name = transactionName,
                Transaction = db.BeginTransaction(transactionName)
            };
            
            _transactionList.Add(_objectTransaction);
        }

        public void Commit(bool dispose = true) {
            if (_objectTransaction == null) {
                throw new SqlMinimalException("Transaction doesn't exists or is not selected.");
            }

            _objectTransaction.Transaction.Commit();

            if (dispose)
                DisposeTransaction();
        }
        public void Rollback(bool dispose = true) {
            if (_objectTransaction == null) {
                throw new SqlMinimalException("Transaction doesn't exists or is not selected.");
            }

            _objectTransaction.Transaction.Rollback();

            if (dispose)
                DisposeTransaction();
        }

        public void ChangeTransaction(string transactionName) {
            if (string.IsNullOrEmpty(transactionName)) {
                _objectTransaction = null;
            }

            TransactionObject tObject = null;
            foreach (TransactionObject _obj in _transactionList) {
                if (_obj.Name == transactionName) {
                    tObject = _obj;
                    break;
                }
            }

            if (tObject == null) {
                throw new SqlMinimalException("Transaction not found.");
            }

            _objectTransaction = tObject;
        }

        private void DisposeTransaction() {
            _objectTransaction.Transaction.Dispose();

            int i = 0;
            for (i = 0; i < _transactionList.Count; i++) {
                if (_transactionList[i].Name == _objectTransaction.Name) {
                    _transactionList.RemoveAt(i);
                    break;
                }
            }

            if (_transactionList.Count == 0)
                _objectTransaction = null;
            else if (i < _transactionList.Count)
                _objectTransaction = _transactionList[i];
            else
                _objectTransaction = _transactionList[i - 1];
        }
        #endregion

        #region Parameters
        private int _timeOut = 120;
        public int TimeOut { get { return _timeOut; } set { _timeOut = value > 5 ? value : 5; } }

        private int _bufferSize = 65536;
        public int BufferSize { get { return _bufferSize; } set { _bufferSize = value > 255 ? value : 255; } }
        
        private List<TransactionObject> _transactionList = null;
        private TransactionObject _objectTransaction = null;
        private class TransactionObject {
            public string Name { get; set; }
            public SqlTransaction Transaction { get; set; }
        }
        #endregion
    }
}
