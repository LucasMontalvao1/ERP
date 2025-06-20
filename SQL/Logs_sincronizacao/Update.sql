UPDATE logsSincronizacao 
SET statusprocessamento = @statusProcessamento,
    respostarecebida = @respostaRecebida,
    codigohttp = @codigoHttp,
    mensagemerro = @mensagemErro,
    tempoprocessamento_ms = @tempoProcessamentoMs,
    numerotentativa = @numeroTentativa,
    proximatentativa = @proximaTentativa,
    tamanhoresposta_bytes = @tamanhoRespostaBytes,
    dataatualizacao = @dataAtualizacao
WHERE id = @id;
