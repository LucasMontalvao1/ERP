SELECT a.*, 
       uc.nome as criadornome,
       ua.nome as atualizadornome
FROM atividades a
LEFT JOIN usuarios uc ON a.criadopor = uc.id
LEFT JOIN usuarios ua ON a.atualizadopor = ua.id
ORDER BY a.datacriacao DESC;