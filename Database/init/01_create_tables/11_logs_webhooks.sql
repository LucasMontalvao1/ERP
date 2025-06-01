CREATE TABLE IF NOT EXISTS logs_webhooks (
    id INT AUTO_INCREMENT PRIMARY KEY,
    webhook_id INT NOT NULL,
    log_sincronizacao_id INT NOT NULL,
    evento VARCHAR(50) NOT NULL,
    payload_enviado LONGTEXT NOT NULL,
    resposta_recebida TEXT NULL,
    codigo_http INT NULL,
    sucesso TINYINT(1) NOT NULL DEFAULT 0,
    tempo_processamento_ms INT NOT NULL DEFAULT 0,
    numero_tentativa INT NOT NULL DEFAULT 1,
    erro TEXT NULL,
    
    -- 🎯 NOVO: Assinatura de segurança
    signature_enviada VARCHAR(255) NULL,
    signature_valida TINYINT(1) NULL,
    
    data_criacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_logs_webhooks_webhook (webhook_id),
    INDEX idx_logs_webhooks_log_sync (log_sincronizacao_id),
    INDEX idx_logs_webhooks_evento (evento),
    INDEX idx_logs_webhooks_sucesso (sucesso),
    INDEX idx_logs_webhooks_data (data_criacao),
    
    FOREIGN KEY (webhook_id) REFERENCES webhooks_notificacao(id) ON DELETE CASCADE,
    FOREIGN KEY (log_sincronizacao_id) REFERENCES logs_sincronizacao(id) ON DELETE CASCADE
);
