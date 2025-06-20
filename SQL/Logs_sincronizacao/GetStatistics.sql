SELECT 
    statusprocessamento,
    COUNT(*) as total,
    AVG(tempoprocessamentoms) as tempomedio,
    DATE(datacriacao) as data
FROM logsSincronizacao
WHERE datacriacao >= DATE_SUB(NOW(), INTERVAL @days DAY)
GROUP BY statusprocessamento, DATE(datacriacao)
ORDER BY data DESC, statusprocessamento;