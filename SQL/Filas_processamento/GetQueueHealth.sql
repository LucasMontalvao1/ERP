SELECT 
    nome_fila,
    COUNT(*) as total_items,
    SUM(CASE WHEN status_fila = 0 THEN 1 ELSE 0 END) as pendentes,
    SUM(CASE WHEN status_fila = 1 THEN 1 ELSE 0 END) as processando,
    SUM(CASE WHEN status_fila = 2 THEN 1 ELSE 0 END) as processados,
    SUM(CASE WHEN status_fila = 3 THEN 1 ELSE 0 END) as com_erro,
    SUM(CASE WHEN status_fila = 4 THEN 1 ELSE 0 END) as cancelados,
    MIN(data_criacao) as item_mais_antigo,
    MAX(data_criacao) as item_mais_recente
FROM filas_processamento
GROUP BY nome_fila
ORDER BY nome_fila;