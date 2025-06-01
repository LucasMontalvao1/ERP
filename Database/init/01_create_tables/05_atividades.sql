CREATE TABLE IF NOT EXISTS atividades (
    cod_ativ VARCHAR(50) NOT NULL PRIMARY KEY,
    ramo VARCHAR(100) NOT NULL,
    perc_desc DECIMAL(5,2) NOT NULL,
    calcula_st VARCHAR(1) NOT NULL DEFAULT 'N',
    status_sincronizacao TINYINT NOT NULL DEFAULT 0 COMMENT '0=Pendente, 1=Sincronizado, 2=Erro, 3=Reprocessando, 4=Cancelado',
    data_ultima_sincronizacao DATETIME NULL,
    tentativas_sincronizacao INT NOT NULL DEFAULT 0,
    ultimo_erro_sincronizacao TEXT NULL,
    data_criacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    data_atualizacao DATETIME NULL,
    criado_por INT NULL,
    atualizado_por INT NULL,
    version TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    INDEX idx_atividades_ramo (ramo),
    INDEX idx_atividades_calcula_st (calcula_st),
    INDEX idx_atividades_status_sync (status_sincronizacao),
    INDEX idx_atividades_data_criacao (data_criacao),
    INDEX idx_atividades_data_sync (data_ultima_sincronizacao),
    
    CONSTRAINT chk_atividades_calcula_st CHECK (calcula_st IN ('S', 'N')),
    CONSTRAINT chk_atividades_status_sync CHECK (status_sincronizacao BETWEEN 0 AND 4),
    CONSTRAINT chk_atividades_perc_desc CHECK (perc_desc BETWEEN -99.99 AND 99.99),
    
    FOREIGN KEY (criado_por) REFERENCES usuarios(id) ON DELETE SET NULL,
    FOREIGN KEY (atualizado_por) REFERENCES usuarios(id) ON DELETE SET NULL
);