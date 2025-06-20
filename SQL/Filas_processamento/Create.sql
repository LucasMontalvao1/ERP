INSERT INTO filas_processamento (
    nome_fila, cod_ativ, tipo_operacao, status_fila, mensagem_json,
    tentativas_processamento, max_tentativas, proximo_processamento,
    correlation_id, prioridade, data_criacao
) VALUES (
    @nomeFila, @codAtiv, @tipoOperacao, @statusFila, @mensagemJson,
    @tentativasProcessamento, @maxTentativas, @proximoProcessamento,
    @correlationId, @prioridade, @dataCriacao
);
SELECT LAST_INSERT_ID();