UPDATE usuarios 
SET ultimologin = @ultimologin,
    dataatualizacao = @ultimologin
WHERE id = @usuarioid;