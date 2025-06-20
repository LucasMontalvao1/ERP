SELECT * FROM filas_processamento 
WHERE nome_fila = @nomeFila 
  AND status_fila IN (0, 1)
ORDER BY prioridade, data_criacao
LIMIT @limit;