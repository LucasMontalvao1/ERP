UPDATE usuarios 
SET tentativaslogin = 0,
    databloqueio = NULL,
    dataatualizacao = UTC_TIMESTAMP()
WHERE id = @usuarioid;