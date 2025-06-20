SELECT * FROM filas_processamento 
WHERE nome_fila = @nomeFila 
  AND status_fila = 3
  AND tentativas_processamento < max_tentativas
ORDER BY data_criacao
LIMIT @limit;