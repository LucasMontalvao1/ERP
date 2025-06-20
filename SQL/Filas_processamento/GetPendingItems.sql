SELECT * FROM filas_processamento 
WHERE nome_fila = @nomeFila 
  AND status_fila = 0
  AND (proximo_processamento IS NULL OR proximo_processamento <= NOW())
ORDER BY prioridade, data_criacao
LIMIT @limit;