UPDATE atividades 
SET statussincronizacao = @status,
    dataultimasincronizacao = NOW()
WHERE codativ IN (@codAtivs);