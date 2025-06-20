CREATE TABLE IF NOT EXISTS logsWebhooks (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    WebhookId INT NOT NULL,
    LogSincronizacaoId INT NULL,
    Evento VARCHAR(50) NOT NULL,
    PayloadEnviado LONGTEXT NOT NULL,
    RespostaRecebida TEXT NULL,
    CodigoHttp INT NULL,
    Sucesso TINYINT(1) NOT NULL DEFAULT 0,
    TempoProcessamentoMs INT NOT NULL DEFAULT 0,
    NumeroTentativa INT NOT NULL DEFAULT 1,
    Erro TEXT NULL,
    DataCriacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Índices
    INDEX idx_logsWebhooks_WebhookId (WebhookId),
    INDEX idx_logsWebhooks_LogSincronizacaoId (LogSincronizacaoId),
    INDEX idx_logsWebhooks_Evento (Evento),
    INDEX idx_logsWebhooks_Sucesso (Sucesso),
    INDEX idx_logsWebhooks_DataCriacao (DataCriacao),
    
    -- Constraints
    CONSTRAINT chk_logsWebhooks_NumeroTentativa CHECK (NumeroTentativa > 0)
);