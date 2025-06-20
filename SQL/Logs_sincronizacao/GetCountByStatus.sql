SELECT COUNT(*)
FROM logsSincronizacao
WHERE statusprocessamento = @status
  AND (@startDate IS NULL OR datacriacao >= @startDate)
  AND (@endDate IS NULL OR datacriacao <= @endDate);