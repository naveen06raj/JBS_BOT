using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using ERP.API.Models;
using Microsoft.Extensions.Logging;

namespace ERP.API.Services
{
    public class UserService : IUserService
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<UserService> _logger;

        public UserService(IDbConnection connection, ILogger<UserService> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<IEnumerable<UserDropdownDto>> GetPresenterDropdownAsync()
        {
            const string sql = @"SELECT user_id AS Id, username FROM users ORDER BY username";
            return await _connection.QueryAsync<UserDropdownDto>(sql);
        }
    }
}
