UPDATE filas_processamento 
SET tentativas_processamento = tentativas_processamento + 1,
    proximo_processamento = @nextProcessing
WHERE id = @id;