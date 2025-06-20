CREATE TABLE IF NOT EXISTS filasProcessamento (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    NomeFila VARCHAR(100) NOT NULL,
    CodAtiv VARCHAR(50) NOT NULL,
    TipoOperacao INT NOT NULL,
    StatusFila INT NOT NULL DEFAULT 0 COMMENT '0=Pendente, 1=Processando, 2=Processado, 3=Erro, 4=Cancelado',
    MensagemJson LONGTEXT NOT NULL,
    TentativasProcessamento INT NOT NULL DEFAULT 0,
    MaxTentativas INT NOT NULL DEFAULT 3,
    ProximoProcessamento DATETIME NULL,
    CorrelationId VARCHAR(100) NOT NULL,
    ErroProcessamento TEXT NULL,
    Prioridade INT NOT NULL DEFAULT 5 COMMENT '1=Muito Alta, 5=Normal, 9=Baixa',
    DataCriacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    DataProcessamento DATETIME NULL,
    
    -- Índices
    INDEX idx_filas_NomeFila (NomeFila),
    INDEX idx_filas_CodAtiv (CodAtiv),
    INDEX idx_filas_StatusFila (StatusFila),
    INDEX idx_filas_CorrelationId (CorrelationId),
    INDEX idx_filas_ProximoProcessamento (ProximoProcessamento),
    INDEX idx_filas_DataCriacao (DataCriacao),
    INDEX idx_filas_Prioridade (Prioridade),
    
    -- Índices compostos
    INDEX idx_filas_processamento (StatusFila, Prioridade, ProximoProcessamento),
    
    -- Constraints
    CONSTRAINT chk_filas_StatusFila CHECK (StatusFila BETWEEN 0 AND 4),
    CONSTRAINT chk_filas_TentativasProcessamento CHECK (TentativasProcessamento >= 0),
    CONSTRAINT chk_filas_Prioridade CHECK (Prioridade BETWEEN 1 AND 9)
);