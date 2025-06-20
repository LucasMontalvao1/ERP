SELECT r.nome
FROM usuarioRoles ur
INNER JOIN roles r ON ur.roleid = r.id
WHERE ur.usuarioid = @usuarioid
    AND r.ativo = 1
ORDER BY r.nome;