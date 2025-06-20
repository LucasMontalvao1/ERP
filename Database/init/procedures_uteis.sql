DELIMITER $$

-- ===================================================================
-- PROCEDURE: Limpar logs antigos
-- ===================================================================
CREATE PROCEDURE sp_limpar_logs_antigos(
    IN dias_manter INT DEFAULT 30
)
BEGIN
    DECLARE data_limite DATETIME;
    DECLARE logs_removidos INT DEFAULT 0;
    
    SET data_limite = DATE_SUB(NOW(), INTERVAL dias_manter DAY);
    
    -- Limpar logs de sincronização antigos
    DELETE FROM logsSincronizacao WHERE DataCriacao < data_limite;
    SET logs_removidos = logs_removidos + ROW_COUNT();
    
    -- Limpar logs de login antigos
    DELETE FROM logLoginTentativas WHERE DataTentativa < data_limite;
    SET logs_removidos = logs_removidos + ROW_COUNT();
    
    -- Limpar filas processadas há mais tempo
    DELETE FROM filasProcessamento 
    WHERE StatusFila IN (2, 4) -- Processado ou Cancelado
      AND DataCriacao < data_limite;
    SET logs_removidos = logs_removidos + ROW_COUNT();
    
    SELECT 
        logs_removidos as 'Registros removidos', 
        data_limite as 'Data limite',
        NOW() as 'Data execucao';
END$$

-- ===================================================================
-- PROCEDURE: Reprocessar atividades com erro
-- ===================================================================
CREATE PROCEDURE sp_reprocessar_atividades_erro(
    IN limite_registros INT DEFAULT 100
)
BEGIN
    DECLARE done INT DEFAULT FALSE;
    DECLARE cod_ativ_var VARCHAR(50);
    DECLARE contador INT DEFAULT 0;
    
    DECLARE cursor_atividades CURSOR FOR
        SELECT CodAtiv
        FROM atividades 
        WHERE StatusSincronizacao = 2 -- Erro
          AND TentativasSincronizacao < 5
        ORDER BY DataUltimaSincronizacao ASC
        LIMIT limite_registros;
    
    DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = TRUE;
    
    OPEN cursor_atividades;
    
    read_loop: LOOP
        FETCH cursor_atividades INTO cod_ativ_var;
        IF done THEN
            LEAVE read_loop;
        END IF;
        
        -- Inserir na fila de processamento
        INSERT INTO filasProcessamento (
            NomeFila, CodAtiv, TipoOperacao, MensagemJson, 
            CorrelationId, Prioridade
        ) VALUES (
            'reprocessamento',
            cod_ativ_var,
            4, -- Sync
            CONCAT('{"CodAtiv": "', cod_ativ_var, '", "Tipo": "reprocessamento"}'),
            UUID(),
            3 -- Alta prioridade
        );
        
        -- Atualizar status da atividade
        UPDATE atividades 
        SET StatusSincronizacao = 3, -- Reprocessando
            TentativasSincronizacao = TentativasSincronizacao + 1
        WHERE CodAtiv = cod_ativ_var;
        
        SET contador = contador + 1;
        
    END LOOP;
    
    CLOSE cursor_atividades;
    
    SELECT contador as 'Atividades reprocessadas';
END$$

