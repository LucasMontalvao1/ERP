UPDATE logsSincronizacao 
SET statusprocessamento = @status,
    respostarecebida = @response,
    mensagemerro = @errorMessage,
    dataatualizacao = NOW()
WHERE id = @logId;