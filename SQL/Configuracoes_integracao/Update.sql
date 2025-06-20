UPDATE configuracoes_integracao 
SET nome = @nome,
    descricao = @descricao,
    urlapi = @urlApi,
    versaoapi = @versaoApi,
    ativo = @ativo,
    timeoutsegundos = @timeoutSegundos,
    maxtentativas = @maxTentativas,
    configuracaopadrao = @configuracaoPadrao,
    retrypolicy = @retryPolicy,
    retrydelaybaseseconds = @retryDelayBaseSeconds,
    enablecircuitbreaker = @enableCircuitBreaker,
    circuitbreakerthreshold = @circuitBreakerThreshold,
    dataatualizacao = @dataAtualizacao,
    atualizadopor = @atualizadoPor
WHERE id = @id;