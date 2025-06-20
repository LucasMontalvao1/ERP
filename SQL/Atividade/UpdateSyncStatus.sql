UPDATE atividades 
SET statussincronizacao = @status,
    dataultimasincronizacao = @dataUltimaSincronizacao,
    tentativassincronizacao = tentativassincronizacao + 1,
    ultimoerrosincronizacao = @ultimoErroSincronizacao
WHERE codativ = @codAtiv;