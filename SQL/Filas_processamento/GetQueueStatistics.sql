SELECT nome_fila, COUNT(*) as total
FROM filas_processamento
WHERE status_fila IN (0, 1)
GROUP BY nome_fila
ORDER BY nome_fila;