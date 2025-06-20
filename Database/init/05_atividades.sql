CREATE TABLE IF NOT EXISTS atividades (
    CodAtiv VARCHAR(50) NOT NULL PRIMARY KEY,
    Ramo VARCHAR(100) NOT NULL,
    PercDesc DECIMAL(5,2) NOT NULL,
    CalculaSt VARCHAR(1) NOT NULL DEFAULT 'N',
    
    -- Status de sincronização
    StatusSincronizacao INT NOT NULL DEFAULT 0 COMMENT '0=Pendente, 1=Sincronizado, 2=Erro, 3=Reprocessando, 4=Cancelado',
    DataUltimaSincronizacao DATETIME NULL,
    TentativasSincronizacao INT NOT NULL DEFAULT 0,
    UltimoErroSincronizacao TEXT NULL,
    
    -- Auditoria
    DataCriacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    DataAtualizacao DATETIME NULL,
    CriadoPor INT NULL,
    AtualizadoPor INT NULL,
    Version DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    -- Índices
    INDEX idx_atividades_Ramo (Ramo),
    INDEX idx_atividades_CalculaSt (CalculaSt),
    INDEX idx_atividades_StatusSincronizacao (StatusSincronizacao),
    INDEX idx_atividades_DataCriacao (DataCriacao),
    INDEX idx_atividades_DataUltimaSincronizacao (DataUltimaSincronizacao),
    
    -- Constraints
    CONSTRAINT chk_atividades_CalculaSt CHECK (CalculaSt IN ('S', 'N')),
    CONSTRAINT chk_atividades_StatusSincronizacao CHECK (StatusSincronizacao BETWEEN 0 AND 4),
    CONSTRAINT chk_atividades_PercDesc CHECK (PercDesc BETWEEN -99.99 AND 99.99)
);