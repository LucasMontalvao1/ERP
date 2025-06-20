INSERT INTO logsSincronizacao (
    configuracaoid, codativ, tipooperacao, statusprocessamento,
    categoriaendpoint, acaoendpoint, endpointusado, metodohttpusado,
    payloadenviado, codigohttp, tempoprocessamentoms, numerotentativa,
    correlationid, useragent, iporigem, tamanhopayloadbytes,
    datacriacao
) VALUES (
    @configuracaoId, @codAtiv, @tipoOperacao, @statusProcessamento,
    @categoriaEndpoint, @acaoEndpoint, @endpointUsado, @metodoHttpUsado,
    @payloadEnviado, @codigoHttp, @tempoProcessamentoMs, @numeroTentativa,
    @correlationId, @userAgent, @ipOrigem, @tamanhoPayloadBytes,
    @dataCriacao
);
SELECT LAST_INSERT_ID();