-- ===================================================================
-- PROCEDURE: Estatísticas do sistema
-- ===================================================================
CREATE PROCEDURE sp_estatisticas_sistema()
BEGIN
    -- Estatísticas gerais
    SELECT 'ESTATÍSTICAS GERAIS' as categoria, '' as item, '' as valor;
    
    SELECT 
        'Usuários' as item,
        COUNT(*) as total,
        COUNT(CASE WHEN Ativo = 1 THEN 1 END) as ativos,
        COUNT(CASE WHEN DataBloqueio IS NOT NULL THEN 1 END) as bloqueados
    FROM usuarios
    UNION ALL
    SELECT 
        'Atividades' as item,
        COUNT(*) as total,
        COUNT(CASE WHEN StatusSincronizacao = 1 THEN 1 END) as sincronizadas,
        COUNT(CASE WHEN StatusSincronizacao = 2 THEN 1 END) as com_erro
    FROM atividades
    UNION ALL
    SELECT 
        'Configurações' as item,
        COUNT(*) as total,
        COUNT(CASE WHEN Ativo = 1 THEN 1 END) as ativas,
        COUNT(CASE WHEN ConfiguracaoPadrao = 1 THEN 1 END) as padrao
    FROM configuracoes_integracao;
    
    -- Logs dos últimos 7 dias
    SELECT '' as item, '' as total, '' as ativas, '' as padrao;
    SELECT 'LOGS DOS ÚLTIMOS 7 DIAS' as item, '' as total, '' as ativas, '' as padrao;
    
    SELECT 
        DATE(DataCriacao) as data,
        COUNT(*) as total_logs,
        COUNT(CASE WHEN StatusProcessamento = 1 THEN 1 END) as sucessos,
        COUNT(CASE WHEN StatusProcessamento = 2 THEN 1 END) as erros,
        ROUND(AVG(TempoProcessamentoMs), 2) as tempo_medio_ms
    FROM logsSincronizacao 
    WHERE DataCriacao >= DATE_SUB(NOW(), INTERVAL 7 DAY)
    GROUP BY DATE(DataCriacao)
    ORDER BY data DESC;
    
    -- Top 10 atividades com mais erros
    SELECT '' as data, '' as total_logs, '' as sucessos, '' as erros, '' as tempo_medio_ms;
    SELECT 'TOP 10 ATIVIDADES COM ERROS' as data, '' as total_logs, '' as sucessos, '' as erros, '' as tempo_medio_ms;
    
    SELECT 
        a.CodAtiv,
        a.Ramo,
        a.StatusSincronizacao,
        a.TentativasSincronizacao,
        COUNT(l.Id) as total_logs_erro
    FROM atividades a
    LEFT JOIN logsSincronizacao l ON a.CodAtiv = l.CodAtiv AND l.StatusProcessamento = 2
    WHERE a.StatusSincronizacao = 2
    GROUP BY a.CodAtiv, a.Ramo, a.StatusSincronizacao, a.TentativasSincronizacao
    ORDER BY total_logs_erro DESC, a.TentativasSincronizacao DESC
    LIMIT 10;
END$$

-- ===================================================================
-- PROCEDURE: Verificar saúde do sistema
-- ===================================================================
CREATE PROCEDURE sp_verificar_saude_sistema()
BEGIN
    DECLARE config_ativas INT DEFAULT 0;
    DECLARE filas_pendentes INT DEFAULT 0;
    DECLARE logs_erro_recentes INT DEFAULT 0;
    DECLARE usuarios_bloqueados INT DEFAULT 0;
    
    -- Verificar configurações ativas
    SELECT COUNT(*) INTO config_ativas 
    FROM configuracoes_integracao 
    WHERE Ativo = 1;
    
    -- Verificar filas pendentes há mais de 1 hora
    SELECT COUNT(*) INTO filas_pendentes 
    FROM filasProcessamento 
    WHERE StatusFila = 0 
      AND DataCriacao < DATE_SUB(NOW(), INTERVAL 1 HOUR);
    
    -- Verificar logs com erro nas últimas 24h
    SELECT COUNT(*) INTO logs_erro_recentes 
    FROM logsSincronizacao 
    WHERE StatusProcessamento = 2 
      AND DataCriacao >= DATE_SUB(NOW(), INTERVAL 24 HOUR);
    
    -- Verificar usuários bloqueados
    SELECT COUNT(*) INTO usuarios_bloqueados 
    FROM usuarios 
    WHERE DataBloqueio IS NOT NULL 
      AND Ativo = 0;
    
    -- Retornar diagnóstico
    SELECT 
        'VERIFICAÇÃO DE SAÚDE DO SISTEMA' as categoria,
        NOW() as data_verificacao;
        
    SELECT 
        'Configurações ativas' as item,
        config_ativas as valor,
        CASE WHEN config_ativas > 0 THEN 'OK' ELSE 'ATENÇÃO' END as status
    UNION ALL
    SELECT 
        'Filas pendentes (>1h)' as item,
        filas_pendentes as valor,
        CASE WHEN filas_pendentes = 0 THEN 'OK' ELSE 'ATENÇÃO' END as status
    UNION ALL
    SELECT 
        'Erros recentes (24h)' as item,
        logs_erro_recentes as valor,
        CASE WHEN logs_erro_recentes < 10 THEN 'OK' ELSE 'ATENÇÃO' END as status
    UNION ALL
    SELECT 
        'Usuários bloqueados' as item,
        usuarios_bloqueados as valor,
        CASE WHEN usuarios_bloqueados = 0 THEN 'OK' ELSE 'ATENÇÃO' END as status;
