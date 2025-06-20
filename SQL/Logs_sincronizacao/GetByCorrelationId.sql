SELECT l.*, c.nome as configuracaonome
FROM logsSincronizacao l
INNER JOIN configuracoes_integracao c ON l.configuracaoid = c.id
WHERE l.correlationid = @correlationId
ORDER BY l.datacriacao DESC;