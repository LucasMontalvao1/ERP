UPDATE atividades 
SET ramo = @ramo,
    percdesc = @percDesc,
    calculast = @calculaSt,
    statussincronizacao = @statusSincronizacao,
    dataultimasincronizacao = @dataUltimaSincronizacao,
    tentativassincronizacao = @tentativasSincronizacao,
    ultimoerrosincronizacao = @ultimoErroSincronizacao,
    dataatualizacao = @dataAtualizacao,
    atualizadopor = @atualizadoPor
WHERE codativ = @codAtiv;