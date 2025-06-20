UPDATE usuarios 
SET tentativaslogin = tentativaslogin + 1,
    dataatualizacao = UTC_TIMESTAMP()
WHERE id = @usuarioid;

SELECT tentativaslogin 
FROM usuarios 
WHERE id = @usuarioid;