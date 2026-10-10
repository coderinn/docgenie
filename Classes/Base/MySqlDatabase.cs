using Dapper;
using MySqlConnector;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;
using System.Data;

namespace LeadsPortal.Classes.Base
{
    public class MySqlDatabase
    {
        private readonly string _connectionString;
        private readonly QueryFactory _queryFactory;

        public MySqlDatabase(string connectionString)
        {
            _connectionString = connectionString;

            // Create SQLKata QueryFactory with MySQL compiler
            var compiler = new MySqlCompiler();
            var connection = CreateConnection();

            _queryFactory = new QueryFactory(connection, compiler);
        }

        private IDbConnection CreateConnection()
        {
            return new MySqlConnection(_connectionString);
        }

        public async Task<int> ExecuteAsync(
            string sql,
            object? parameters = null)
        {
            using (IDbConnection connection = CreateConnection())
            {
                connection.Open();

                return await connection.ExecuteAsync(
                    sql,
                    parameters);
            }
        }

        public async Task<T> QuerySingleAsync<T>(
            string sql,
            object? parameters = null)
        {
            using (IDbConnection connection = CreateConnection())
            {
                connection.Open();

                return await connection.QuerySingleAsync<T>(
                    sql,
                    parameters);
            }
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(
            string sql,
            object? parameters = null)
        {
            using (IDbConnection connection = CreateConnection())
            {
                connection.Open();

                return await connection.QueryAsync<T>(
                    sql,
                    parameters);
            }
        }

        public async Task<int> ExecuteWithIdentityAsync(
            string sql,
            object? parameters = null)
        {
            using (IDbConnection connection = CreateConnection())
            {
                connection.Open();

                string identitySql =
                    sql + " ; SELECT LAST_INSERT_ID();";

                return await connection.ExecuteScalarAsync<int>(
                    identitySql,
                    parameters);
            }
        }

        public async Task<int> InsertAsync<T>(
            string sql,
            T entity)
        {
            return await ExecuteAsync(sql, entity);
        }

        public async Task<int> ExecuteStoredProcedureAsync(
            string procedureName,
            object? parameters = null)
        {
            using (IDbConnection connection = CreateConnection())
            {
                connection.Open();

                return await connection.ExecuteAsync(
                    procedureName,
                    parameters,
                    commandType: CommandType.StoredProcedure);
            }
        }

        // SQLKata QueryFactory
        public QueryFactory QueryFactory => _queryFactory;

        public async Task<IEnumerable<dynamic>> QueryWithKataAsync(
            Query query)
        {
            return await _queryFactory
                .FromQuery(query)
                .GetAsync();
        }

        public async Task<int> InsertWithKataAsync(
            Query query,
            object data)
        {
            return await _queryFactory
                .FromQuery(query)
                .InsertAsync(data);
        }

        public async Task<int> UpdateWithKataAsync(
            Query query,
            object data)
        {
            return await _queryFactory
                .FromQuery(query)
                .UpdateAsync(data);
        }

        public async Task<int> DeleteWithKataAsync(
            Query query)
        {
            return await _queryFactory
                .FromQuery(query)
                .DeleteAsync();
        }
    }
}