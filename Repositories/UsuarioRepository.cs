using API.Infra.Data;
using API.Models.Entities;
using API.Repositories.Base;
using API.Repositories.Interfaces;
using API.SQL;
using MySqlConnector;
using System.Data;

namespace API.Repositories;

public class UsuarioRepository : BaseRepository<Usuario>, IUsuarioRepository
{
    private readonly SqlLoader _sqlLoader;

    public UsuarioRepository(IDatabaseService databaseService, SqlLoader sqlLoader, ILogger<UsuarioRepository> logger)
        : base(databaseService, logger)
    {
        _sqlLoader = sqlLoader;
    }

    protected override string TableName => "usuarios";
    protected override string IdColumn => "id";

    protected override Usuario MapFromDataRow(DataRow row)
    {
        return new Usuario
        {
            Id = Convert.ToInt32(row["id"]),
            Nome = row["nome"].ToString() ?? "",
            Login = row["login"].ToString() ?? "",
            Email = row["email"].ToString() ?? "",
            SenhaHash = row["senha_hash"].ToString() ?? "",
            Ativo = Convert.ToBoolean(row["ativo"]),
            DataCriacao = Convert.ToDateTime(row["data_criacao"]),
            DataAtualizacao = row["data_atualizacao"] != DBNull.Value ? Convert.ToDateTime(row["data_atualizacao"]) : null,
            UltimoLogin = row["ultimo_login"] != DBNull.Value ? Convert.ToDateTime(row["ultimo_login"]) : null,
            TentativasLogin = Convert.ToInt32(row["tentativas_login"]),
            DataBloqueio = row["data_bloqueio"] != DBNull.Value ? Convert.ToDateTime(row["data_bloqueio"]) : null,
            PrimeiroAcesso = Convert.ToBoolean(row["primeiro_acesso"])
        };
    }

    protected override MySqlParameter[] GetInsertParameters(Usuario entity)
    {
        return new[]
        {
            new MySqlParameter("@nome", entity.Nome),
            new MySqlParameter("@login", entity.Login),
            new MySqlParameter("@email", entity.Email),
            new MySqlParameter("@senha_hash", entity.SenhaHash),
            new MySqlParameter("@ativo", entity.Ativo),
            new MySqlParameter("@data_criacao", entity.DataCriacao),
            new MySqlParameter("@primeiro_acesso", entity.PrimeiroAcesso)
        };
    }

    protected override MySqlParameter[] GetUpdateParameters(Usuario entity)
    {
        return new[]
        {
            new MySqlParameter("@id", entity.Id),
            new MySqlParameter("@nome", entity.Nome),
            new MySqlParameter("@login", entity.Login),
            new MySqlParameter("@email", entity.Email),
            new MySqlParameter("@ativo", entity.Ativo),
            new MySqlParameter("@data_atualizacao", DateTime.UtcNow),
            new MySqlParameter("@primeiro_acesso", entity.PrimeiroAcesso)
        };
    }

    protected override void SetEntityId(Usuario entity, int id)
    {
        entity.Id = id;
    }

