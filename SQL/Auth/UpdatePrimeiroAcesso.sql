UPDATE usuarios 
SET primeiroacesso = @primeiroacesso,
    dataatualizacao = @dataatualizacao
WHERE id = @usuarioid;