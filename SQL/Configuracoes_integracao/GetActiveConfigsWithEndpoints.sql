SELECT c.*, 
       e.id as endpoint_id,
       e.categoria as endpoint_categoria,
       e.acao as endpoint_acao,
       e.endpoint as endpoint_path,
       e.metodo_http as endpoint_metodo
FROM configuracoes_integracao c
LEFT JOIN endpoints_configuracao e ON c.id = e.configuracao_id AND e.ativo = 1
WHERE c.ativo = 1
ORDER BY c.configuracaopadrao DESC, c.nome, e.categoria, e.acao;