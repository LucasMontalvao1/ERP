-- 1. USUÁRIO ROLES
ALTER TABLE usuarioRoles 
ADD CONSTRAINT fk_usuarioRoles_UsuarioId 
    FOREIGN KEY (UsuarioId) REFERENCES usuarios(Id) ON DELETE CASCADE,
ADD CONSTRAINT fk_usuarioRoles_RoleId 
    FOREIGN KEY (RoleId) REFERENCES roles(Id) ON DELETE CASCADE;

-- 2. ATIVIDADES
ALTER TABLE atividades 
ADD CONSTRAINT fk_atividades_CriadoPor 
    FOREIGN KEY (CriadoPor) REFERENCES usuarios(Id) ON DELETE SET NULL,
ADD CONSTRAINT fk_atividades_AtualizadoPor 
    FOREIGN KEY (AtualizadoPor) REFERENCES usuarios(Id) ON DELETE SET NULL;

-- 3. CONFIGURAÇÕES INTEGRAÇÃO
ALTER TABLE configuracoes_integracao 
ADD CONSTRAINT fk_config_CriadoPor 
    FOREIGN KEY (CriadoPor) REFERENCES usuarios(Id) ON DELETE SET NULL,
ADD CONSTRAINT fk_config_AtualizadoPor 
    FOREIGN KEY (AtualizadoPor) REFERENCES usuarios(Id) ON DELETE SET NULL;

-- 4. ENDPOINTS CONFIGURAÇÃO
ALTER TABLE endpointsConfiguracao 
ADD CONSTRAINT fk_endpoints_ConfiguracaoId 
    FOREIGN KEY (ConfiguracaoId) REFERENCES configuracoes_integracao(Id) ON DELETE CASCADE;

-- 5. LOGS SINCRONIZAÇÃO
ALTER TABLE logsSincronizacao 
ADD CONSTRAINT fk_logs_ConfiguracaoId 
    FOREIGN KEY (ConfiguracaoId) REFERENCES configuracoes_integracao(Id) ON DELETE CASCADE,
ADD CONSTRAINT fk_logs_CodAtiv 
    FOREIGN KEY (CodAtiv) REFERENCES atividades(CodAtiv) ON DELETE CASCADE;

-- 6. FILAS PROCESSAMENTO
ALTER TABLE filasProcessamento 
ADD CONSTRAINT fk_filas_CodAtiv 
    FOREIGN KEY (CodAtiv) REFERENCES atividades(CodAtiv) ON DELETE CASCADE;

-- 7. WEBHOOKS (OPCIONAL - apenas se criar as tabelas)
 ALTER TABLE logsWebhooks 
 ADD CONSTRAINT fk_logsWebhooks_WebhookId 
     FOREIGN KEY (WebhookId) REFERENCES webhooksNotificacao(Id) ON DELETE CASCADE,
 ADD CONSTRAINT fk_logsWebhooks_LogSincronizacaoId 
     FOREIGN KEY (LogSincronizacaoId) REFERENCES logsSincronizacao(Id) ON DELETE CASCADE;

-- ===================================================================
-- VERIFICAÇÃO FINAL
-- ===================================================================
SELECT 'Foreign Keys criadas com sucesso!' as status, NOW() as data_execucao;