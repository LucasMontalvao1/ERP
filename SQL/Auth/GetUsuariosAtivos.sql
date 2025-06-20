SELECT 
    id,
    nome,
    login,
    email,
    senhahash,
    ativo,
    datacriacao,
    dataatualizacao,
    ultimologin,
    tentativaslogin,
    databloqueio,
    primeiroacesso
FROM usuarios 
WHERE ativo = 1
ORDER BY nome;