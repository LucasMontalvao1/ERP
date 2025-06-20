UPDATE configuracoes_integracao 
SET token_atual = @token,
    token_expiracao = @expiracao
WHERE id = @id;