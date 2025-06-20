SELECT 
    tipooperacao,
    statusprocessamento,
    COUNT(*) as total,
    AVG(tempoprocessamentoms) as tempomedio
FROM logsSincronizacao
WHERE datacriacao >= DATE_SUB(NOW(), INTERVAL @days DAY)
GROUP BY tipooperacao, statusprocessamento
ORDER BY tipooperacao, statusprocessamento;