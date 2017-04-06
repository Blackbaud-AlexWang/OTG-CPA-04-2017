﻿using System;
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
                var command = connection.CreateCommand();
                command.CommandText = sql;
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value);
                    }
                }

                connection.Open();
                var reader = await command.ExecuteReaderAsync();
                while (reader.Read())
                {
                    ret.Add(Mapper(reader));
                }
                connection.Close();
            }

            return ret;
        }
    }
}
