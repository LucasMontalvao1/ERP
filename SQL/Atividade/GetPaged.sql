SELECT 
    a.CodAtiv,
    a.Ramo,
    a.PercDesc,
    a.CalculaSt,
    a.StatusSincronizacao,
    CASE a.StatusSincronizacao
        WHEN 0 THEN 'Pendente'
        WHEN 1 THEN 'Sincronizado'
        WHEN 2 THEN 'Erro'
        WHEN 3 THEN 'Reprocessando'
        WHEN 4 THEN 'Cancelado'
        ELSE 'Desconhecido'
    END AS StatusSincronizacaoDescricao,
    a.DataUltimaSincronizacao,
    a.DataCriacao,
    uc.Nome AS CriadorNome
FROM atividades a
LEFT JOIN usuarios uc ON a.CriadoPor = uc.Id
WHERE (@CodAtiv IS NULL OR a.CodAtiv LIKE CONCAT('%', @CodAtiv, '%'))
  AND (@Ramo IS NULL OR a.Ramo LIKE CONCAT('%', @Ramo, '%'))
  AND (@CalculaSt IS NULL OR a.CalculaSt = @CalculaSt)
  AND (@StatusSincronizacao IS NULL OR a.StatusSincronizacao = @StatusSincronizacao)
  AND (@DataCriacaoInicio IS NULL OR a.DataCriacao >= @DataCriacaoInicio)
  AND (@DataCriacaoFim IS NULL OR a.DataCriacao <= @DataCriacaoFim)
ORDER BY
    CASE 
        WHEN @orderBy = 'CodAtiv' AND @orderDirection = 'ASC' THEN a.CodAtiv 
    END ASC,
    CASE 
        WHEN @orderBy = 'CodAtiv' AND @orderDirection = 'DESC' THEN a.CodAtiv 
    END DESC,
    CASE 
        WHEN @orderBy = 'Ramo' AND @orderDirection = 'ASC' THEN a.Ramo 
    END ASC,
    CASE 
        WHEN @orderBy = 'Ramo' AND @orderDirection = 'DESC' THEN a.Ramo 
    END DESC,
    CASE 
        WHEN @orderBy = 'DataCriacao' AND @orderDirection = 'ASC' THEN a.DataCriacao 
    END ASC,
    CASE 
        WHEN @orderBy = 'DataCriacao' AND @orderDirection = 'DESC' THEN a.DataCriacao 
    END DESC,
    a.DataCriacao DESC 
LIMIT @pageSize OFFSET @offset;
