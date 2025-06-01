CREATE TABLE IF NOT EXISTS endpoints_configuracao (
    id INT AUTO_INCREMENT PRIMARY KEY,
    configuracao_id INT NOT NULL,
    categoria VARCHAR(50) NOT NULL COMMENT 'auth, atividades, produtos, usuarios, etc.',
    acao VARCHAR(50) NOT NULL COMMENT 'login, list, create, update, delete, etc.',
    endpoint VARCHAR(500) NOT NULL COMMENT 'Path do endpoint, ex: /api/v1/atividades/{id}',
    metodo_http VARCHAR(10) NOT NULL DEFAULT 'POST' COMMENT 'GET, POST, PUT, DELETE, PATCH',
    headers_especificos JSON NULL COMMENT 'Headers específicos para este endpoint',
    timeout_especifico INT NULL COMMENT 'Timeout específico em segundos (sobrescreve configuração geral)',
    ativo TINYINT(1) NOT NULL DEFAULT 1,
    ordem_prioridade INT DEFAULT 0 COMMENT 'Ordem de prioridade quando há múltiplos endpoints',
    observacoes TEXT NULL,
    data_criacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    data_atualizacao DATETIME NULL,
    
    INDEX idx_endpoints_config (configuracao_id),
    INDEX idx_endpoints_categoria (categoria),
    INDEX idx_endpoints_acao (acao),
    INDEX idx_endpoints_ativo (ativo),
    INDEX idx_endpoints_prioridade (ordem_prioridade),
    
    UNIQUE KEY uk_endpoint_config_cat_acao (configuracao_id, categoria, acao),
    
    FOREIGN KEY (configuracao_id) REFERENCES configuracoes_integracao(id) ON DELETE CASCADE
);
