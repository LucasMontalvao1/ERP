UPDATE usuarios 
SET tentativas_login = 0,
    data_bloqueio = NULL,
    data_atualizacao = UTC_TIMESTAMP()
WHERE id = @usuario_id;