using System.ComponentModel.DataAnnotations;

namespace API.Models.Entities;

public class Usuario
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Login { get; set; } = string.Empty;

    [Required, MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string SenhaHash { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataAtualizacao { get; set; }
    public DateTime? UltimoLogin { get; set; }
    public int TentativasLogin { get; set; } = 0;
    public DateTime? DataBloqueio { get; set; }
    public bool PrimeiroAcesso { get; set; } = true;
}

public class Role
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public bool Ativo { get; set; } = true;
}

public class UsuarioRole
{
    public int UsuarioId { get; set; }
    public int RoleId { get; set; }
    public DateTime DataAtribuicao { get; set; } = DateTime.UtcNow;
}

public class LogLoginTentativa
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public bool Sucesso { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime DataTentativa { get; set; } = DateTime.UtcNow;
}