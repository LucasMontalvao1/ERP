UPDATE usuarios 
SET primeiro_acesso = @primeiro_acesso,
    data_atualizacao = @data_atualizacao
WHERE id = @usuario_id;