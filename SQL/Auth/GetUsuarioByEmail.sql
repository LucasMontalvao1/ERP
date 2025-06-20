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
WHERE email = @email 
    AND ativo = 1
LIMIT 1;