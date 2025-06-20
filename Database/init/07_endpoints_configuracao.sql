CREATE TABLE IF NOT EXISTS endpointsConfiguracao (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    ConfiguracaoId INT NOT NULL,
    Categoria VARCHAR(50) NOT NULL,
    Acao VARCHAR(50) NOT NULL,
    Endpoint VARCHAR(500) NOT NULL,
    MetodoHttp VARCHAR(10) NOT NULL DEFAULT 'POST',
    HeadersEspecificos TEXT NULL,
    TimeoutEspecifico INT NULL,
    Ativo TINYINT(1) NOT NULL DEFAULT 1,
    OrdemPrioridade INT NOT NULL DEFAULT 0,
    Observacoes TEXT NULL,
    DataCriacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    DataAtualizacao DATETIME NULL,
    
    -- Índices
    INDEX idx_endpoints_ConfiguracaoId (ConfiguracaoId),
    INDEX idx_endpoints_Categoria (Categoria),
    INDEX idx_endpoints_Acao (Acao),
    INDEX idx_endpoints_Ativo (Ativo),
    INDEX idx_endpoints_OrdemPrioridade (OrdemPrioridade),
    
    -- Chave única
    UNIQUE KEY uk_endpoint_config_cat_acao (ConfiguracaoId, Categoria, Acao),
    
    -- Constraints
    CONSTRAINT chk_endpoints_MetodoHttp CHECK (MetodoHttp IN ('GET', 'POST', 'PUT', 'DELETE', 'PATCH')),
    CONSTRAINT chk_endpoints_TimeoutEspecifico CHECK (TimeoutEspecifico IS NULL OR TimeoutEspecifico BETWEEN 5 AND 300)
);