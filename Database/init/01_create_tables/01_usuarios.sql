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
