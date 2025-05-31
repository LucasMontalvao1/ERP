-- Tabela de usuários
CREATE TABLE IF NOT EXISTS usuarios (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    login VARCHAR(50) NOT NULL UNIQUE,
    email VARCHAR(254) NOT NULL UNIQUE,
    senha_hash TEXT NOT NULL,
    ativo TINYINT(1) DEFAULT 1,
    data_criacao DATETIME DEFAULT CURRENT_TIMESTAMP,
    data_atualizacao DATETIME NULL,
    ultimo_login DATETIME NULL,
    tentativas_login INT DEFAULT 0,
    data_bloqueio DATETIME NULL,
    primeiro_acesso TINYINT(1) DEFAULT 1,
    INDEX idx_login (login),
    INDEX idx_email (email),
    INDEX idx_ativo (ativo)
);

-- Tabela de roles/papéis
CREATE TABLE IF NOT EXISTS roles (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(50) NOT NULL UNIQUE,
    descricao VARCHAR(255) NULL,
    ativo TINYINT(1) DEFAULT 1,
    INDEX idx_nome (nome),
    INDEX idx_ativo (ativo)
);

-- Tabela de relacionamento usuário-role
CREATE TABLE IF NOT EXISTS usuario_roles (
    usuario_id INT NOT NULL,
    role_id INT NOT NULL,
    data_atribuicao DATETIME DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (usuario_id, role_id),
    FOREIGN KEY (usuario_id) REFERENCES usuarios(id) ON DELETE CASCADE,
    FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE
);

-- Tabela de log de tentativas de login
CREATE TABLE IF NOT EXISTS log_login_tentativas (
    id INT AUTO_INCREMENT PRIMARY KEY,
    login VARCHAR(50) NOT NULL,
    sucesso TINYINT(1) NOT NULL,
    ip_address VARCHAR(45) NOT NULL,
    user_agent TEXT NULL,
    data_tentativa DATETIME DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_login (login),
    INDEX idx_data_tentativa (data_tentativa),
    INDEX idx_sucesso (sucesso)
);

