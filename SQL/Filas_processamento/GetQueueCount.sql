SELECT COUNT(*) FROM filas_processamento 
WHERE nome_fila = @nomeFila
  AND (@status IS NULL OR status_fila = @status);