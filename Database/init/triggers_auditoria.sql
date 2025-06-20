DELIMITER $$

-- ===================================================================
-- TRIGGER: Atualizar DataAtualizacao automaticamente
-- ===================================================================

CREATE TRIGGER tr_usuarios_update_timestamp 
BEFORE UPDATE ON usuarios 
FOR EACH ROW 
BEGIN 
    SET NEW.DataAtualizacao = CURRENT_TIMESTAMP;
END$$

CREATE TRIGGER tr_atividades_update_timestamp 
BEFORE UPDATE ON atividades 
FOR EACH ROW 
BEGIN 
    SET NEW.DataAtualizacao = CURRENT_TIMESTAMP;
END$$

CREATE TRIGGER tr_configuracoes_integracao_update_timestamp 
BEFORE UPDATE ON configuracoes_integracao 
FOR EACH ROW 
BEGIN 
    SET NEW.DataAtualizacao = CURRENT_TIMESTAMP;
END$$

CREATE TRIGGER tr_endpointsConfiguracao_update_timestamp 
BEFORE UPDATE ON endpointsConfiguracao 
FOR EACH ROW 
BEGIN 
    SET NEW.DataAtualizacao = CURRENT_TIMESTAMP;
END$$

CREATE TRIGGER tr_logsSincronizacao_update_timestamp 
BEFORE UPDATE ON logsSincronizacao 
FOR EACH ROW 
BEGIN 
    SET NEW.DataAtualizacao = CURRENT_TIMESTAMP;
END$$

-- ===================================================================
-- TRIGGER: Garantir configuração padrão única
-- ===================================================================

CREATE TRIGGER tr_config_integracao_padrao_unica_insert 
BEFORE INSERT ON configuracoes_integracao 
FOR EACH ROW 
BEGIN 
    IF NEW.ConfiguracaoPadrao = 1 THEN
        UPDATE configuracoes_integracao 
        SET ConfiguracaoPadrao = 0 
        WHERE ConfiguracaoPadrao = 1;
    END IF;
END$$

CREATE TRIGGER tr_config_integracao_padrao_unica_update 
BEFORE UPDATE ON configuracoes_integracao 
FOR EACH ROW 
BEGIN 
    IF NEW.ConfiguracaoPadrao = 1 AND OLD.ConfiguracaoPadrao = 0 THEN
        UPDATE configuracoes_integracao 
        SET ConfiguracaoPadrao = 0 
        WHERE ConfiguracaoPadrao = 1 AND Id != NEW.Id;
    END IF;
END$$

-- ===================================================================
-- TRIGGER: Controlar status de sincronização das atividades
-- ===================================================================

CREATE TRIGGER tr_atividades_sync_status_update 
BEFORE UPDATE ON atividades 
FOR EACH ROW 
BEGIN 
    -- Se mudou dados importantes, marcar para resincronização
    IF (OLD.Ramo != NEW.Ramo OR 
        OLD.PercDesc != NEW.PercDesc OR 
        OLD.CalculaSt != NEW.CalculaSt) AND 
       OLD.StatusSincronizacao = 1 THEN -- Era sincronizado
        
        SET NEW.StatusSincronizacao = 0; -- Marcar como pendente
        SET NEW.TentativasSincronizacao = 0; -- Reset tentativas
        SET NEW.UltimoErroSincronizacao = NULL; -- Limpar erro anterior
    END IF;
END$$

-- ===================================================================
-- TRIGGER: Log automático de mudanças de status de atividades
-- ===================================================================

CREATE TRIGGER tr_atividades_log_status_change 
AFTER UPDATE ON atividades 
FOR EACH ROW 
BEGIN 
    -- Se mudou o status de sincronização, criar log
    IF OLD.StatusSincronizacao != NEW.StatusSincronizacao THEN
        INSERT INTO logsSincronizacao (
            ConfiguracaoId,
            CodAtiv,
            TipoOperacao,
            StatusProcessamento,
            MensagemErro,
            TempoProcessamentoMs,
            NumeroTentativa,
            Metadados
        ) VALUES (
            COALESCE(NEW.AtualizadoPor, 1), -- Usar ID do usuário ou 1 (sistema)
            NEW.CodAtiv,
            2, -- Update
            CASE 
                WHEN NEW.StatusSincronizacao = 1 THEN 1 -- Sucesso
                WHEN NEW.StatusSincronizacao = 2 THEN 2 -- Erro
                ELSE 0 -- Iniciado/Pendente
            END,
            NEW.UltimoErroSincronizacao,
            0, -- Tempo não calculado para mudanças manuais
            NEW.TentativasSincronizacao,
            CONCAT('{"status_anterior": ', OLD.StatusSincronizacao, 
                   ', "status_novo": ', NEW.StatusSincronizacao, 
                   ', "tipo": "mudanca_automatica"}')
        );
    END IF;
END$$

-- ===================================================================
-- TRIGGER: Controlar bloqueio de usuários
-- ===================================================================

CREATE TRIGGER tr_usuarios_controle_bloqueio 
BEFORE UPDATE ON usuarios 
FOR EACH ROW 
BEGIN 
    -- Se tentativas de login >= 5, bloquear usuário
    IF NEW.TentativasLogin >= 5 AND OLD.TentativasLogin < 5 THEN
        SET NEW.DataBloqueio = CURRENT_TIMESTAMP;
        SET NEW.Ativo = 0; -- Desativar usuário
    END IF;
    
    -- Se resetou tentativas, remover bloqueio
    IF NEW.TentativasLogin = 0 AND OLD.TentativasLogin > 0 THEN
        SET NEW.DataBloqueio = NULL;
        SET NEW.Ativo = 1; -- Reativar usuário
    END IF;
END$$

-- ===================================================================
-- TRIGGER: Log detalhado de login
-- ===================================================================

CREATE TRIGGER tr_log_login_detalhado 
AFTER INSERT ON logLoginTentativas 
FOR EACH ROW 
BEGIN 
    -- Atualizar contador de tentativas do usuário
    IF NEW.Sucesso = 0 THEN
        UPDATE usuarios 
        SET TentativasLogin = TentativasLogin + 1
        WHERE Login = NEW.Login;
    ELSE
        -- Login com sucesso - resetar tentativas e atualizar último login
        UPDATE usuarios 
        SET TentativasLogin = 0, 
            UltimoLogin = NEW.DataTentativa,
            PrimeiroAcesso = 0
        WHERE Login = NEW.Login;
    END IF;
END$$

DELIMITER ;

-- ===================================================================
-- VERIFICAÇÃO
-- ===================================================================
SELECT 'Triggers de auditoria criadas com sucesso!' as status;