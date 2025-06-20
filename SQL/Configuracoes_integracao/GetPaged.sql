SELECT id, nome, descricao, urlapi, versaoapi, ativo, configuracaopadrao, 
       timeoutsegundos, maxtentativas, datacriacao
FROM configuracoes_integracao
WHERE (@ativo IS NULL OR ativo = @ativo)
ORDER BY configuracaopadrao DESC, nome
LIMIT @pageSize OFFSET @offset;