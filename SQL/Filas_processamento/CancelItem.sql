UPDATE filas_processamento 
SET status_fila = 4,
    erro_processamento = CONCAT('Cancelado: ', @motivo),
    data_processamento = @dataProcessamento
WHERE id = @id;