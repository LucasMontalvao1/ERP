SELECT COUNT(*) 
FROM configuracoes_integracao
WHERE (@ativo IS NULL OR ativo = @ativo);