SELECT COUNT(*)
FROM atividades
WHERE (@CodAtiv IS NULL OR CodAtiv LIKE CONCAT('%', @CodAtiv, '%'))
  AND (@Ramo IS NULL OR Ramo LIKE CONCAT('%', @Ramo, '%'))
  AND (@CalculaSt IS NULL OR CalculaSt = @CalculaSt)
  AND (@StatusSincronizacao IS NULL OR StatusSincronizacao = @StatusSincronizacao)
  AND (@DataCriacaoInicio IS NULL OR DataCriacao >= @DataCriacaoInicio)
  AND (@DataCriacaoFim IS NULL OR DataCriacao <= @DataCriacaoFim);