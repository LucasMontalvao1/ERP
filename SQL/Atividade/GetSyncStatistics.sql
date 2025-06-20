SELECT 
    statussincronizacao,
    COUNT(*) as total,
    CASE status_sincronizacao
        WHEN 0 THEN 'Pendente'
        WHEN 1 THEN 'Sincronizado'
        WHEN 2 THEN 'Erro'
        WHEN 3 THEN 'Reprocessando'
        WHEN 4 THEN 'Cancelado'
        ELSE 'Desconhecido'
    END as statusdescricao
FROM atividades 
GROUP BY statussincronizacao
ORDER BY statussincronizacao;