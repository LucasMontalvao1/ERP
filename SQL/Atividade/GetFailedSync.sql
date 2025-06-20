SELECT * FROM atividades 
WHERE statussincronizacao = 2
  AND tentativassincronizacao < 3
ORDER BY datacriacao ASC
LIMIT @limit;