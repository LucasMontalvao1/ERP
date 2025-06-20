-- Roles padrão
INSERT IGNORE INTO roles (Nome, Descricao, Ativo) VALUES
('SuperAdmin', 'Super Administrador - Acesso Total', 1),
('Admin', 'Administrador do Sistema', 1),
('Gerente', 'Gerente - Acesso Avançado', 1),
('Operador', 'Operador - Acesso Básico', 1),
('Visualizador', 'Apenas Visualização', 1);

-- Usuário administrador padrão
-- Login: admin / Senha: admin123 (hash bcrypt)
INSERT IGNORE INTO usuarios (
    Nome, Login, Email, SenhaHash, Ativo, PrimeiroAcesso
) VALUES (
    'Administrador do Sistema', 
    'admin', 
    'admin@montalvao.com', 
    'admin123', 
    1, 
    1
);

-- Atribuir role de SuperAdmin ao usuário admin (ID 1 para SuperAdmin, ID 1 para admin)
INSERT IGNORE INTO usuarioRoles (UsuarioId, RoleId) VALUES (1, 1);

-- Configuração de integração padrão (exemplo SalesForce)
INSERT IGNORE INTO configuracoes_integracao (
    Nome, 
    Descricao, 
    UrlApi, 
    Login, 
    SenhaCriptografada,
    VersaoApi, 
    EndpointLogin, 
    EndpointPrincipal,
    ConfiguracaoPadrao, 
    Ativo, 
    CriadoPor
) VALUES (
    'FV Produção',
    'Integração com FV ambiente de produção',
    'https://intext-oerp-novo.solucoesmaxima.com.br',
    'vaT7MdYkINzT3lWqWdFs0pcRUPuLpV8iXdkl48V7nTg=',
    'XC9D2SWJnGArIQ/iLhUE/UwtprTApXfQWDyNkTCyJRU=',
    'v1',
    '/auth/login',
    '/api/v1',
    1, -- Configuração padrão
    1, -- Ativo
    1  -- Criado pelo admin
);

-- Endpoints para a configuração SalesForce
INSERT IGNORE INTO endpointsConfiguracao (
    ConfiguracaoId, Categoria, Acao, Endpoint, MetodoHttp, Ativo
) VALUES
(1, 'auth', 'login', '/api/v1/login', 'POST', 1),
(1, 'atividades', 'list', '/atividades/todos', 'GET', 1),
(1, 'atividades', 'create', '/atividades', 'POST', 1),
(1, 'atividades', 'update', '/api/v1/atividades/{codAtiv}', 'PUT', 1),
(1, 'atividades', 'delete', '/api/v1/atividades/{codAtiv}', 'DELETE', 1),
(1, 'atividades', 'sync', '/api/v1/atividades/sync', 'POST', 1);

-- Exemplo de atividades para testar
INSERT IGNORE INTO atividades (
    CodAtiv, Ramo, PercDesc, CalculaSt, StatusSincronizacao, CriadoPor
) VALUES
('01001', 'Agricultura, pecuária e serviços relacionados', 5.50, 'S', 0, 1),
('02001', 'Produção florestal - florestas plantadas', 3.75, 'N', 0, 1),
('03001', 'Pesca em água salgada', 2.25, 'N', 0, 1),
('05001', 'Extração de carvão mineral', 8.90, 'S', 0, 1),
('10001', 'Frigorífico - abate de bovinos', 12.45, 'S', 0, 1);

-- Algumas tentativas de login para demonstração
INSERT IGNORE INTO logLoginTentativas (
    Login, Sucesso, IpAddress, UserAgent
) VALUES
('admin', 1, '127.0.0.1', 'PostmanRuntime/7.32.0'),
('admin', 1, '192.168.1.100', 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)'),
('usuario_teste', 0, '192.168.1.105', 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)');

-- Mensagem de sucesso
SELECT 'Dados iniciais inseridos com sucesso!' as status;