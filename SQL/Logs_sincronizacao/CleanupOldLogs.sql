DELETE FROM logsSincronizacao 
WHERE datacriacao < DATE_SUB(NOW(), INTERVAL @olderThanDays DAY)
  AND statusprocessamento IN (1, 4);