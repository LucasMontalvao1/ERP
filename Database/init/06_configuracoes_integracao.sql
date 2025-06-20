CREATE TABLE IF NOT EXISTS configuracoes_integracao (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL UNIQUE,
    Descricao VARCHAR(255) NULL,
    UrlApi VARCHAR(500) NOT NULL,
    Login VARCHAR(100) NOT NULL,
    SenhaCriptografada VARCHAR(500) NOT NULL,  -- ← Campo que estava faltando!
    
    -- Endpoints e configurações
    Endpoints TEXT NULL COMMENT 'JSON com endpoints organizados',
    VersaoApi VARCHAR(10) NOT NULL DEFAULT 'v1',
    EndpointLogin VARCHAR(200) NOT NULL DEFAULT '/auth/login',
    EndpointPrincipal VARCHAR(200) NOT NULL DEFAULT '/api',
    
    -- Token e autenticação
    TokenAtual TEXT NULL,
    TokenExpiracao DATETIME NULL,
    
    -- Configurações de comportamento
    Ativo TINYINT(1) NOT NULL DEFAULT 1,
    TimeoutSegundos INT NOT NULL DEFAULT 30,
    MaxTentativas INT NOT NULL DEFAULT 3,
    HeadersCustomizados TEXT NULL COMMENT 'JSON com headers customizados',
    ConfiguracaoPadrao TINYINT(1) NOT NULL DEFAULT 0,
    
    -- Políticas de retry
    RetryPolicy VARCHAR(50) NOT NULL DEFAULT 'exponential',
    RetryDelayBaseSeconds INT NOT NULL DEFAULT 60,
    EnableCircuitBreaker TINYINT(1) NOT NULL DEFAULT 0,
    CircuitBreakerThreshold INT NOT NULL DEFAULT 5,
    
    -- Auditoria
    DataCriacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    DataAtualizacao DATETIME NULL,
    CriadoPor INT NULL,
    AtualizadoPor INT NULL,
    Version DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    -- Índices
    INDEX idx_config_Ativo (Ativo),
    INDEX idx_config_Nome (Nome),
    INDEX idx_config_ConfiguracaoPadrao (ConfiguracaoPadrao),
    INDEX idx_config_VersaoApi (VersaoApi),
    
    -- Constraints
    CONSTRAINT chk_config_TimeoutSegundos CHECK (TimeoutSegundos BETWEEN 5 AND 300),
    CONSTRAINT chk_config_MaxTentativas CHECK (MaxTentativas BETWEEN 1 AND 10),
    CONSTRAINT chk_config_RetryDelayBaseSeconds CHECK (RetryDelayBaseSeconds BETWEEN 10 AND 3600),
    CONSTRAINT chk_config_CircuitBreakerThreshold CHECK (CircuitBreakerThreshold BETWEEN 1 AND 100)
);