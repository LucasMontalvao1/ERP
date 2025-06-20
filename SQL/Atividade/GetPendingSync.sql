SELECT * FROM atividades 
WHERE statussincronizacao = 0
ORDER BY datacriacao ASC
LIMIT @limit;