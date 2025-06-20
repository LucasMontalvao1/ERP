CREATE TABLE IF NOT EXISTS webhooksNotificacao (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL UNIQUE,
    Descricao VARCHAR(255) NULL,
    UrlWebhook VARCHAR(500) NOT NULL,
    Eventos JSON NOT NULL COMMENT 'Array: ["sucesso", "erro", "timeout"]',
    HeadersCustomizados JSON NULL,
    TimeoutSegundos INT NOT NULL DEFAULT 10,
    MaxTentativas INT NOT NULL DEFAULT 3,
    Ativo TINYINT(1) NOT NULL DEFAULT 1,
    SecretKey VARCHAR(255) NULL,
    EnableSignature TINYINT(1) NOT NULL DEFAULT 0,
    DataCriacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    DataAtualizacao DATETIME NULL,
    
    -- Índices
    INDEX idx_webhooks_Ativo (Ativo),
    INDEX idx_webhooks_Nome (Nome),
    
    -- Constraints
    CONSTRAINT chk_webhooks_TimeoutSegundos CHECK (TimeoutSegundos BETWEEN 5 AND 60),
    CONSTRAINT chk_webhooks_MaxTentativas CHECK (MaxTentativas BETWEEN 1 AND 5)
);