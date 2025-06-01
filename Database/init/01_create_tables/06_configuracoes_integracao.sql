CREATE TABLE IF NOT EXISTS configuracoes_integracao (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(100) NOT NULL UNIQUE,
    descricao VARCHAR(255) NULL,
    url_api VARCHAR(500) NOT NULL,
    login VARCHAR(100) NOT NULL,
    senha_criptografada TEXT NOT NULL,
    
    endpoints JSON NULL COMMENT 'Estrutura JSON com endpoints organizados por categoria e ação',
    
    versao_api VARCHAR(10) DEFAULT 'v1',
    
    endpoint_login VARCHAR(200) DEFAULT '/auth/login',
    endpoint_principal VARCHAR(200) DEFAULT '/api',
    
    token_atual TEXT NULL,
    token_expiracao DATETIME NULL,
    ativo TINYINT(1) NOT NULL DEFAULT 1,
    timeout_segundos INT NOT NULL DEFAULT 30,
    max_tentativas INT NOT NULL DEFAULT 3,
    headers_customizados JSON NULL,
    configuracao_padrao TINYINT(1) NOT NULL DEFAULT 0,
    
    retry_policy VARCHAR(50) DEFAULT 'exponential' COMMENT 'linear, exponential, fixed',
    retry_delay_base_seconds INT DEFAULT 60 COMMENT 'Delay base para retry em segundos',
    enable_circuit_breaker TINYINT(1) DEFAULT 0,
    circuit_breaker_threshold INT DEFAULT 5,
    
    data_criacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    data_atualizacao DATETIME NULL,
    criado_por INT NULL,
    atualizado_por INT NULL,
    version TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    INDEX idx_config_ativo (ativo),
    INDEX idx_config_nome (nome),
    INDEX idx_config_padrao (configuracao_padrao),
    INDEX idx_config_versao_api (versao_api),
    
    CONSTRAINT chk_config_timeout CHECK (timeout_segundos BETWEEN 5 AND 300),
    CONSTRAINT chk_config_max_tentativas CHECK (max_tentativas BETWEEN 1 AND 10),
    CONSTRAINT chk_config_retry_delay CHECK (retry_delay_base_seconds BETWEEN 10 AND 3600),
    CONSTRAINT chk_config_circuit_threshold CHECK (circuit_breaker_threshold BETWEEN 1 AND 100),
    
    FOREIGN KEY (criado_por) REFERENCES usuarios(id) ON DELETE SET NULL,
    FOREIGN KEY (atualizado_por) REFERENCES usuarios(id) ON DELETE SET NULL
);