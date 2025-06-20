UPDATE usuarios 
SET senhahash = @senhahash,
    dataatualizacao = @dataatualizacao
WHERE id = @usuarioid;