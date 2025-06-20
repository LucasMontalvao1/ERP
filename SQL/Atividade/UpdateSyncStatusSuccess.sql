UPDATE atividades 
SET statussincronizacao = 1,
    dataultimasincronizacao = NOW(),
    ultimoerrosincronizacao = NULL
WHERE codativ = @codAtiv;