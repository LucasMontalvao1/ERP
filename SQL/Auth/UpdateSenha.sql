UPDATE usuarios 
SET senha_hash = @senha_hash,
    data_atualizacao = @data_atualizacao
WHERE id = @usuario_id;