END$$

-- ===================================================================
-- PROCEDURE: Configurar sistema inicial
-- ===================================================================
CREATE PROCEDURE sp_configurar_sistema_inicial()
BEGIN
    DECLARE config_exists INT DEFAULT 0;
    DECLARE admin_exists INT DEFAULT 0;
    
    -- Verificar se já existe configuração
    SELECT COUNT(*) INTO config_exists FROM configuracoes_integracao;
    SELECT COUNT(*) INTO admin_exists FROM usuarios WHERE Login = 'admin';
    
    IF config_exists = 0 THEN
        -- Inserir configuração padrão se não existir
        INSERT INTO configuracoes_integracao (
            Nome, Descricao, UrlApi, Login, SenhaCriptografada,
            ConfiguracaoPadrao, Ativo, CriadoPor
        ) VALUES (
            'Configuração Padrão',
            'Configuração inicial do sistema',
            'https://api.exemplo.com',
            'admin',
            'senha_temporaria_criptografada',
            1,
            1,
            1
        );
    END IF;
    
    IF admin_exists = 0 THEN
        -- Criar usuário admin se não existir
        INSERT INTO usuarios (Nome, Login, Email, SenhaHash, Ativo) VALUES
        ('Administrador', 'admin', 'admin@sistema.com', 
         '$2a$11$CwTycUXWue0Thq9StjUM0uBTFkXEPSHAA1MrSFXCid3.s5HVDR1lO', 1);
    END IF;
    
    SELECT 'Sistema configurado com sucesso!' as resultado;
END$$

-- ===================================================================
-- PROCEDURE: Buscar atividades para sincronização
-- ===================================================================
CREATE PROCEDURE sp_buscar_atividades_para_sync(
    IN limite INT DEFAULT 50,
    IN prioridade_minima INT DEFAULT 5
)
BEGIN
    SELECT 
        a.CodAtiv,
        a.Ramo,
        a.PercDesc,
        a.CalculaSt,
        a.StatusSincronizacao,
        a.TentativasSincronizacao,
        a.DataUltimaSincronizacao,
        c.Id as ConfiguracaoId,
        c.Nome as ConfiguracaoNome,
        c.UrlApi
    FROM atividades a
    CROSS JOIN configuracoes_integracao c
    WHERE a.StatusSincronizacao IN (0, 2, 3) -- Pendente, Erro, Reprocessando
      AND c.Ativo = 1
      AND c.ConfiguracaoPadrao = 1
      AND a.TentativasSincronizacao < 5
    ORDER BY 
        CASE a.StatusSincronizacao 
            WHEN 3 THEN 1  -- Reprocessando (prioridade alta)
            WHEN 2 THEN 2  -- Erro (prioridade média)
            WHEN 0 THEN 3  -- Pendente (prioridade baixa)
        END,
        a.DataCriacao ASC
    LIMIT limite;
END$$

-- ===================================================================
-- PROCEDURE: Marcar atividade como sincronizada
-- ===================================================================
CREATE PROCEDURE sp_marcar_atividade_sincronizada(
    IN cod_ativ VARCHAR(50),
    IN configuracao_id INT,
    IN tempo_processamento BIGINT DEFAULT 0
)
BEGIN
    -- Atualizar status da atividade
    UPDATE atividades 
    SET StatusSincronizacao = 1, -- Sincronizado
        DataUltimaSincronizacao = NOW(),
        UltimoErroSincronizacao = NULL
    WHERE CodAtiv = cod_ativ;
    
    -- Inserir log de sucesso
    INSERT INTO logsSincronizacao (
        ConfiguracaoId, CodAtiv, TipoOperacao, StatusProcessamento,
        TempoProcessamentoMs, NumeroTentativa
    ) VALUES (
        configuracao_id, cod_ativ, 4, 1, -- Sync, Sucesso
        tempo_processamento, 1
    );
    
    SELECT 'Atividade marcada como sincronizada!' as resultado;
END$$

DELIMITER ;

-- ===================================================================
-- VERIFICAÇÃO
-- ===================================================================
SELECT 'Procedures criadas com sucesso!' as status;