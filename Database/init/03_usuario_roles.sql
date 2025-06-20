CREATE TABLE IF NOT EXISTS usuarioRoles (
    UsuarioId INT NOT NULL,
    RoleId INT NOT NULL,
    DataAtribuicao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    PRIMARY KEY (UsuarioId, RoleId),
    
    -- Índices
    INDEX idx_usuarioRoles_UsuarioId (UsuarioId),
    INDEX idx_usuarioRoles_RoleId (RoleId)
);