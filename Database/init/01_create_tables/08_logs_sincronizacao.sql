CREATE TABLE IF NOT EXISTS logs_sincronizacao (
    id INT AUTO_INCREMENT PRIMARY KEY,
    configuracao_id INT NOT NULL,
    cod_ativ VARCHAR(50) NOT NULL,
    tipo_operacao TINYINT NOT NULL COMMENT '1=Create, 2=Update, 3=Delete',
    status_processamento TINYINT NOT NULL COMMENT '0=Iniciado, 1=Sucesso, 2=Erro, 3=Timeout, 4=Cancelado, 5=Reprocessando',
    
    categoria_endpoint VARCHAR(50) NULL,
    acao_endpoint VARCHAR(50) NULL,
    endpoint_usado VARCHAR(500) NULL,
    metodo_http_usado VARCHAR(10) NULL,
    
    payload_enviado LONGTEXT NULL,
    resposta_recebida LONGTEXT NULL,
    codigo_http INT NULL,
    mensagem_erro TEXT NULL,
    tempo_processamento_ms BIGINT NOT NULL DEFAULT 0,
    numero_tentativa INT NOT NULL DEFAULT 1,
    proxima_tentativa DATETIME NULL,
    job_id VARCHAR(100) NULL,
    metadados JSON NULL,
    correlation_id VARCHAR(100) NULL,
    
    user_agent VARCHAR(500) NULL,
    ip_origem VARCHAR(45) NULL,
    tamanho_payload_bytes INT NULL,
    tamanho_resposta_bytes INT NULL,
    
    data_criacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    data_atualizacao DATETIME NULL,
    version TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    INDEX idx_logs_configuracao (configuracao_id),
    INDEX idx_logs_cod_ativ (cod_ativ),
    INDEX idx_logs_tipo_operacao (tipo_operacao),
    INDEX idx_logs_status (status_processamento),
    INDEX idx_logs_data_criacao (data_criacao),
    INDEX idx_logs_correlation (correlation_id),
    INDEX idx_logs_job_id (job_id),
    INDEX idx_logs_proxima_tentativa (proxima_tentativa),
    INDEX idx_logs_endpoint_categoria (categoria_endpoint, acao_endpoint),
    INDEX idx_logs_codigo_http (codigo_http),
    
    CONSTRAINT chk_logs_tipo_operacao CHECK (tipo_operacao BETWEEN 1 AND 3),
    CONSTRAINT chk_logs_status CHECK (status_processamento BETWEEN 0 AND 5),
    CONSTRAINT chk_logs_numero_tentativa CHECK (numero_tentativa > 0),
    
    FOREIGN KEY (configuracao_id) REFERENCES configuracoes_integracao(id) ON DELETE CASCADE,
    FOREIGN KEY (cod_ativ) REFERENCES atividades(cod_ativ) ON DELETE CASCADE
);