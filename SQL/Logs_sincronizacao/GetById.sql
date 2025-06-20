SELECT l.*, c.nome as configuracaonome, a.ramo as atividaderamo
FROM logs_sincronizacao l
INNER JOIN configuracoes_integracao c ON l.configuracaoid = c.id
INNER JOIN atividades a ON l.codativ = a.codativ
WHERE l.id = @id;