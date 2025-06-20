SELECT * FROM filas_processamento 
WHERE correlation_id = @correlationId
ORDER BY data_criacao;