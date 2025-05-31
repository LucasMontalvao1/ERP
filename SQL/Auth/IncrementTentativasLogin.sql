UPDATE usuarios 
SET tentativas_login = tentativas_login + 1,
    data_atualizacao = UTC_TIMESTAMP()
WHERE id = @usuario_id;

SELECT tentativas_login 
FROM usuarios 
WHERE id = @usuario_id;