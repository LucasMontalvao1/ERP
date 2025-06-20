CREATE TABLE IF NOT EXISTS usuarios (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    Login VARCHAR(50) NOT NULL UNIQUE,
    Email VARCHAR(254) NOT NULL UNIQUE,
    SenhaHash TEXT NOT NULL,
    Ativo TINYINT(1) NOT NULL DEFAULT 1,
    DataCriacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    DataAtualizacao DATETIME NULL,
    UltimoLogin DATETIME NULL,
    TentativasLogin INT NOT NULL DEFAULT 0,
    DataBloqueio DATETIME NULL,
    PrimeiroAcesso TINYINT(1) NOT NULL DEFAULT 1,
    
    -- Índices
    INDEX idx_usuarios_Login (Login),
    INDEX idx_usuarios_Email (Email),
    INDEX idx_usuarios_Ativo (Ativo),
    INDEX idx_usuarios_UltimoLogin (UltimoLogin),
    
    -- Constraints
    CONSTRAINT chk_usuarios_TentativasLogin CHECK (TentativasLogin BETWEEN 0 AND 10)
);