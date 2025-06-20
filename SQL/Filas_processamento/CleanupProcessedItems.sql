DELETE FROM filas_processamento 
WHERE status_fila = 2 
  AND data_processamento < DATE_SUB(NOW(), INTERVAL @olderThanDays DAY);