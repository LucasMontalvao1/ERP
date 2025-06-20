SELECT 
    DATE(datacriacao) as data,
    COUNT(*) as total,
    SUM(CASE WHEN statusprocessamento = 1 THEN 1 ELSE 0 END) as sucesso,
    SUM(CASE WHEN statusprocessamento = 2 THEN 1 ELSE 0 END) as erro,
    SUM(CASE WHEN statusprocessamento = 0 THEN 1 ELSE 0 END) as pendente
FROM logsSincronizacao
WHERE datacriacao >= DATE_SUB(NOW(), INTERVAL @days DAY)
GROUP BY DATE(datacriacao)
ORDER BY data DESC;