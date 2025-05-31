SELECT r.nome
FROM usuario_roles ur
INNER JOIN roles r ON ur.role_id = r.id
WHERE ur.usuario_id = @usuario_id
    AND r.ativo = 1
ORDER BY r.nome;