INSERT INTO configuracoes_integracao (
    nome, descricao, urlapi, login, senhacriptografada, versaoapi,
    endpointlogin, endpointprincipal, ativo, timeoutsegundos, maxtentativas,
    configuracaopadrao, retrypolicy, retrydelaybaseseconds, 
    enablecircuitbreaker, circuitbreaker_threshold, datacriacao, criadopor
) VALUES (
    @nome, @descricao, @urlApi, @login, @senhaCriptografada, @versaoApi,
    @endpointLogin, @endpointPrincipal, @ativo, @timeoutSegundos, @maxTentativas,
    @configuracaoPadrao, @retryPolicy, @retryDelayBaseSeconds,
    @enableCircuitBreaker, @circuitBreakerThreshold, @dataCriacao, @criadoPor
);
SELECT LAST_INSERT_ID();