UPDATE filas_processamento 
SET status_fila = @status,
    erro_processamento = @erro,
    data_processamento = @dataProcessamento
WHERE id = @id;