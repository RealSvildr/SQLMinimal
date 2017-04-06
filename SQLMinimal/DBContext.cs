using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;

namespace SqlMinimal {
    public sealed class DBContext {
        private string stringConnection = "Data Source={0}; Initial Catalog={1};{2}";
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
        /// Retorna o número de linhas afetadas pelo comando
        /// </summary>
        /// <param name="sqlQuery">Comando SQL</param>
        /// <param name="sqlParams">Parametros do SQL</param>
        /// <returns></returns>
        public int ExecuteSqlCommand(string sqlQuery, params object[] sqlParams) {
            int numeroRows = 0;

            SqlCommand cmd = new SqlCommand(sqlQuery, db);
            cmd.CommandType = CommandType.Text;

            if (sqlParams != null)
                for (int i = 0; i < sqlParams.Length; i++)
                    cmd.Parameters.AddWithValue("p" + i.ToString(), sqlParams[i] == null ? DBNull.Value : sqlParams[i]);

            db.Open();
            numeroRows = cmd.ExecuteNonQuery();
            db.Close();

            return numeroRows;
        }

        /// <summary>
        /// Retorna uma lista com os dados de tipo T
        /// </summary>
        /// <typeparam name="T">Tipo de dados ao qual será retornado</typeparam>
        /// <param name="sqlQuery">Comando SQL</param>
        /// <param name="sqlParams">Parametros do SQL</param>
        /// <returns></returns>
        public List<T> SqlQuery<T>(string sqlQuery, params object[] sqlParams) {
            List<T> lista = new List<T>();
            T obj = Activator.CreateInstance<T>();
            int i = 0;

            SqlCommand cmd = new SqlCommand(sqlQuery, db);
            cmd.CommandType = CommandType.Text;

            if (sqlParams != null)
                for (i = 0; i < sqlParams.Length; i++)
                    cmd.Parameters.AddWithValue("p" + i.ToString(), sqlParams[i] == null ? DBNull.Value : sqlParams[i]);

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
    }
}
