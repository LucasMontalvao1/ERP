UPDATE configuracoes_integracao SET configuracaopadrao = 0;
UPDATE configuracoes_integracao SET configuracaopadrao = 1 WHERE id = @id;