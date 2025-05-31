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
WHERE ativo = 1
ORDER BY nome;