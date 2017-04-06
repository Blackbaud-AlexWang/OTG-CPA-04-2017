using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Database
{
    public class SqlHandler
    {
        private string _connectionString { get; set; }

        public SqlHandler(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, Dictionary<string, dynamic> parameters, Func<SqlDataReader, T> Mapper)
        {
            var ret = new List<T>();

            using (var connection = new SqlConnection(_connectionString))
            {
                var command = CreateSqlCommand(connection, sql, parameters);

                connection.Open();
                var reader = await command.ExecuteReaderAsync();
                while (reader.Read() && Mapper != null)
                {
                    ret.Add(Mapper(reader));
                }
                connection.Close();
            }

            return ret;
        }

        public async Task<int> ExecuteAsync(string sql, Dictionary<string, dynamic> parameters)
        {
            var output = 0;
            using (var connection = new SqlConnection(_connectionString))
            {
                var command = CreateSqlCommand(connection, sql, parameters);

                connection.Open();
                output = await command.ExecuteNonQueryAsync();
                connection.Close();
            }

            return output;
        }

        private SqlCommand CreateSqlCommand(SqlConnection connection, string sql, Dictionary<string, dynamic> parameters)
        {
            var command = connection.CreateCommand();
            command.CommandText = sql;
            if (parameters == null)
            {
                return command;
            }

            foreach (var param in parameters)
            {
                command.Parameters.AddWithValue(param.Key, param.Value);
            }

            return command;
        }
    }
}
