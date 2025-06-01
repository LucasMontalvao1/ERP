CREATE TABLE IF NOT EXISTS webhooks_notificacao (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    url_webhook VARCHAR(500) NOT NULL,
    eventos JSON NOT NULL COMMENT 'Array de eventos: ["sucesso", "erro", "timeout"]',
    headers_customizados JSON NULL,
    timeout_segundos INT NOT NULL DEFAULT 10,
    max_tentativas INT NOT NULL DEFAULT 3,
    ativo TINYINT(1) NOT NULL DEFAULT 1,
    
    filtros_categoria JSON NULL COMMENT 'Filtrar por categoria de endpoint',
    filtros_configuracao JSON NULL COMMENT 'Filtrar por configuração específica',
    
    secret_key VARCHAR(255) NULL COMMENT 'Chave para assinatura HMAC',
    enable_signature TINYINT(1) DEFAULT 0,
    
    data_criacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    data_atualizacao DATETIME NULL,
    
    INDEX idx_webhooks_ativo (ativo),
    INDEX idx_webhooks_nome (nome),
    
    CONSTRAINT chk_webhooks_timeout CHECK (timeout_segundos BETWEEN 5 AND 60),
    CONSTRAINT chk_webhooks_tentativas CHECK (max_tentativas BETWEEN 1 AND 5),
    
    UNIQUE KEY uk_webhook_nome (nome)
);