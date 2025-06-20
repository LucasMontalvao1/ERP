UPDATE configuracoes_integracao 
SET data_atualizacao = NOW()
WHERE id = @id;