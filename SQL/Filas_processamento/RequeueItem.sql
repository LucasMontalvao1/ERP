UPDATE filas_processamento 
SET status_fila = 0,
    erro_processamento = NULL,
    data_processamento = @dataProcessamento
WHERE id = @id;