    public async Task<Usuario?> GetByLoginAsync(string login)
    {
        try
        {
            var sql = await _sqlLoader.LoadSqlAsync("Auth/GetUsuarioByLogin.sql");
            var dataTable = await _databaseService.ExecuteQueryAsync(sql, new[] { new MySqlParameter("@login", login) });

            if (dataTable.Rows.Count == 0)
                return null;

            return MapFromDataRow(dataTable.Rows[0]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar usuário por login: {Login}", login);
            throw;
        }
    }

    public async Task<Usuario?> GetByEmailAsync(string email)
    {
        try
        {
            var sql = await _sqlLoader.LoadSqlAsync("Auth/GetUsuarioByEmail.sql");
            var dataTable = await _databaseService.ExecuteQueryAsync(sql, new[] { new MySqlParameter("@email", email) });

            if (dataTable.Rows.Count == 0)
                return null;

            return MapFromDataRow(dataTable.Rows[0]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar usuário por email: {Email}", email);
            throw;
        }
    }

    public async Task<List<string>> GetUserRolesAsync(int usuarioId)
    {
        try
        {
            var sql = await _sqlLoader.LoadSqlAsync("Auth/GetUsuarioRoles.sql");
            var dataTable = await _databaseService.ExecuteQueryAsync(sql, new[] { new MySqlParameter("@usuario_id", usuarioId) });

            var roles = new List<string>();
            foreach (DataRow row in dataTable.Rows)
            {
                roles.Add(row["nome"].ToString() ?? "");
            }

            return roles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar roles do usuário: {UsuarioId}", usuarioId);
            throw;
        }
    }

    public async Task UpdateLastLoginAsync(int usuarioId)
    {
        try
        {
            var sql = await _sqlLoader.LoadSqlAsync("Auth/UpdateUltimoLogin.sql");
            await _databaseService.ExecuteNonQueryAsync(sql, new[]
            {
                new MySqlParameter("@usuario_id", usuarioId),
                new MySqlParameter("@ultimo_login", DateTime.UtcNow)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar último login: {UsuarioId}", usuarioId);
            throw;
        }
    }

    public async Task<int> IncrementLoginAttemptsAsync(int usuarioId)
    {
        try
        {
            var sql = await _sqlLoader.LoadSqlAsync("Auth/IncrementTentativasLogin.sql");
            var result = await _databaseService.ExecuteScalarAsync(sql, new[] { new MySqlParameter("@usuario_id", usuarioId) });
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao incrementar tentativas de login: {UsuarioId}", usuarioId);
            throw;
        }
    }

    public async Task ResetLoginAttemptsAsync(int usuarioId)
    {
        try
        {
            var sql = await _sqlLoader.LoadSqlAsync("Auth/ResetTentativasLogin.sql");
            await _databaseService.ExecuteNonQueryAsync(sql, new[] { new MySqlParameter("@usuario_id", usuarioId) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao resetar tentativas de login: {UsuarioId}", usuarioId);
            throw;
        }
    }

    public async Task LogLoginAttemptAsync(string login, bool success, string ipAddress, string userAgent)
    {
        try
        {
            var sql = await _sqlLoader.LoadSqlAsync("Auth/LogTentativaLogin.sql");
            await _databaseService.ExecuteNonQueryAsync(sql, new[]
            {
                new MySqlParameter("@login", login),
                new MySqlParameter("@sucesso", success),
                new MySqlParameter("@ip_address", ipAddress),
                new MySqlParameter("@user_agent", userAgent),
                new MySqlParameter("@data_tentativa", DateTime.UtcNow)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar tentativa de login: {Login}", login);
        }
    }

    public async Task UpdatePasswordAsync(int usuarioId, string newPasswordHash)
    {
        try
        {
            var sql = await _sqlLoader.LoadSqlAsync("Auth/UpdateSenha.sql");
            await _databaseService.ExecuteNonQueryAsync(sql, new[]
            {
                new MySqlParameter("@usuario_id", usuarioId),
                new MySqlParameter("@senha_hash", newPasswordHash),
                new MySqlParameter("@data_atualizacao", DateTime.UtcNow)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar senha: {UsuarioId}", usuarioId);
            throw;
        }
    }

    public async Task UpdateFirstAccessAsync(int usuarioId, bool firstAccess)
    {
        try
        {
            var sql = await _sqlLoader.LoadSqlAsync("Auth/UpdatePrimeiroAcesso.sql");
            await _databaseService.ExecuteNonQueryAsync(sql, new[]
            {
                new MySqlParameter("@usuario_id", usuarioId),
                new MySqlParameter("@primeiro_acesso", firstAccess),
                new MySqlParameter("@data_atualizacao", DateTime.UtcNow)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar primeiro acesso: {UsuarioId}", usuarioId);
            throw;
        }
    }

    public async Task<List<Usuario>> GetActiveUsersAsync()
    {
        try
        {
            var sql = await _sqlLoader.LoadSqlAsync("Auth/GetUsuariosAtivos.sql");
            var dataTable = await _databaseService.ExecuteQueryAsync(sql);

            var usuarios = new List<Usuario>();
            foreach (DataRow row in dataTable.Rows)
            {
                usuarios.Add(MapFromDataRow(row));
            }

            return usuarios;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar usuários ativos");
            throw;
        }
    }

    public async Task<bool> LoginExistsAsync(string login, int? excludeUserId = null)
    {
        try
        {
            var sql = await _sqlLoader.LoadSqlAsync("Auth/CheckLoginExists.sql");
            var parameters = new List<MySqlParameter>
            {
                new("@login", login)
            };

            if (excludeUserId.HasValue)
            {
                sql = await _sqlLoader.LoadSqlAsync("Auth/CheckLoginExistsExcludeUser.sql");
                parameters.Add(new MySqlParameter("@exclude_user_id", excludeUserId.Value));
            }

            var result = await _databaseService.ExecuteScalarAsync(sql, parameters.ToArray());
            return Convert.ToInt32(result) > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar se login existe: {Login}", login);
            throw;
        }
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeUserId = null)
    {
        try
        {
            var sql = await _sqlLoader.LoadSqlAsync("Auth/CheckEmailExists.sql");
            var parameters = new List<MySqlParameter>
            {
                new("@email", email)
            };

            if (excludeUserId.HasValue)
            {
                sql = await _sqlLoader.LoadSqlAsync("Auth/CheckEmailExistsExcludeUser.sql");
                parameters.Add(new MySqlParameter("@exclude_user_id", excludeUserId.Value));
            }

            var result = await _databaseService.ExecuteScalarAsync(sql, parameters.ToArray());
            return Convert.ToInt32(result) > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar se email existe: {Email}", email);
            throw;
        }
    }
}