SELECT l.*, c.nome as configuracaonome
FROM logsSincronizacao l
INNER JOIN configuracoes_integracao c ON l.configuracaoid = c.id
ORDER BY l.datacriacao DESC;