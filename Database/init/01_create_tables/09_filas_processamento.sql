CREATE TABLE IF NOT EXISTS filas_processamento (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nome_fila VARCHAR(100) NOT NULL,
    cod_ativ VARCHAR(50) NOT NULL,
    tipo_operacao TINYINT NOT NULL,
    status_fila TINYINT NOT NULL DEFAULT 0 COMMENT '0=Pendente, 1=Processando, 2=Processado, 3=Erro, 4=Cancelado',
    mensagem_json LONGTEXT NOT NULL,
    tentativas_processamento INT NOT NULL DEFAULT 0,
    max_tentativas INT NOT NULL DEFAULT 3,
    proximo_processamento DATETIME NULL,
    correlation_id VARCHAR(100) NOT NULL,
    erro_processamento TEXT NULL,
    
    prioridade TINYINT DEFAULT 5 COMMENT '1=Muito Alta, 5=Normal, 9=Baixa',
    
    data_criacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    data_processamento DATETIME NULL,
    
    INDEX idx_filas_nome (nome_fila),
    INDEX idx_filas_cod_ativ (cod_ativ),
    INDEX idx_filas_status (status_fila),
    INDEX idx_filas_correlation (correlation_id),
    INDEX idx_filas_proximo_proc (proximo_processamento),
    INDEX idx_filas_data_criacao (data_criacao),
    INDEX idx_filas_prioridade (prioridade, data_criacao),
    
    CONSTRAINT chk_filas_status CHECK (status_fila BETWEEN 0 AND 4),
    CONSTRAINT chk_filas_tentativas CHECK (tentativas_processamento >= 0),
    CONSTRAINT chk_filas_prioridade CHECK (prioridade BETWEEN 1 AND 9),
    
    FOREIGN KEY (cod_ativ) REFERENCES atividades(cod_ativ) ON DELETE CASCADE
);