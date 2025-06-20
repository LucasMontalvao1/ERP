CREATE TABLE IF NOT EXISTS roles (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(50) NOT NULL UNIQUE,
    Descricao VARCHAR(255) NULL,
    Ativo TINYINT(1) NOT NULL DEFAULT 1,
    
    -- Índices
    INDEX idx_roles_Nome (Nome),
    INDEX idx_roles_Ativo (Ativo)
);