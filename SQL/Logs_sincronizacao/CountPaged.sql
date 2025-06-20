SELECT COUNT(*)
FROM logsSincronizacao l
WHERE (@codAtiv IS NULL OR l.codativ LIKE CONCAT('%', @codAtiv, '%'))
  AND (@tipoOperacao IS NULL OR l.tipooperacao = @tipoOperacao)
  AND (@statusProcessamento IS NULL OR l.statusprocessamento = @statusProcessamento)
  AND (@dataInicio IS NULL OR l.datacriacao >= @dataInicio)
  AND (@dataFim IS NULL OR l.datacriacao <= @dataFim)
  AND (@correlationId IS NULL OR l.correlationid = @correlationId);