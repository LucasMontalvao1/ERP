SELECT 
    id,
    nome,
    login,
    email,
    senha_hash,
    ativo,
    data_criacao,
    data_atualizacao,
    ultimo_login,
    tentativas_login,
    data_bloqueio,
    primeiro_acesso
FROM usuarios 
WHERE email = @email 
    AND ativo = 1
LIMIT 1;