UPDATE filas_processamento 
SET status_fila = @statusFila,
    tentativas_processamento = @tentativasProcessamento,
    proximo_processamento = @proximoProcessamento,
    erro_processamento = @erroProcessamento,
    data_processamento = @dataProcessamento
WHERE id = @id;