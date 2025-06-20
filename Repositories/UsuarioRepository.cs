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
            SenhaHash = row["senhahash"].ToString() ?? "",
            Ativo = Convert.ToBoolean(row["ativo"]),
            DataCriacao = Convert.ToDateTime(row["datacriacao"]),
            DataAtualizacao = row["dataatualizacao"] != DBNull.Value ? Convert.ToDateTime(row["dataatualizacao"]) : null,
            UltimoLogin = row["ultimologin"] != DBNull.Value ? Convert.ToDateTime(row["ultimologin"]) : null,
            TentativasLogin = Convert.ToInt32(row["tentativaslogin"]),
            DataBloqueio = row["databloqueio"] != DBNull.Value ? Convert.ToDateTime(row["databloqueio"]) : null,
            PrimeiroAcesso = Convert.ToBoolean(row["primeiroacesso"])
        };
    }

    protected override MySqlParameter[] GetInsertParameters(Usuario entity)
    {
        return new[]
        {
            new MySqlParameter("@nome", entity.Nome),
            new MySqlParameter("@login", entity.Login),
            new MySqlParameter("@email", entity.Email),
            new MySqlParameter("@senhahash", entity.SenhaHash),
            new MySqlParameter("@ativo", entity.Ativo),
            new MySqlParameter("@datacriacao", entity.DataCriacao),
            new MySqlParameter("@primeiroacesso", entity.PrimeiroAcesso)
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
            new MySqlParameter("@dataatualizacao", DateTime.UtcNow),
            new MySqlParameter("@primeiroacesso", entity.PrimeiroAcesso)
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
            var sql = _sqlLoader.GetQuery("Auth", "GetUsuarioByLogin");
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
            var sql = _sqlLoader.GetQuery("Auth", "GetUsuarioByEmail");
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
            var sql = _sqlLoader.GetQuery("Auth", "GetUsuarioRoles");
            var dataTable = await _databaseService.ExecuteQueryAsync(sql, new[] { new MySqlParameter("@usuarioid", usuarioId) });

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
            var sql = _sqlLoader.GetQuery("Auth", "UpdateUltimoLogin");
            await _databaseService.ExecuteNonQueryAsync(sql, new[]
            {
                new MySqlParameter("@usuarioid", usuarioId),
                new MySqlParameter("@ultimologin", DateTime.UtcNow)
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
            var sql = _sqlLoader.GetQuery("Auth", "IncrementTentativasLogin");
            var result = await _databaseService.ExecuteScalarAsync(sql, new[] { new MySqlParameter("@usuarioid", usuarioId) });
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
            var sql = _sqlLoader.GetQuery("Auth", "ResetTentativasLogin");
            await _databaseService.ExecuteNonQueryAsync(sql, new[] { new MySqlParameter("@usuarioid", usuarioId) });
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
            var sql = _sqlLoader.GetQuery("Auth", "LogTentativaLogin");
            await _databaseService.ExecuteNonQueryAsync(sql, new[]
            {
                new MySqlParameter("@login", login),
                new MySqlParameter("@sucesso", success),
                new MySqlParameter("@ipaddress", ipAddress),
                new MySqlParameter("@useragent", userAgent),
                new MySqlParameter("@datatentativa", DateTime.UtcNow)
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
            var sql = _sqlLoader.GetQuery("Auth", "UpdateSenha");
            await _databaseService.ExecuteNonQueryAsync(sql, new[]
            {
                new MySqlParameter("@usuarioid", usuarioId),
                new MySqlParameter("@senhahash", newPasswordHash),
                new MySqlParameter("@dataatualizacao", DateTime.UtcNow)
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
            var sql = _sqlLoader.GetQuery("Auth", "UpdatePrimeiroAcesso");
            await _databaseService.ExecuteNonQueryAsync(sql, new[]
            {
                new MySqlParameter("@usuarioid", usuarioId),
                new MySqlParameter("@primeiroacesso", firstAccess),
                new MySqlParameter("@dataatualizacao", DateTime.UtcNow)
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
            var sql = _sqlLoader.GetQuery("Auth", "GetUsuariosAtivos");
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
            string sql;
            var parameters = new List<MySqlParameter>
            {
                new("@login", login)
            };

            if (excludeUserId.HasValue)
            {
                sql = _sqlLoader.GetQuery("Auth", "CheckLoginExistsExcludeUser");
                parameters.Add(new MySqlParameter("@exclude_user_id", excludeUserId.Value));
            }
            else
            {
                sql = _sqlLoader.GetQuery("Auth", "CheckLoginExists");
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
            string sql;
            var parameters = new List<MySqlParameter>
            {
                new("@email", email)
            };

            if (excludeUserId.HasValue)
            {
                sql = _sqlLoader.GetQuery("Auth", "CheckEmailExistsExcludeUser");
                parameters.Add(new MySqlParameter("@exclude_user_id", excludeUserId.Value));
            }
            else
            {
                sql = _sqlLoader.GetQuery("Auth", "CheckEmailExists");
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