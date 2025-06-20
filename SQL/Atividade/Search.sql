SELECT a.codativ,
       a.ramo,
       a.percdesc,
       a.calculast,
       a.statussincronizacao,
       a.dataultimasincronizacao,
       a.tentativassincronizacao,
       a.ultimoerrosincronizacao,
       a.datacriacao,
       a.dataatualizacao,
       a.criadopor,
       a.atualizadopor,
       CURRENT_TIMESTAMP as version
FROM atividades a
WHERE a.codativ LIKE CONCAT('%', @searchTerm, '%')
   OR a.ramo LIKE CONCAT('%', @searchTerm, '%')
ORDER BY a.datacriacao DESC
LIMIT @limit;