UPDATE usuarios 
SET ultimo_login = @ultimo_login,
    data_atualizacao = @ultimo_login
WHERE id = @usuario_id;