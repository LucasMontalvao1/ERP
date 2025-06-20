SELECT l.*, c.nome as configuracaonome
FROM logsSincronizacao l
INNER JOIN configuracoes_integracao c ON l.configuracaoid = c.id
WHERE l.statusprocessamento = 2 
  AND l.numerotentativa < 3
  AND (l.proximatentativa IS NULL OR l.proximatentativa <= NOW())
ORDER BY l.datacriacao ASC
LIMIT @limit;