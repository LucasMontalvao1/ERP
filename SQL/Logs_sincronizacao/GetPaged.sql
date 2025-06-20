SELECT l.id,
       l.codativ,
       CASE l.tipooperacao
           WHEN 1 THEN 'Create'
           WHEN 2 THEN 'Update'
           WHEN 3 THEN 'Delete'
           ELSE 'Unknown'
       END as tipooperacaodesc,
       CASE l.statusprocessamento
           WHEN 0 THEN 'Iniciado'
           WHEN 1 THEN 'Sucesso'
           WHEN 2 THEN 'Erro'
           WHEN 3 THEN 'Timeout'
           WHEN 4 THEN 'Cancelado'
           WHEN 5 THEN 'Reprocessando'
           ELSE 'Unknown'
       END as statusdesc,
       l.endpointusado,
       l.metodohttpusado,
       l.codigohttp,
       l.mensagemerro,
       l.tempoprocessamentoms,
       l.numerotentativa,
       l.proximatentativa,
       l.correlationid,
       l.datacriacao,
       c.nome as configuracaonome
FROM logsSincronizacao l
INNER JOIN configuracoes_integracao c ON l.configuracaoid = c.id
WHERE (@codAtiv IS NULL OR l.codativ LIKE CONCAT('%', @codAtiv, '%'))
  AND (@tipoOperacao IS NULL OR l.tipooperacao = @tipoOperacao)
  AND (@statusProcessamento IS NULL OR l.statusprocessamento = @statusProcessamento)
  AND (@dataInicio IS NULL OR l.datacriacao >= @dataInicio)
  AND (@dataFim IS NULL OR l.datacriacao <= @dataFim)
  AND (@correlationId IS NULL OR l.correlationid = @correlationId)
ORDER BY l.datacriacao DESC
LIMIT @pageSize OFFSET @offset;