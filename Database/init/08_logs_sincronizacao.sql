CREATE TABLE IF NOT EXISTS logsSincronizacao (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    ConfiguracaoId INT NOT NULL,
    CodAtiv VARCHAR(50) NOT NULL,
    TipoOperacao INT NOT NULL COMMENT '1=Create, 2=Update, 3=Delete',
    StatusProcessamento INT NOT NULL COMMENT '0=Iniciado, 1=Sucesso, 2=Erro, 3=Timeout, 4=Cancelado, 5=Reprocessando',
    
    -- Detalhes do endpoint
    CategoriaEndpoint VARCHAR(50) NULL,
    AcaoEndpoint VARCHAR(50) NULL,
    EndpointUsado VARCHAR(500) NULL,
    MetodoHttpUsado VARCHAR(10) NULL,
    
    -- Dados da requisição/resposta
    PayloadEnviado LONGTEXT NULL,
    RespostaRecebida LONGTEXT NULL,
    CodigoHttp INT NULL,
    MensagemErro TEXT NULL,
    TempoProcessamentoMs BIGINT NOT NULL DEFAULT 0,
    
    -- Controle de retry
    NumeroTentativa INT NOT NULL DEFAULT 1,
    ProximaTentativa DATETIME NULL,
    
    -- Rastreabilidade
    JobId VARCHAR(100) NULL,
    Metadados TEXT NULL,
    CorrelationId VARCHAR(100) NULL,
    
    -- Informações da origem
    UserAgent VARCHAR(500) NULL,
    IpOrigem VARCHAR(45) NULL,
    TamanhoPayloadBytes INT NULL,
    TamanhoRespostaBytes INT NULL,
    
    -- Auditoria
    DataCriacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    DataAtualizacao DATETIME NULL,
    Version DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    -- Índices otimizados
    INDEX idx_logs_ConfiguracaoId (ConfiguracaoId),
    INDEX idx_logs_CodAtiv (CodAtiv),
    INDEX idx_logs_TipoOperacao (TipoOperacao),
    INDEX idx_logs_StatusProcessamento (StatusProcessamento),
    INDEX idx_logs_DataCriacao (DataCriacao),
    INDEX idx_logs_CorrelationId (CorrelationId),
    INDEX idx_logs_JobId (JobId),
    INDEX idx_logs_ProximaTentativa (ProximaTentativa),
    INDEX idx_logs_CategoriaAcao (CategoriaEndpoint, AcaoEndpoint),
    INDEX idx_logs_CodigoHttp (CodigoHttp),
    
    -- Constraints
    CONSTRAINT chk_logs_TipoOperacao CHECK (TipoOperacao BETWEEN 1 AND 3),
    CONSTRAINT chk_logs_StatusProcessamento CHECK (StatusProcessamento BETWEEN 0 AND 5),
    CONSTRAINT chk_logs_NumeroTentativa CHECK (NumeroTentativa > 0)
);